// 派生自 AvalonEdit 的 TextEditor：生成器页面直接用它，就自带 JSON 感知编辑能力。
// 例：<jsonText:JsonTextEditor x:Name="Editor" />  或  editor.Reload(text);
using System;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using CBHK.Utility.Data.Mcdoc;

namespace CBHK.Utility.Data.JsonText
{
    public class JsonTextEditor : TextEditor
    {
        private JsonTextState state;

        public JsonTextEditor()
        {
            Document ??= new TextDocument();
        }

        /// <summary>当前文档的 JSON 状态（解析结果 + 锚点表）。</summary>
        public JsonTextState State => state ??= CreateState();

        /// <summary>状态层不认识控件：光标由编辑器注入，整篇回写后按节点映射放回去。</summary>
        private JsonTextState CreateState()
        {
            var created = new JsonTextState(Document ??= new TextDocument())
            {
                CaretOffsetProvider = () => CaretOffset,
                CaretOffsetRestorer = offset => CaretOffset = offset,
            };
            return created;
        }

        public McdocJsonValue Root => State.Root;

        /// <summary>
        /// 换一代文本：原地更新 State（换实例会让外部持有者继续写在旧代上），
        /// 文本落地与重解析在同一次调用里完成。
        /// </summary>
        public void Reload(string text) => State.Reload(text);

        /// <summary>物理整篇写回次数（诊断 / 门禁用）。</summary>
        public int WholeWriteCount => State.WholeWriteCount;

        /// <summary>按节点改值：只替换该节点的文本片段，其余内容与光标位置不受影响。</summary>
        public void ReplaceValue(McdocJsonValue node, McdocJsonValue newValue) => State.ReplaceValue(node, newValue);

        public void RemoveProperty(McdocJsonValue node) => State.RemoveProperty(node);

        public void Reformat(int indentSize = 2) => State.Reformat(indentSize);

        public TextAnchor CreateCaretAnchor(int offset) => State.CreateCaretAnchor(offset);
    }
}
