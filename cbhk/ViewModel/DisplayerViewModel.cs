using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;
using System;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CBHK.CustomControls;
using System.Windows.Media.Imaging;
using System.IO;

namespace CBHK.ViewModel
{
    public class DisplayerViewModel:ObservableObject
    {
        #region Field
        SolidColorBrush tranparentBrush = new((Color)ColorConverter.ConvertFromString("Transparent"));
        SolidColorBrush blackBrush = new((Color)ColorConverter.ConvertFromString("#000000"));
        SolidColorBrush grayBrush = new((Color)ColorConverter.ConvertFromString("#484848"));
        SolidColorBrush whiteBrush = new((Color)ColorConverter.ConvertFromString("#FFFFFF"));
        string fontFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\Fonts\\SourceCodePro-Medium\\SourceCodePro-Medium.ttf";
        FontFamily fontFamily = null;
        private TabControl ResultTabControl = null;
        #endregion

        #region Method
        /// <summary>
        /// 单例模式,用于显示生成结果
        /// </summary>
        public DisplayerViewModel()
        {
            fontFamily = new FontFamily(new Uri(fontFilePath, UriKind.Absolute), "Source Code Pro Medium");
        }
        #endregion

        #region Event
        public void ResultTabControl_Loaded(object sender, RoutedEventArgs e) => ResultTabControl = sender as TabControl;

        /// <summary>
        /// 生成结果
        /// </summary>
        /// <param name="Overlying">是否覆盖</param>
        /// <param name="spawnResult">数据集</param>
        /// <param name="headerText">数据头</param>
        /// <param name="head_image_pathes">所表示的生成器图标</param>
        public void GeneratorResult(string spawnResult, string headerText, string headImagePath)
        {
            #region 遍历当前标签页对象，查找是否有相同类型的
            bool ExistSamePage = false;
            RichTabItems currentTabItem = null;
            foreach (RichTabItems tab in ResultTabControl.Items)
            {
                ExistSamePage = tab.Header.ToString() == headerText;
                if (ExistSamePage)
                {
                    currentTabItem = tab;
                    break;
                }
            }
            #endregion

            #region 处理新加入的数据
            ToolTip toolTip = new()
            {
                Foreground = whiteBrush,
                Content = "点击复制",
                Background = grayBrush
            };
            //如果不存在相同标签页
            if (!ExistSamePage)
            {
                EnabledFlowDocument flowDocument = new() { LineHeight = 1 };
                #region 显示生成器图标
                Paragraph firstParagraph = new() { FontSize = 15, TextAlignment = TextAlignment.Center };
                firstParagraph.Inlines.Add(new Run("------------ "));
                if (File.Exists(headImagePath) || (headImagePath.StartsWith("pack") && Application.GetResourceStream(new Uri(headImagePath, UriKind.RelativeOrAbsolute)) is not null))
                    firstParagraph.Inlines.Add(new Image()
                    {
                        Source = new BitmapImage(new Uri(headImagePath, UriKind.RelativeOrAbsolute)),
                        Width = 20,
                        Height = 20
                    });
                firstParagraph.Inlines.Add(new Run(" ------------"));
                #endregion
                Paragraph paragraph = new() { TextAlignment = TextAlignment.Left };
                Paragraph splitParagraph = new() { TextAlignment = TextAlignment.Center };
                Run splitRun = new("---------- " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ----------");
                splitParagraph.Inlines.Add(splitRun);
                Run newResult = new() { ToolTip = toolTip, Text = spawnResult, FontFamily = fontFamily, Cursor = Cursors.Hand };
                ToolTipService.SetBetweenShowDelay(newResult, 0);
                ToolTipService.SetInitialShowDelay(newResult, 0);
                newResult.MouseEnter += (a, b) =>
                {
                    Run run = a as Run;
                    run.TextDecorations = TextDecorations.Baseline;
                };
                newResult.MouseLeave += (a, b) =>
                {
                    Run run = a as Run;
                    run.TextDecorations = null;
                };
                newResult.MouseLeftButtonDown += (a, b) => { Clipboard.SetText(spawnResult); };
                paragraph.Inlines.Add(newResult);
                flowDocument.Blocks.Add(firstParagraph);
                flowDocument.Blocks.Add(paragraph);
                flowDocument.Blocks.Add(splitParagraph);
                RichTextBox result_box = new()
                {
                    IsReadOnly = true,
                    Document = flowDocument,
                    FontFamily = fontFamily,
                    BorderBrush = blackBrush,
                    BorderThickness = new(0),
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                    Background = tranparentBrush,
                    FontSize = 15,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    CaretBrush = new SolidColorBrush(Colors.White)
                };
                ScrollViewer.SetVerticalScrollBarVisibility(result_box, ScrollBarVisibility.Auto);
                ScrollViewer.SetHorizontalScrollBarVisibility(result_box, ScrollBarVisibility.Disabled);

                RichTabItems itt = new()
                {
                    IsContentSaved = true,
                    Content = result_box,
                    Header = headerText,
                    BorderThickness = new(4, 4, 4, 0),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
                    SelectedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC6B23")),
                    Foreground = new SolidColorBrush(Colors.White),
                    Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                    LeftBorderTexture = Application.Current.Resources["TabItemLeft"] as Brush,
                    RightBorderTexture = Application.Current.Resources["TabItemRight"] as Brush,
                    TopBorderTexture = Application.Current.Resources["TabItemTop"] as Brush,
                    SelectedLeftBorderTexture = Application.Current.Resources["SelectedTabItemLeft"] as Brush,
                    SelectedRightBorderTexture = Application.Current.Resources["SelectedTabItemRight"] as Brush,
                    SelectedTopBorderTexture = Application.Current.Resources["SelectedTabItemTop"] as Brush,
                };
                ResultTabControl.Items.Add(itt);
                ResultTabControl.SelectedItem = itt;
            }
            else//如果存在相同标签页
            {
                RichTextBox richTextBox = currentTabItem.Content as RichTextBox;
                Paragraph firstParagraph = richTextBox.Document.Blocks.ElementAt(0) as Paragraph;
                #region 用于分割的段落
                Paragraph splitParagraph = new() { FontSize = 15, TextAlignment = TextAlignment.Center };
                Run splitRun = new("---------- " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ----------");
                splitParagraph.Inlines.Add(splitRun);
                #endregion
                Paragraph newParagraph = new() { TextAlignment = TextAlignment.Left };
                Run newResult = new() { ToolTip = toolTip, FontFamily = fontFamily, Cursor = Cursors.Hand, Text = spawnResult };
                newResult.MouseLeftButtonDown += (a, b) => { Clipboard.SetText(spawnResult); };
                newResult.MouseEnter += (a, b) =>
                {
                    Run run = a as Run;
                    run.TextDecorations = TextDecorations.Baseline;
                };
                newResult.MouseLeave += (a, b) =>
                {
                    Run run = a as Run;
                    run.TextDecorations = null;
                };
                ToolTipService.SetBetweenShowDelay(newResult, 0);
                ToolTipService.SetInitialShowDelay(newResult, 0);
                newParagraph.Inlines.Add(newResult);
                richTextBox.Document.Blocks.InsertAfter(firstParagraph, newParagraph);
                richTextBox.Document.Blocks.InsertAfter(newParagraph, splitParagraph);
            }
            #endregion
        }

        /// <summary>
        /// 隐藏窗体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Displayer_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            (sender as Window).Hide();
        }
        #endregion
    }
}
