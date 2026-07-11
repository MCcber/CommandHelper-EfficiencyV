using CBHK.Model.Data;
using ICSharpCode.AvalonEdit.Document;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace CBHK.Utility.Data
{
    public class RapidJsonDataParser
    {
        #region Field
        private const int SamplingInterval = 4096;
        #endregion

        #region Method
        public (string error, int errorCharOffset, List<KeyValueAnchors> anchors) ParseFullText(
            string jsonString,TextDocument Document)
        {
            if (Document is null)
            {
                return ("TextDocument 不能为 null", -1, new List<KeyValueAnchors>());
            }

            byte[] buffer = Encoding.UTF8.GetBytes(jsonString);
            int[] sparseIndex = BuildSparseIndex(buffer);

            var anchors = new List<KeyValueAnchors>();
            var reader = new Utf8JsonReader(buffer, true, new JsonReaderState());

            // 容器栈：每个条目记录了容器的起止字节、属性名及键的字节范围、是否对象
            Stack<(int startByte, bool isObject)> containerStack = new();
            Stack<KeyValueAnchors> pendingContainerAnchors = new();   // 新增
            // 属性名栈：暂存属性名的Key起始位置与字符串长度
            Stack<(string name,int StartByte, int Length)> propertyStack = new();

            try
            {
                while (reader.Read())
                {
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.PropertyName:
                            int keyStart = (int)reader.TokenStartIndex;
                            string key = reader.GetString();
                            // UTF‑8 字节长度
                            int keyLen = key.Length;
                            //将名称起始位置与长度入栈
                            propertyStack.Push((key, keyStart, keyLen));
                            break;

                        case JsonTokenType.String:
                        case JsonTokenType.Number:
                        case JsonTokenType.True:
                        case JsonTokenType.False:
                        case JsonTokenType.Null:
                            ProcessSimpleValue(ref reader, buffer, Document, sparseIndex, anchors, containerStack, propertyStack);
                            break;

                        case JsonTokenType.StartObject:
                            ProcessContainerStart(ref reader, buffer, Document, sparseIndex, anchors, containerStack, pendingContainerAnchors, propertyStack, true);
                            break;

                        case JsonTokenType.StartArray:
                            ProcessContainerStart(ref reader, buffer, Document, sparseIndex, anchors, containerStack, pendingContainerAnchors, propertyStack, false);
                            break;

                        case JsonTokenType.EndObject:
                        case JsonTokenType.EndArray:
                            ProcessContainerEnd(ref reader, buffer, Document, sparseIndex, anchors, containerStack, pendingContainerAnchors);
                            break;
                    }
                }
            }
            catch (JsonException ex)
            {
                int errorByte = reader.TokenStartIndex > int.MaxValue ? int.MaxValue : (int)reader.TokenStartIndex;
                int errorChar = GetCharOffset(buffer, sparseIndex, Document, errorByte);
                return (ex.Message, errorChar, anchors);
            }

            return (string.Empty, -1, anchors);
        }

        private void ProcessSimpleValue(
            ref Utf8JsonReader reader,
            byte[] buffer,
            TextDocument Document,
            int[] sparseIndex,
            List<KeyValueAnchors> anchors,
            Stack<(int startByte, bool isObject)> containerStack,
            Stack<(string name,int startByte, int length)> propertyNameStack)
        {
            int keyStartByte = 0, keyEndByte = 0;
            string key = "";

            if (containerStack.Count > 0)
            {
                var (_, parentIsObject) = containerStack.Peek();
                if (parentIsObject && propertyNameStack.Count > 0)
                {
                    var (name,start, len) = propertyNameStack.Pop();
                    key = name;
                    keyStartByte = start;
                    keyEndByte = start + len;
                }
            }

            int valStartByte = (int)reader.TokenStartIndex;
            int valEndByte = valStartByte + reader.ValueSpan.Length;

            ITextAnchor keyStart = CreateAnchor(buffer, sparseIndex, Document, keyStartByte);
            ITextAnchor keyEnd = CreateAnchor(buffer, sparseIndex, Document, keyEndByte);

            ITextAnchor valStart = CreateAnchor(buffer, sparseIndex, Document, valStartByte);
            ITextAnchor valEnd = CreateAnchor(buffer, sparseIndex, Document, valEndByte);

            anchors.Add(new KeyValueAnchors
            {
                Key = key,
                IsContainer = false,
                KeyStart = keyStart,
                KeyEnd = keyEnd,
                ValueStart = valStart,
                ValueEnd = valEnd
            });
        }

        private void ProcessContainerStart(
            ref Utf8JsonReader reader,
            byte[] buffer,
            TextDocument Document,
            int[] sparseIndex,
            List<KeyValueAnchors> anchors,
            Stack<(int startByte, bool isObject)> containerStack,
            Stack<KeyValueAnchors> pendingContainerAnchors,
            Stack<(string name, int StartByte, int Length)> propertyNameStack,
            bool isObject)
        {
            int containerStartByte = (int)reader.TokenStartIndex;
            int keyStartByte = 0, keyEndByte = 0;
            string key = "";

            // 只有父容器为对象时才使用始末属性
            if (containerStack.Count > 0)
            {
                var (_, parentIsObject) = containerStack.Peek();
                if (parentIsObject && propertyNameStack.Count > 0)
                {
                    var (name,start, len) = propertyNameStack.Pop();
                    key = name;
                    keyStartByte = start;
                    keyEndByte = start + len;
                }
            }

            // 创建值起始锚点，结束锚点先用占位
            ITextAnchor valStart = CreateAnchor(buffer, sparseIndex, Document, containerStartByte);
            ITextAnchor valEnd = Document.CreateAnchor(0); // 临时占位，End 时将更新

            // 创建键锚点
            ITextAnchor keyStart = CreateAnchor(buffer, sparseIndex, Document, keyStartByte);
            ITextAnchor keyEnd = CreateAnchor(buffer, sparseIndex, Document, keyEndByte);

            var containerAnchor = new KeyValueAnchors
            {
                Key = key,
                IsArray = !isObject,
                IsContainer = true,
                KeyStart = keyStart,
                KeyEnd = keyEnd,
                ValueStart = valStart,
                ValueEnd = valEnd          // 临时
            };

            // 立即加入列表，保证父节点在子节点之前
            anchors.Add(containerAnchor);

            // 记录到待闭合栈，以便在 End 时修正 ValueEnd
            pendingContainerAnchors.Push(containerAnchor);
            containerStack.Push((containerStartByte, isObject));
        }

        private void ProcessContainerEnd(
            ref Utf8JsonReader reader,
            byte[] buffer,
            TextDocument Document,
            int[] sparseIndex,
            List<KeyValueAnchors> anchors,
            Stack<(int startByte, bool isObject)> containerStack,
            Stack<KeyValueAnchors> pendingContainerAnchors)
        {
            var (startByte, _) = containerStack.Pop();
            int endByte = (int)reader.TokenStartIndex + 1/*(int)reader.BytesConsumed*/;

            // 取出待修正的锚点对象，直接更新其 ValueEnd
            var containerAnchor = pendingContainerAnchors.Pop();
            containerAnchor.ValueEnd = CreateAnchor(buffer, sparseIndex, Document, endByte);
        }

        /// <summary>
        /// 稀疏字节→字符索引
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private int[] BuildSparseIndex(byte[] buffer)
        {
            var sparse = new int[buffer.Length / SamplingInterval + 2];
            int charCount = 0;
            int sampleIdx = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (i % SamplingInterval == 0)
                    sparse[sampleIdx++] = charCount;
                if ((buffer[i] & 0xC0) != 0x80) charCount++;
            }
            sparse[sampleIdx] = charCount;
            return sparse;
        }

        private int GetCharOffset(byte[] buffer, int[] sparseIndex, TextDocument Document, int byteOffset)
        {
            if (byteOffset <= 0) return 0;
            if (byteOffset >= buffer.Length) return Document.TextLength;

            int sampleIdx = byteOffset / SamplingInterval;
            int startByte = sampleIdx * SamplingInterval;
            int charOffset = sparseIndex[sampleIdx];

            for (int i = startByte; i < byteOffset; i++)
                if ((buffer[i] & 0xC0) != 0x80) charOffset++;

            return charOffset;
        }

        private TextAnchor CreateAnchor(byte[] buffer, int[] sparseIndex, TextDocument Document, int byteOffset)
        {
            int charOffset = GetCharOffset(buffer, sparseIndex, Document, byteOffset);
            var anchor = Document.CreateAnchor(charOffset);
            anchor.MovementType = AnchorMovementType.BeforeInsertion;
            return anchor;
        }
        #endregion
    }
}