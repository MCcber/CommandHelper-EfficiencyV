using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace CBHK.CustomControl
{
    /// <summary>
    /// 自动补全数据上下文
    /// </summary>
    [Serializable]
    public class McfunctionIntellisenseDataContext
    {
        /// <summary>
        /// 当前上下文的唯一标记码
        /// </summary>
        public string Uid { get; set; } = Guid.NewGuid().ToString();
        /// <summary>
        /// 命令路径
        /// </summary>
        public string CommandPath { get; set; } = "";
        /// <summary>
        /// 需要补全宏变量
        /// </summary>
        public bool IsCompleteMacros { get; set; } = false;
        /// <summary>
        /// 需要补全坐标
        /// </summary>
        public bool IsCompletePos { get; set; } = false;
        /// <summary>
        /// 需要补全选择器参数
        /// </summary>
        public bool IsCompleteSelectorParameters { get; set; } = false;
        /// <summary>
        /// 需要补全选择器参数值
        /// </summary>
        public bool IsCompleteSelectorParameterValues { get; set; } = false;
        /// <summary>
        /// 需要补全大纲
        /// </summary>
        public bool IsCompleteOutline { get; set; } = false;
        /// <summary>
        /// 需要补全运行时变量
        /// </summary>
        public bool IsCompleteRuntimeVariable { get; set; } = false;

        #region 坐标数据
        public string CoordinateType { get; set; } = "";
        public int CoordinateCount { get; set; } = 0;
        #endregion

        /// <summary>
        /// 选择器内联上下文
        /// </summary>
        public string SelectorInlineContext { get; set; } = "";
        /// <summary>
        /// 保存正在键入的内容
        /// </summary>
        public string TypingContent { get; set; } = "";
        /// <summary>
        /// 抓取当前光标所在的文本
        /// </summary>
        public string CurrentCode { get; set; } = "";
        /// <summary>
        /// 当前游戏规则名
        /// </summary>
        public string CurrentGameRuleName { get; set; } = "";
        /// <summary>
        /// 计算光标对于当前命令上下文的偏移量
        /// </summary>
        public int CurrentCaretIndex { get; set; } = 0;
        /// <summary>
        /// 标记语法树是否已结束
        /// </summary>
        public bool IsCompletionOver { get; set; } = false;
        /// <summary>
        /// 记录当前行的索引
        /// </summary>
        public int CurrentLineIndex { get; set; } = 0;
        /// <summary>
        /// 执行了粘贴
        /// </summary>
        public bool RunningPaste { get; set; } = false;

        /// <summary>
        /// 是否需要计算数据
        /// </summary>
        public bool IsNeedCalculate { get; set; } = true;

        /// <summary>
        /// 需要被移除的宏定义行索引
        /// </summary>
        public List<int> RemoveMacroIndexes { get; set; } = [];

        /// <summary>
        /// 补全框与它的显示状态
        /// </summary>
        public bool IsCompletionWindowShowing { get; set; } = true;

        #region 编码变量及标记
        public bool TypingBossbarIdString { get; set; } = false;
        public string currentBossbarIdString { get; set; } = "";

        public bool TypingScoreboardVariable { get; set; } = false;
        public string currentScoreboardVariable { get; set; } = "";

        public bool TypingTriggerVariable { get; set; } = false;
        public string currentTriggerVariable { get; set; } = "";

        public bool TypingStorageIdString { get; set; } = false;
        public string currentStorageVariable { get; set; } = "";

        public bool TypingTagVariable { get; set; } = false;
        public string currentTagVariable { get; set; } = "";

        public bool TypingTeamNameString { get; set; } = false;
        public string currentTeamVariable { get; set; } = "";
        #endregion
    }

    #region 代码块折叠策略
    public class LineFoldingStrategy
    {
        /// <summary>
        /// 添加大纲
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="document"></param>
        /// <param name="startLine"></param>
        /// <param name="endLine"></param>
        public static void AddFolding(FoldingManager manager, TextDocument document, DocumentLine startLine, DocumentLine endLine)
        {
            #region 检查重复项
            bool Had = false;
            foreach (FoldingSection item in manager.AllFoldings)
            {
                if (item.StartOffset == startLine.Offset)
                {
                    Had = true;
                    break;
                }
            }
            if (Had)
                return;
            #endregion
            #region 处理大纲预览文本
            string previewText = document.GetText(startLine);
            previewText = previewText.Length < 9 ? "#region" : previewText[9..];
            if (previewText.Length > 50)
                previewText = previewText[0..50];
            #endregion
            #region 处理添加
            FoldingSection foldingSection = manager.CreateFolding(startLine.Offset, endLine.EndOffset);
            foldingSection.Title = previewText;
            #endregion
        }
    }
    #endregion

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

    /// <summary>
    /// 语法树节点类
    /// </summary>
    public class SyntaxTreeItem
    {
        public enum SyntaxTreeItemType
        {
            Literal,
            Reference,
            Radical,
            DataType,
            Redirect
        }

        /// <summary>
        /// 对应的键
        /// </summary>
        public string Key { get; set; } = "";

        /// <summary>
        /// 对应的文本
        /// </summary>
        public string Text { get; set; } = "";

        /// <summary>
        /// 文本的数据类型
        /// </summary>
        public SyntaxTreeItemType Type { get; set; }

        public string Description { get; set; } = "";

        /// <summary>
        /// 当前节点的子级
        /// </summary>
        public List<SyntaxTreeItem> Children { get; set; } = [];
    }

    /// <summary>
    /// 缺失坐标的数据
    /// </summary>
    public class IncompleteCoordinatesData
    {
        public enum Coordinate
        {
            Local,
            Relative,
            Absolute
        }

        public Coordinate CoordinateType { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// 用于在指定位置的文本下方渲染波浪线
    /// </summary>
    /// <param name="textView">目标文本视图</param>
    /// <param name="startOffset"></param>
    /// <param name="endOffset"></param>
    /// <param name="underlineColor">波浪线颜色</param>
    public class WaveUnderlineBackgroundRenderer(int startOffset, int endOffset, SolidColorBrush underlineColor) : IBackgroundRenderer
    {
        public KnownLayer Layer => KnownLayer.Selection;

        private readonly int startOffset = startOffset;
        private readonly int endOffset = endOffset;
        private readonly SolidColorBrush underlineColor = underlineColor;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            textView.EnsureVisualLines();
            var visualLines = textView.VisualLines;
            if (visualLines.Count == 0)
                return;

            var startPoint = textView.GetVisualPosition(new TextViewPosition(textView.Document.GetLocation(startOffset)), VisualYPosition.LineBottom) - textView.ScrollOffset;
            var endPoint = textView.GetVisualPosition(new TextViewPosition(textView.Document.GetLocation(endOffset)), VisualYPosition.LineBottom) - textView.ScrollOffset;

            Pen pen = new(underlineColor, 1);
            double offset = 2.5;
            double waveLength = offset * 2;
            StreamGeometry geometry = new();
            using (StreamGeometryContext ctx = geometry.Open())
            {
                double x = startPoint.X;

                while (x < endPoint.X)
                {
                    double nextX = x + waveLength; // 下一个波段的结束x坐标
                    if (nextX > endPoint.X)
                    {
                        nextX = endPoint.X; //如果下一个波段会超过endPoint.X，调整它，使得不会绘制过界
                    }

                    double midX = (x + nextX) / 2; // 当前波段的中点x坐标

                    // 绘制从当前开始点到下一个波段的顶点
                    ctx.BeginFigure(new Point(x, startPoint.Y), false, false);
                    ctx.QuadraticBezierTo(
                        new Point(midX, startPoint.Y - offset), // 波谷在中点上方offset个单位
                        new Point(nextX, startPoint.Y), true, false);

                    x = nextX; // 更新x到下一个波段的开始点
                }
            }

            geometry.Freeze();
            drawingContext.DrawGeometry(null, pen, geometry);
        }
    }

    /// <summary>
    /// 用于在指定位置的文本下方渲染虚线
    /// </summary>
    /// <param name="startOffset"></param>
    /// <param name="endOffset"></param>
    /// <param name="underlineColor"></param>
    public class DotUnderlineBackgroundRenderer(TextLocation textLocation, int length, SolidColorBrush underlineColor) : IBackgroundRenderer
    {
        public KnownLayer Layer => KnownLayer.Selection;

        private readonly SolidColorBrush underlineColor = underlineColor;
        private readonly TextLocation textLocation = textLocation;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            textView.EnsureVisualLines();
            var visualLines = textView.VisualLines;
            if (visualLines.Count == 0)
                return;

            // 创建一个TextViewPosition对象
            TextViewPosition position = new(textLocation);
            Point coordinates = textView.GetVisualPosition(position, VisualYPosition.LineMiddle);
            coordinates.Y += 6;
            coordinates.X += 5;

            Pen pen = new(underlineColor, 1);
            for (int i = 0; i < length; i++)
            {
                drawingContext.DrawEllipse(underlineColor, pen, coordinates, 0.5, 0.5);
                coordinates.X += 5;
            }
        }
    }

    /// <summary>
    /// 在指定位置之后渲染文本水印
    /// </summary>
    /// <param name="textToRender"></param>
    /// <param name="startLocation"></param>
    /// <param name="typeface"></param>
    /// <param name="fontSize"></param>
    /// <param name="foregroundBrush"></param>
    public class WaterMarkRenderer(string textToRender, TextLocation startLocation, Typeface typeface, double fontSize, SolidColorBrush foregroundBrush) : IBackgroundRenderer
    {
        public KnownLayer Layer => KnownLayer.Selection; // Draw on the selection layer

        private readonly string textToRender = textToRender;
        private readonly TextLocation startLocation = startLocation;
        private readonly Typeface typeface = typeface;
        private readonly double fontSize = fontSize;
        private readonly SolidColorBrush foregroundBrush = foregroundBrush;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (textView is null || textView.VisualLinesValid == false)
                return;

            int startOffset = textView.Document.GetOffset(startLocation.Line, startLocation.Column);
            //int endOffset = startOffset + textToRender.Length;
            VisualLine vl = textView.GetVisualLine(startLocation.Line);

            if (vl is null) return;

            int visualColumn = vl.GetVisualColumn(startOffset);
            double topLeft = vl.GetTextLineVisualXPosition(vl.TextLines[0], visualColumn);

            FormattedText formattedText = new(
                textToRender,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                foregroundBrush, 1);

            drawingContext.DrawText(formattedText, new Point(topLeft, vl.VisualTop - textView.ScrollOffset.Y));
        }
    }

    public class MacroItem
    {
        public string Text { get; set; } = "";
        public string Description { get; set; } = "";
    }
}
