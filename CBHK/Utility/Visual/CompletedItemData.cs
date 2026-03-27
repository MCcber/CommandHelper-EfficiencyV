using CBHK.CustomControl.Input;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace CBHK.Utility.Visual
{
    /// <summary>
    /// 补全成员属性
    /// </summary>
    public partial class CompletedItemData : ICompletionData
    {
        #region 字段
        /// <summary>
        /// 主程序状态更新委托
        /// </summary>
        public event Action<int> StatusUpdated;

        /// <summary>
        /// 是否补全了复合型选择器参数
        /// </summary>
        public bool IsCompoundSelectorParameter = false;

        public McfunctionIntellisenseDataContext CurrentContext = new();

        public ImageSource Image { get; set; }

        public object Content
        {
            get;
            set;
        } = "";

        public object Description
        {
            get;
            set;
        } = "";

        public string Text { get; set; } = "";

        public double Priority
        {
            get;
            set;
        }

        /// <summary>
        /// 截取当前文本时不能被匹配的字符串
        /// </summary>
        public List<string> DonotMatches = [",", "[", "{", "(", "=", " "];
        /// <summary>
        /// 截取当前文本时可被缺省的字符串
        /// </summary>
        public List<string> Matches = [".", ":", "@"];
        int DropStartOffset = 0;
        int DropEndOffset = 0;
        #endregion

        /// <summary>
        /// 排序逻辑
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(CompletedItemData other)
        {
            return Text.CompareTo(other!.Text);
        }

        /// <summary>
        /// 执行补全
        /// </summary>
        /// <param name="textArea"></param>
        /// <param name="completionSegment"></param>
        /// <param name="insertionRequestEventArgs"></param>
        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            #region 为坐标数据填补缺失的后导空格
            if ((Text.Contains('^') || Text.Contains('~')) && textArea.Document.GetText(completionSegment.Offset - 1, 1) != " ")
                textArea.Document.Insert(completionSegment.Offset, " ");
            #endregion

            #region 处理宏变量
            if (CurrentContext.CurrentCode.TrimStart().StartsWith('$'))
            {
                if (DropEndOffset - DropStartOffset != 0)
                {
                    textArea.Document.Replace(DropStartOffset, DropEndOffset - DropStartOffset, "$(" + Text + ")");
                    StatusUpdated?.Invoke(DropStartOffset + Text.Length + 3);
                }
                else
                {
                    textArea.Document.Replace(completionSegment, "$(" + Text + ")");
                    StatusUpdated?.Invoke(0);
                }
                return;
            }
            #endregion

            #region 处理普通补全
            if (CurrentContext is not null && CurrentContext.TypingContent.Length > 0)
                textArea.Document.Replace(textArea.Caret.Offset - CurrentContext.TypingContent.Length, CurrentContext.TypingContent.Length, Text);
            else
                textArea.Document.Replace(completionSegment, Text);
            #endregion

            #region 若在补全括号则光标后退一个单位
            if (IsCompoundSelectorParameter)
            {
                textArea.Caret.Offset--;
            }
            #endregion

            //执行回调方法
            StatusUpdated?.Invoke(0);
        }
    }
}
