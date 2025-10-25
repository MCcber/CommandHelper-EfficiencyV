using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Input;
using System.Text;
using CBHK.Utility.Common;
using Prism.Events;
using CBHK.Model.Common;
using Newtonsoft.Json.Linq;

namespace CBHK.CustomControl
{
    /// <summary>
    /// StylizedTextBox.xaml 的交互逻辑
    /// </summary>
    public partial class StylizedTextBox : UserControl, Interface.IComponent
    {
        private int currentVersion = 1202;
        public int CurrentVersion
        {
            get => currentVersion;
            set
            {
                currentVersion = value;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsPresetMode = CurrentVersion < 1130;
                });
            }
        }

        public bool IsPresetMode
        {
            get { return (bool)GetValue(IsPresetModeProperty); }
            set
            {
                SetValue(IsPresetModeProperty, value);
                colorPicker.IsPresetColorMode = IsPresetMode;
            }
        }

        private List<RichRun> RichRunList = [];

        public IEventAggregator EventAggregator { get;set; }

        public RemoveComponentEvent RemoveComponentEvent { get;set; }

        public string ExternFilePath { get; set; }
        public JToken ExternallyData { get; set; }
        public bool ImportMode { get; set; }
        public StringBuilder Result { get; set; }
        public string Version { get; set; }
        public string TargetVersion { get; init; }
        public bool IsContainer { get; set; }
        public List<Interface.IComponent> Children { get; set; }

        public static readonly DependencyProperty IsPresetModeProperty =
            DependencyProperty.Register("IsPresetMode", typeof(bool), typeof(StylizedTextBox), new PropertyMetadata(default(bool)));

        public bool IsMultiLine = false;

        public StylizedTextBox()
        {
            InitializeComponent();
            colorPicker.IsPresetColorMode = IsPresetMode;
        }

        /// <summary>
        /// 设置被选择文本的字体颜色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SetSelectionColor(object sender, PropertyChangedEventArgs e)
        {
            TextRange textRange = new(richTextBox.Selection.Start, richTextBox.Selection.End);
            textRange.ApplyPropertyValue(TextBlock.ForegroundProperty, colorPicker.SelectColor);
        }

        private void SetBold_Click(object sender, RoutedEventArgs e)
        {
            if (richTextBox.Selection is null || richTextBox.Selection.Text.Length == 0)
                return;
            TextRange textRange = new(richTextBox.Selection.Start, richTextBox.Selection.End);
            if (Equals(textRange.GetPropertyValue(TextBlock.FontWeightProperty), FontWeights.Normal))
                textRange.ApplyPropertyValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            else
                textRange.ApplyPropertyValue(TextBlock.FontWeightProperty, FontWeights.Normal);
        }

        private void SetItalic_Click(object sender, RoutedEventArgs e)
        {
            if (richTextBox.Selection is null)
                return;
            if (richTextBox.Selection.Text.Length == 0)
                return;
            TextRange textRange = new(richTextBox.Selection.Start, richTextBox.Selection.End);
            if (Equals(textRange.GetPropertyValue(TextBlock.FontStyleProperty), FontStyles.Normal))
                textRange.ApplyPropertyValue(TextBlock.FontStyleProperty, FontStyles.Italic);
            else
                textRange.ApplyPropertyValue(TextBlock.FontStyleProperty, FontStyles.Normal);
        }

        private void SetUnderlined_Click(object sender, RoutedEventArgs e)
        {
            if (richTextBox.Selection is null)
                return;
            if (richTextBox.Selection.Text.Length == 0)
                return;
            TextRange textRange = new(richTextBox.Selection.Start, richTextBox.Selection.End);

            if (textRange.GetPropertyValue(TextBlock.TextDecorationsProperty) is TextDecorationCollection current_decorations)
            {
                int strikethrough_index = current_decorations.IndexOf(TextDecorations.Strikethrough.First());
                if (!current_decorations.Contains(TextDecorations.Baseline.First()))
                {
                    TextDecorationCollection textDecorations =
                    [
                        .. strikethrough_index != -1 ? TextDecorations.Strikethrough : [],
                        TextDecorations.Baseline[0],
                    ];
                    textRange.ApplyPropertyValue(TextBlock.TextDecorationsProperty, textDecorations);
                }
                else
                {
                    if (strikethrough_index != -1)
                        textRange.ApplyPropertyValue(TextBlock.TextDecorationsProperty, TextDecorations.Strikethrough);
                    else
                        textRange.ApplyPropertyValue(TextBlock.TextDecorationsProperty, new TextDecorationCollection());
                }
            }
            else
                textRange.ApplyPropertyValue(TextBlock.TextDecorationsProperty, TextDecorations.Baseline);
        }

        private void SetStrikethrough_Click(object sender, RoutedEventArgs e)
        {
            if (richTextBox.Selection is null)
                return;
            if (richTextBox.Selection.Text.Length == 0)
                return;
            TextRange textRange = new(richTextBox.Selection.Start, richTextBox.Selection.End);

            if (textRange.GetPropertyValue(TextBlock.TextDecorationsProperty) is TextDecorationCollection current_decorations)
            {
                int underline_index = current_decorations.IndexOf(TextDecorations.Baseline.First());
                if (!current_decorations.Contains(TextDecorations.Strikethrough.First()))
                {
                    TextDecorationCollection textDecorations = [.. underline_index != -1 ? TextDecorations.Baseline : [], TextDecorations.Strikethrough[0]];
                    textRange.ApplyPropertyValue(TextBlock.TextDecorationsProperty, textDecorations);
                }
                else
                {
                    if (underline_index != -1)
                        textRange.ApplyPropertyValue(TextBlock.TextDecorationsProperty, TextDecorations.Baseline);
                    else
                        textRange.ApplyPropertyValue(TextBlock.TextDecorationsProperty, new TextDecorationCollection());
                }
            }
            else
                textRange.ApplyPropertyValue(TextBlock.TextDecorationsProperty, TextDecorations.Strikethrough);
        }

        private void SetObfuscated_Click(object sender, RoutedEventArgs e)
        {
            if (richTextBox.Selection is null) return;
            if (richTextBox.Selection.Text.Length == 0) return;

            if (richTextBox.Selection.Start.Paragraph is not RichParagraph || richTextBox.Selection.End.Paragraph is not RichParagraph)
            {
                return;
            }
            if (richTextBox.Selection.Text.Length > 0)
                ObfuscateRunHelper.Run(ref richTextBox);
        }

        private void ResetStyle_Click(object sender, RoutedEventArgs e)
        {
            if (richTextBox.Selection.End.Parent is RichRun end_run && richTextBox.Selection.Start.Parent is RichRun start_run)
            {
                RichParagraph StartParagraph = start_run.Parent as RichParagraph;
                RichParagraph EndParagraph = end_run.Parent as RichParagraph;
                List<RichRun> StartRuns = [];
                StartRuns = StartParagraph.Inlines.ToList().ConvertAll(item => item as RichRun);
                int StartRunIndex = StartRuns.IndexOf(start_run);
                //移除不包括在内的对象
                if (StartRunIndex > 0)
                    StartRuns.RemoveRange(0, StartRunIndex);
                if (!Equals(StartParagraph, EndParagraph))
                {
                    FlowDocument page = StartParagraph.Parent as FlowDocument;
                    List<RichParagraph> paragraphs = page.Blocks.ToList().ConvertAll(item => item as RichParagraph);
                    int StartParagraphIndex = paragraphs.IndexOf(StartParagraph);
                    int EndParagrapghIndex = paragraphs.IndexOf(EndParagraph);
                    //表示两行相邻
                    if (EndParagrapghIndex - StartParagraphIndex == 1)
                    {
                        StartRuns.AddRange(EndParagraph.Inlines.ToList().ConvertAll(item => item as RichRun));

                    }//表示选中了不止两行
                    else
                    {
                        List<RichRun> EndRuns = [];
                        for (int i = StartParagraphIndex + 1; i <= EndParagrapghIndex; i++)
                        {
                            EndRuns = paragraphs[i].Inlines.ToList().ConvertAll(item => item as RichRun);
                            //最后一个段落只加行首到选区末尾处
                            if (i == EndParagrapghIndex)
                            {
                                int EndRunIndex = EndRuns.IndexOf(end_run);
                                EndRuns.RemoveRange(EndRunIndex + 1, EndRuns.Count - (EndRunIndex + 1));
                            }
                            StartRuns.AddRange(EndRuns);
                        }
                    }
                }
                else
                {
                    int EndRunIndex = StartRuns.IndexOf(end_run);
                    StartRuns.RemoveRange(EndRunIndex + 1, StartRuns.Count - (EndRunIndex + 1));
                }

                foreach (RichRun item in StartRuns)
                {
                    item.FontWeight = FontWeights.Normal;
                    item.FontStyle = FontStyles.Normal;
                    item.FontSize = 18;
                    item.TextDecorations = [];
                    item.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                    item.IsObfuscated = false;
                    item.ObfuscateTimer.IsEnabled = false;
                    if (item.UID.Trim() != "")
                        item.Text = item.UID;
                }
            }
        }

        private void CannotPressKey_KeyDown(object sender, KeyEventArgs e) => e.Handled = e.Key == Key.Enter && !IsMultiLine;

        public void UpdateVersion(string SelectedVersion)
        {
        }

        public StringBuilder Create()
        {
            StringBuilder Result = new();
            return Result;
        }

        public void CollectionData(StringBuilder Result)
        {
            Paragraph paragraph = richTextBox.Document.Blocks.FirstBlock as Paragraph;
            if (IsMultiLine)
            {
                foreach (var item in richTextBox.Document.Blocks.Cast<Paragraph>())
                {
                    RichRunList.AddRange(item.Inlines.Cast<RichRun>().ToList());
                }
            }
            else
            {
                RichRunList = paragraph.Inlines.Cast<RichRun>().ToList();
            }
        }

        public void Build(StringBuilder Result)
        {
            if (Name == "ItemLore" && CurrentVersion >= 1130)
            {
                Result.Append("'[");
            }
            foreach (var item in RichRunList)
            {
                item.CurrentVersion = CurrentVersion;
                string currentResult = item.Result;
                int commaIndex = currentResult.IndexOf(',');
                if (commaIndex > -1 && currentResult[(commaIndex - 4)..(commaIndex - 1)] == @"\\n" && Name == "ItemLore" && CurrentVersion >= 1130)
                    Result.Append(currentResult.TrimEnd(',').Remove(commaIndex - 4, 3) + "]','[");
                else
                    Result.Append(currentResult.TrimEnd('\n'));
            }
            if (Name == "ItemLore" && CurrentVersion >= 1130)
            {
                Result.Append("]'");
            }
            if (Result.ToString().EndsWith(",'[]'"))
            {
                Result.Remove(Result.Length - 5, 5);
            }
            if (Result.ToString() == "'[]'")
            {
                Result.Clear();
            }
            if (Result.Length > 0 && Result[^1] == ',')
            {
                Result.Length--;
            }
        }
    }
}