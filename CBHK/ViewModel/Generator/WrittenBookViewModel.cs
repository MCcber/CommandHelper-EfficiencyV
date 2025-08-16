using CBHK.CustomControl;
using CBHK.CustomControl.ColorPickerComponents;
using CBHK.GeneralTool;
using CBHK.GeneralTool.MessageTip;
using CBHK.View;
using CBHK.View.Compoment.WrittenBook;
using CBHK.WindowDictionaries;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using EditPage = CBHK.View.Compoment.WrittenBook.EditPage;
using Image = System.Windows.Controls.Image;

namespace CBHK.ViewModel.Generator
{
    public partial class WrittenBookViewModel(IContainerProvider container,MainView mainView) : ObservableObject
    {
        #region Field
        //成书编辑框引用
        public RichTextBox WrittenBookEditor = null;

        //成书背景文件路径
        string backgroundFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\WrittenBook\Image\written_book_background.png";
        //左箭头背景文件路径
        string leftArrowFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\WrittenBook\Image\left_arrow.png";
        //右箭头背景文件路径
        string rightArrowFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\WrittenBook\Image\right_arrow.png";
        //左箭头高亮背景文件路径
        string leftArrowHightLightFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\WrittenBook\Image\enter_left_arrow.png";
        //右箭头高亮背景文件路径
        string rightArrowHightLightFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\WrittenBook\Image\enter_right_arrow.png";
        //署名按钮背景文件路径
        string signatureFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\WrittenBook\Image\signature_button.png";
        //署名背景文件路径
        string signatureBackgroundFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\WrittenBook\Image\signature_page.png";
        //署名并关闭背景文件路径
        string signatureAndCloseFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\WrittenBook\Image\setting_signature.png";
        //署名完毕背景文件路径
        string sureToSignatureFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\WrittenBook\Image\sure_to_signature.png";
        //取消署名背景文件路径
        string signatureCancelFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\WrittenBook\Image\cancel_signature.png";
        //混淆文本配置文件路径
        string obfuscateFilePath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\WrittenBook\Data\obfuscateChars.ini";
        //流文档链表,每个成员代表成书中的一页
        public List<EnabledFlowDocument> WrittenBookPages = [];

        /// <summary>
        /// 一页总字节数
        /// </summary>
        int PageMaxByteLength = 1024;
        int PageMaxLineCount = 7;

        //字符数量超出提示
        TextBlock ExceedsBlock = null;
        /// <summary>
        /// 主页引用
        /// </summary>
        private Window home = mainView;

        /// <summary>
        /// 事件设置控件
        /// </summary>
        TextEvent EventComponent = new();
        //本生成器的图标路径
        string iconPath = "pack://application:,,,/CBHK;component/Resource/Common/Image/SpawnerIcon/IconWrittenBook.png";

        //当前光标选中的文本对象链表
        List<RichRun> CurrentSelectedRichRunList = [];

        //作为内部工具被调用
        public bool AsInternalTool = false;

        /// <summary>
        /// 保存最终结果
        /// </summary>
        public string result = "";

        /// <summary>
        /// 保存最终结果的首个json对象
        /// </summary>
        public string object_result = "";

        //保存成书的文档链表，用于显示和编辑
        public List<EnabledFlowDocument> HistroyFlowDocumentList = [];
        #endregion

        #region 版本数据源与已选择版本
        public ObservableCollection<TextComboBoxItem> VersionSource { get; set; } = [
            new TextComboBoxItem() { Text = "1.20.5+" },
            new TextComboBoxItem() { Text = "1.12" }
            ];
        private TextComboBoxItem selectedVersion;

        public TextComboBoxItem SelectedVersion
        {
            get => selectedVersion;
            set
            {
                SetProperty(ref selectedVersion, value);
                CurrentMinVersion = int.Parse(SelectedVersion.Text.Replace(".", "").Replace("+", "").Split('-')[0]);
            }
        }
        private int CurrentMinVersion = 1205;
        #endregion

        #region 显示字符超出数量
        private Visibility displayExceedsCount = Visibility.Collapsed;
        public Visibility DisplayExceedsCount
        {
            get { return displayExceedsCount; }
            set
            {
                displayExceedsCount = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region 总页数
        int maxPage = 1;
        int MaxPage
        {
            get => maxPage;
            set => SetProperty(ref maxPage,value);
        }
        #endregion

        #region 当前页码下标
        int currentPageIndex = 0;
        int CurrentPageIndex
        {
            get => currentPageIndex;
            set
            {
                SetProperty(ref currentPageIndex,value);
                MaxPage = WrittenBookPages.Count;
                PageData = "页面 ：" + (currentPageIndex + 1).ToString() + "/" + MaxPage.ToString();
                if (currentPageIndex > 0)
                    DisplayLeftArrow = Visibility.Visible;
                else
                    DisplayLeftArrow = Visibility.Collapsed;
            }
        }
        #endregion

        #region 当前光标所在的文本对象引用
        private RichRun currentRichRun = null;
        public RichRun CurrentRichRun
        {
            get => currentRichRun;
            set => SetProperty(ref currentRichRun,value);
        }
        #endregion

        #region 开启点击事件
        private bool enableClickEvent = false;
        public bool EnableClickEvent
        {
            get => enableClickEvent;
            set => SetProperty(ref enableClickEvent,value);
        }
        #endregion

        #region 开启悬浮事件
        private bool enableHoverEvent = false;
        public bool EnableHoverEvent
        {
            get { return enableHoverEvent; }
            set
            {
                enableHoverEvent = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region 开启插入
        private bool enableInsertion = false;
        public bool EnableInsertion
        {
            get { return enableInsertion; }
            set
            {
                enableInsertion = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region 拾色器
        ColorPickers colorPicker = null;
        #endregion

        #region 被选择文本的字体颜色
        private SolidColorBrush selectionColor = new(Color.FromRgb(0,0,0));
        public SolidColorBrush SelectionColor
        {
            get => selectionColor;
            set => SetProperty(ref selectionColor,value);
        }
        #endregion

        #region 当前页码与总页数数据
        private string pageData = "页面 ：1/1";
        public string PageData
        {
            get { return pageData; }
            set { pageData = value; OnPropertyChanged(); }
        }
        #endregion

        #region 是否显示左箭头
        private Visibility displayLeftArrow = Visibility.Collapsed;
        public Visibility DisplayLeftArrow
        {
            get { return displayLeftArrow; }
            set
            {
                displayLeftArrow = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region 是否显示右箭头
        private Visibility displayRightArrow = Visibility.Visible;
        public Visibility DisplayRightArrow
        {
            get { return displayRightArrow; }
            set
            {
                displayRightArrow = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region 显示结果
        private bool showGeneratorResult = false;
        public bool ShowGeneratorResult
        {
            get => showGeneratorResult;
            set => SetProperty(ref showGeneratorResult, value);
        }
        #endregion

        #region 跳转页面数字
        private int jumpSpecificPageNumber;

        public int JumpSpecificPageNumber
        {
            get => jumpSpecificPageNumber;
            set => SetProperty(ref jumpSpecificPageNumber, value);

        }

        #endregion

        #region 悬浮菜单
        Popup popup = new()
        {
            IsOpen = false,
            Placement = PlacementMode.AbsolutePoint,
            StaysOpen = false
        };
        #endregion

        #region 初始化两个页面
        Frame PageFrame = new() { NavigationUIVisibility = NavigationUIVisibility.Hidden };
        EditPage editPage = new();
        SignaturePage signaturePage = new();
        private IContainerProvider _container = container;
        #endregion

        /// <summary>
        /// 载入编辑页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PageFrame_Loaded(object sender, RoutedEventArgs e)
        {
            editPage.DataContext = signaturePage.DataContext = this;
            RichParagraph richParagraph = editPage.richTextBox.Document.Blocks.FirstBlock as RichParagraph;
            RichRun richRun = richParagraph.Inlines.FirstInline as RichRun;
            richRun.Foreground = Brushes.Black;
            ContentControl contentControl = sender as ContentControl;
            PageFrame.Content = editPage;
            contentControl.Content = PageFrame;
        }

        /// <summary>
        /// 按下回车后定位到指定页面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void JumpToSpecificPage_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                if(JumpSpecificPageNumber > 0 && JumpSpecificPageNumber <= WrittenBookPages.Count)
                {
                    WrittenBookEditor.Document = WrittenBookPages[JumpSpecificPageNumber - 1];
                    CurrentPageIndex = JumpSpecificPageNumber - 1;
                }
            }
        }

        /// <summary>
        /// 版本更换
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void Version_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await signaturePage.title.Upgrade(CurrentMinVersion);
            await signaturePage.author.Upgrade(CurrentMinVersion);
        }

        /// <summary>
        /// 异步获取标题
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetTitle()
        {
            string result = await signaturePage.title.Result();
            string quotation = CurrentMinVersion < 113 ? "\"" : "'";
            return "title:" + quotation + result.TrimEnd(',') + quotation + ",";
        }

        /// <summary>
        /// 异步获取作者
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetAuthor()
        {
            string result = await signaturePage.author.Result();
            string quotation = CurrentMinVersion < 113 ? "\"" : "'";
            return "author:" + quotation + result.TrimEnd(',') + quotation + ",";
        }

        [RelayCommand]
        /// <summary>
        /// 重置选中文本的所有属性
        /// </summary>
        private void ResetText()
        {
            if (WrittenBookEditor.Selection.End.Parent is RichRun endRun && WrittenBookEditor.Selection.Start.Parent is RichRun startRun)
            {
                RichParagraph StartParagraph = startRun.Parent as RichParagraph;
                RichParagraph EndParagraph = endRun.Parent as RichParagraph;
                List<RichRun> StartRuns = [];
                StartRuns = StartParagraph.Inlines.ToList().ConvertAll(item => item as RichRun);
                int StartRunIndex = StartRuns.IndexOf(startRun);
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
                                int EndRunIndex = EndRuns.IndexOf(endRun);
                                EndRuns.RemoveRange(EndRunIndex + 1, EndRuns.Count - (EndRunIndex + 1));
                            }
                            StartRuns.AddRange(EndRuns);
                        }
                    }
                }
                else
                {
                    int EndRunIndex = StartRuns.IndexOf(endRun);
                    StartRuns.RemoveRange(EndRunIndex + 1, StartRuns.Count - (EndRunIndex + 1));
                }

                foreach (RichRun item in StartRuns)
                {
                    item.FontWeight = FontWeights.Normal;
                    item.FontStyle = FontStyles.Normal;
                    item.TextDecorations = [];
                    item.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                    item.IsObfuscated = false;
                    item.ObfuscateTimer.IsEnabled = false;
                    if (item.UID.Trim() != "")
                        item.Text = item.UID;
                }
            }
        }

        [RelayCommand]
        /// <summary>
        /// 选中文本切换混淆文字
        /// </summary>
        private void ObfuscateText()
        {
            if (WrittenBookEditor.Selection is null) return;
            if (WrittenBookEditor.Selection.Text.Length == 0) return;

            //获取选区头部所在的文本块以及它所在段落中的索引
            if (WrittenBookEditor.Selection.Start.Paragraph is not RichParagraph || WrittenBookEditor.Selection.End.Paragraph is not RichParagraph)
            {
                return;
            }

            ObfuscateRunHelper.Run(ref WrittenBookEditor);
        }

        [RelayCommand]
        /// <summary>
        /// 选中文本切换删除线
        /// </summary>
        private void StrikethroughText()
        {
            if (WrittenBookEditor.Selection is null)
                return;
            if (WrittenBookEditor.Selection.Text.Length == 0)
                return;
            TextRange textRange = new TextRange(WrittenBookEditor.Selection.Start, WrittenBookEditor.Selection.End);
            TextDecorationCollection current_decorations = textRange.GetPropertyValue(TextBlock.TextDecorationsProperty) as TextDecorationCollection;

            if(current_decorations != null)
            {
                int underline_index = current_decorations.IndexOf(TextDecorations.Baseline.First());
                if (!current_decorations.Contains(TextDecorations.Strikethrough.First()))
                {
                    TextDecorationCollection textDecorations = [..underline_index != -1 ? TextDecorations.Baseline : [], TextDecorations.Strikethrough[0]];
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

        [RelayCommand]
        /// <summary>
        /// 选中文本切换下划线
        /// </summary>
        private void UnderlinedText()
        {
            if (WrittenBookEditor.Selection is null)
                return;
            if (WrittenBookEditor.Selection.Text.Length == 0)
                return;
            TextRange textRange = new(WrittenBookEditor.Selection.Start, WrittenBookEditor.Selection.End);

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

        [RelayCommand]
        /// <summary>
        /// 选中文本切换斜体
        /// </summary>
        private void ItalicText()
        {
            if (WrittenBookEditor.Selection is null)
                return;
            if (WrittenBookEditor.Selection.Text.Length == 0)
                return;
            TextRange textRange = new(WrittenBookEditor.Selection.Start, WrittenBookEditor.Selection.End);
            if (Equals(textRange.GetPropertyValue(TextBlock.FontStyleProperty), FontStyles.Normal))
                textRange.ApplyPropertyValue(TextBlock.FontStyleProperty, FontStyles.Italic);
            else
                textRange.ApplyPropertyValue(TextBlock.FontStyleProperty, FontStyles.Normal);
        }

        [RelayCommand]
        /// <summary>
        /// 选中文本切换粗体
        /// </summary>
        private void BoldText()
        {
            if (WrittenBookEditor.Selection is null || WrittenBookEditor.Selection.Text.Length == 0)
                return;
            TextRange textRange = new(WrittenBookEditor.Selection.Start, WrittenBookEditor.Selection.End);
            if (Equals(textRange.GetPropertyValue(TextBlock.FontWeightProperty), FontWeights.Normal))
                textRange.ApplyPropertyValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            else
                textRange.ApplyPropertyValue(TextBlock.FontWeightProperty, FontWeights.Normal);
        }

        [RelayCommand]
        /// <summary>
        /// 返回主页
        /// </summary>
        /// <param name="win"></param>
        private void Return(CommonWindow win)
        {
            home.WindowState = WindowState.Normal;
            home.Show();
            home.ShowInTaskbar = true;
            home.Focus();
            win.Close();
        }

        [RelayCommand]
        /// <summary>
        /// 执行生成
        /// </summary>
        private async Task Run()
        {
            if (!AsInternalTool)
            {
                //最终结果
                string Result = "give @p written_book";
                //合并所有页数据
                string pagesString = "pages:[";
                //每一页的数据
                string page_string = "";
                //遍历所有文档
                foreach (EnabledFlowDocument page in WrittenBookPages)
                {
                    List<Paragraph> page_content = page.Blocks.ToList().ConvertAll(item => item as Paragraph);
                    page_string += "'[\"\",";
                    for (int i = 0; i < page_content.Count; i++)
                    {
                        page_string += string.Join("", page_content[i].Inlines.ToList().ConvertAll(line => line as RichRun).Select(run =>
                        {
                            return run.Result;
                        }));
                    }
                    page_string = page_string.TrimEnd(',') + "]',";
                }
                pagesString += page_string.TrimEnd(',') + "]";
                pagesString = pagesString.Trim() == "pages:['[\"\"]']" || pagesString.Trim() == "pages:['[\"\",{\"text\":\"\"}]']" ? "" : pagesString;
                string NBTData = "";
                string TitleString = await GetTitle();
                string AuthorString = await GetAuthor();
                NBTData += TitleString + AuthorString + pagesString;
                NBTData = "{" + NBTData.TrimEnd(',') + "}";
                Result += (CurrentMinVersion < 1130 ? " 1 0 " : "") + NBTData;

                if(CurrentMinVersion < 1130)
                {
                    Result = @"give @p minecraft:sign 1 0 {BlockEntityTag:{Text1:""{\""text\"":\""右击执行\"",\""clickEvent\"":{\""action\"":\""run_command\"",\""value\"":\""/setblock ~ ~ ~ minecraft:command_block 0 replace {Command:\\\""" + Result.Replace(@"""",@"\\\\\\\""") + @"\\\""}\""}}""}}";
                }

                if (ShowGeneratorResult)
                {
                    DisplayerView displayer = _container.Resolve<DisplayerView>();
                    if (displayer is not null && displayer.DataContext is DisplayerViewModel displayerViewModel)
                    {
                        displayer.Show();
                        displayerViewModel.GeneratorResult(Result, "成书", iconPath);
                    }
                }
                else
                {
                    Clipboard.SetText(Result);
                    Message.PushMessage("成书生成成功！", MessageBoxImage.Information);
                }
            }
            else//作为内部工具被调用
            {
                //第一页的数据
                string page_string = "";
                //仅获取文档内容
                HistroyFlowDocumentList = WrittenBookPages;
                List<Paragraph> page_content = HistroyFlowDocumentList[0].Blocks.ToList().ConvertAll(item => item as Paragraph);
                page_string += "'[\"\",";
                for (int i = 0; i < page_content.Count; i++)
                {
                    page_string += string.Join("", page_content[i].Inlines.ToList().ConvertAll(line => line as RichRun).Select(run =>
                    {
                        return run.Result;
                    }));

                    if (i == 0)
                    {
                        object_result = string.Join("", page_content[0].Inlines.ToList().ConvertAll(line => line as RichRun).Select(run =>
                        {
                            return run.Result;
                        })).TrimEnd(',');
                    }
                }
                page_string = page_string.TrimEnd(',') + "]',";

                page_string = page_string.TrimEnd(',').Trim('\'');
                result = page_string;
                CommonWindow window = Window.GetWindow(WrittenBookEditor) as CommonWindow;
                window.DialogResult = true;
            }
        }

        /// <summary>
        /// 弹出悬浮菜单
        /// </summary>
        private void ShowHoverMenu()
        {
            bool SameRun = false;
            if (WrittenBookEditor.Selection.Text.Trim().Length > 0)
            {
                Point relativePoint = WrittenBookEditor.Selection.End.GetCharacterRect(LogicalDirection.Forward).TopLeft;
                Point absolutePoint = WrittenBookEditor.PointToScreen(relativePoint);
                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;
                bool isBottom = absolutePoint.Y > screenHeight / 2;
                bool isLeft = absolutePoint.X < screenWidth / 2;

                popup.HorizontalOffset = absolutePoint.X;
                popup.VerticalOffset = absolutePoint.Y;

                if (isBottom)
                {
                    // 显示在光标上方
                    popup.VerticalOffset -= 50;
                }
                else
                if (isLeft)
                {
                    // 显示在光标右侧
                    popup.HorizontalOffset += 50;
                }
                else
                {
                    popup.HorizontalOffset += 50;
                    popup.VerticalOffset -= 50;
                }

                #region 设置数据
                RichRun startRun = WrittenBookEditor.Selection.Start.Parent as RichRun;
                RichRun endRun = WrittenBookEditor.Selection.End.Parent as RichRun;
                if (startRun is Run)
                    startRun ??= new RichRun() { Text = startRun.Text };
                if (endRun is Run)
                    endRun ??= new RichRun() { Text = endRun.Text };
                if (endRun is null)
                {
                    Paragraph paragraph = WrittenBookEditor.Selection.Start.Paragraph;
                    List<Run> runs = paragraph.Inlines.Cast<Run>().ToList();
                    int startIndex = runs.IndexOf(startRun);
                    runs[startIndex] = new RichRun() { Text = startRun.Text };
                }
                SameRun = Equals(startRun, endRun);
                if (SameRun)
                {
                    #region 同步数据
                    CurrentRichRun = startRun;

                    #region 事件的开关
                    Binding HaveClickEventBinder = new()
                    {
                        Path = new PropertyPath("CurrentRichRun.HasClickEvent"),
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    };
                    Binding HaveHoverEventBinder = new()
                    {
                        Path = new PropertyPath("CurrentRichRun.HasHoverEvent"),
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    };
                    Binding HaveInsertionBinder = new()
                    {
                        Path = new PropertyPath("CurrentRichRun.HasInsertion"),
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    };
                    #endregion

                    #region 事件的类型和值
                    Binding ClickEventActionBinder = new()
                    {
                        Path = new PropertyPath("CurrentRichRun.ClickEventActionItem"),
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    };
                    Binding HoverEventActionBinder = new()
                    {
                        Path = new PropertyPath("CurrentRichRun.HoverEventActionItem"),
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    };

                    Binding ClickEventValueBinder = new()
                    {
                        Path = new PropertyPath("CurrentRichRun.ClickEventValue"),
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    };
                    Binding HoverEventValueBinder = new()
                    {
                        Path = new PropertyPath("CurrentRichRun.HoverEventValue"),
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    };
                    Binding InsertionValueBinder = new()
                    {
                        Path = new PropertyPath("CurrentRichRun.InsertionValue"),
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    };
                    #endregion

                    BindingOperations.SetBinding(EventComponent.EnableClickEvent, ToggleButton.IsCheckedProperty, HaveClickEventBinder);
                    BindingOperations.SetBinding(EventComponent.EnableHoverEvent, ToggleButton.IsCheckedProperty, HaveHoverEventBinder);
                    BindingOperations.SetBinding(EventComponent.EnableInsertion, ToggleButton.IsCheckedProperty, HaveInsertionBinder);

                    EventComponent.ClickEventPanel.Visibility = CurrentRichRun.HasClickEvent ? Visibility.Visible : Visibility.Collapsed;
                    EventComponent.HoverEventPanel.Visibility = CurrentRichRun.HasHoverEvent ? Visibility.Visible : Visibility.Collapsed;
                    EventComponent.InsertionPanel.Visibility = CurrentRichRun.HasInsertion ? Visibility.Visible : Visibility.Collapsed;

                    BindingOperations.SetBinding(EventComponent.ClickEventActionBox, Selector.SelectedItemProperty, ClickEventActionBinder);
                    BindingOperations.SetBinding(EventComponent.HoverEventActionBox, Selector.SelectedItemProperty, HoverEventActionBinder);

                    BindingOperations.SetBinding(EventComponent.ClickEventValueBox, TextBox.TextProperty, ClickEventValueBinder);
                    BindingOperations.SetBinding(EventComponent.HoverEventValueBox, TextBox.TextProperty, HoverEventValueBinder);
                    BindingOperations.SetBinding(EventComponent.InsertionValueBox, TextBox.TextProperty, InsertionValueBinder);
                    #endregion
                }
                else//选区首尾文本块不同则更新它们之间的所有文本块
                {

                }
                #endregion
            }
            //判断是否需要关闭悬浮菜单
            popup.StaysOpen = popup.IsOpen = WrittenBookEditor.Selection.Text.Trim().Length > 0 && SameRun;
        }

        /// <summary>
        /// 检测按回车键换行和粘贴
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void WrittenBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            #region 处理粘贴的数据,合并为RichRun以适配混淆效果
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.V)
            {
                RichRun richRun = WrittenBookEditor.CaretPosition.Parent as RichRun;
                TextRange textRange = new(WrittenBookEditor.Selection.Start, WrittenBookEditor.Selection.End)
                {
                    Text = Clipboard.GetText()
                };
                e.Handled = true;
            }
            #endregion

            #region 处理换行
            if (e.Key == Key.Enter)
            {
                RichParagraph richParagraph = new();
                if (WrittenBookEditor.CaretPosition.IsEndOfBlock())
                {
                    WrittenBookEditor.Document.Blocks.InsertAfter(WrittenBookEditor.CaretPosition.Paragraph, richParagraph);
                    WrittenBookEditor.CaretPosition = richParagraph.ContentStart;
                }
                else
                {
                    if (WrittenBookEditor.CaretPosition.IsAtLineStartPosition)
                    {
                        RichParagraph current = WrittenBookEditor.CaretPosition.Paragraph as RichParagraph;
                        WrittenBookEditor.Document.Blocks.InsertBefore(WrittenBookEditor.CaretPosition.Paragraph, richParagraph);
                        WrittenBookEditor.CaretPosition = current.ContentStart;
                    }
                    else//分离光标所在文本块左右侧的文本，并继承左侧属性，把分离出来的文本对象加入新段落中，再将新段落插入到当前段落后，更新光标位置
                    {
                        RichRun richRun = WrittenBookEditor.CaretPosition.Parent as RichRun;
                        TextRange CutDownStartRange = new(WrittenBookEditor.CaretPosition,richRun.ContentStart);
                        TextRange CutDownEndRange = new(WrittenBookEditor.CaretPosition, richRun.ContentEnd);
                        string StartText = CutDownStartRange.Text;
                        string EndText = CutDownEndRange.Text;
                        richRun.Text = richRun.UID = StartText;
                        RichRun SpiltRichRun = new();
                        SpiltRichRun.UID = SpiltRichRun.Text = EndText;
                        SpiltRichRun.ObfuscateTimer.IsEnabled = richRun.ObfuscateTimer.IsEnabled;
                        WrittenBookEditor.CaretPosition.Paragraph.Inlines.InsertAfter(richRun,SpiltRichRun);
                        List<RichRun> CurrentRuns = WrittenBookEditor.CaretPosition.Paragraph.Inlines.ToList().ConvertAll(item=>item as RichRun);
                        int CurrentIndex = CurrentRuns.IndexOf(richRun);
                        for (int i = CurrentIndex + 1; i < CurrentRuns.Count; i++)
                        {
                            richParagraph.Inlines.Add(CurrentRuns[i]);
                            WrittenBookEditor.CaretPosition.Paragraph.Inlines.Remove(CurrentRuns[i]);
                        }
                        WrittenBookEditor.Document.Blocks.InsertAfter(WrittenBookEditor.CaretPosition.Paragraph, richParagraph);
                        WrittenBookEditor.CaretPosition = richParagraph.ContentStart;
                    }
                }
                WrittenBookEditor.Focus();
                e.Handled = true;
            }
            #endregion
        }

        /// <summary>
        /// 处理空文档内容
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void WrittenBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            EnabledFlowDocument enabledFlowDocument = WrittenBookEditor.Document as EnabledFlowDocument;
            if (enabledFlowDocument.Blocks.Count == 0)
            {
                Paragraph paragraph = new();
                paragraph.Inlines.Add(new RichRun());
                enabledFlowDocument.Blocks.Add(paragraph);
            }

            #region 更新超出的内容
            TextRange AllRange = new(WrittenBookEditor.Document.ContentStart, WrittenBookEditor.Document.ContentEnd);

            byte[] exceedByteArray = Encoding.UTF8.GetBytes(AllRange.Text);
            int exceedLength = exceedByteArray.Length - PageMaxByteLength;
            int exceedLineCount = enabledFlowDocument.Blocks.Count - PageMaxLineCount;

            if (exceedLineCount > 0)
            {
                ExceedsBlock.ToolTip = "当前超出" + exceedLineCount + "行";
            }
            if (exceedLength > 0)
            {
                ExceedsBlock.ToolTip += "当前超出" + exceedLength + "个字节";
            }
            if (exceedLength > 0 || exceedLineCount > 0)
            {
                ToolTipService.SetInitialShowDelay(ExceedsBlock, 0);
                ToolTipService.SetBetweenShowDelay(ExceedsBlock, 0);
                ExceedsBlock.Text = "查看超出内容";
                DisplayExceedsCount = Visibility.Visible;
            }
            else
                DisplayExceedsCount = Visibility.Collapsed;
            #endregion
        }

        /// <summary>
        /// 抬起鼠标左键
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void RichTextBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) => ShowHoverMenu();

        /// <summary>
        /// 管理排版与悬浮菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void WrittenBookTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            #region 如果内容被全部删除,则更新空白文档的排版
            if ((e.Key == Key.Back || e.Key == Key.Delete) && WrittenBookEditor.Document.Blocks.Count <= 1)
            {
                if(WrittenBookEditor.Document.Blocks.Count == 1 && WrittenBookEditor.Document.Blocks.First() is not RichParagraph)
                {
                    Paragraph paragraph = WrittenBookEditor.Document.Blocks.First() as Paragraph;
                    List<RichRun> richRuns = paragraph.Inlines.Cast<RichRun>().ToList();
                    RichParagraph richParagraph = new() { FontFamily = new FontFamily("Microsoft YaHei UI"), FontSize = 12 };
                    richParagraph.Inlines.Clear();
                    foreach (var item in richRuns)
                    {
                        richParagraph.Inlines.Add(item);
                    }
                    WrittenBookEditor.Document.Blocks.Add(richParagraph);
                }
            }
            #endregion
            ShowHoverMenu();
        }

        /// <summary>
        /// 设置被选择文本的字体颜色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SetSelectionColor(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ColorPickers colorPickers = sender as ColorPickers;
            TextRange textRange = new TextRange(WrittenBookEditor.Selection.Start, WrittenBookEditor.Selection.End);
            textRange.ApplyPropertyValue(TextBlock.ForegroundProperty, colorPickers.SelectColor);
        }

        /// <summary>
        /// 获取成书编辑框的引用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void WrittenBoxBackgroundLoaded(object sender, RoutedEventArgs e)
        {
            Image image = sender as Image;
            image.Source = new BitmapImage(new Uri(backgroundFilePath, UriKind.Absolute));
        }

        /// <summary>
        /// 获取成书编辑框的引用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void WrittenBoxLoaded(object sender, RoutedEventArgs e)
        {
            //初始化
            if(WrittenBookPages.Count == 0 || CurrentPageIndex < 0 || CurrentPageIndex > WrittenBookPages.Count)
            {
                WrittenBookEditor = sender as RichTextBox;
                //初始化文档链表
                if (HistroyFlowDocumentList.Count == 0)
                    WrittenBookPages.Add(WrittenBookEditor.Document as EnabledFlowDocument);
                else
                {
                    HistroyFlowDocumentList.All(item => { item.FontFamily = new FontFamily("Bitstream Vera Sans Mono"); return true; });
                    WrittenBookPages = HistroyFlowDocumentList;
                    WrittenBookEditor.Document = WrittenBookPages[0];
                }
            }
            else//刚返回编辑页
                WrittenBookEditor.Document = WrittenBookPages[CurrentPageIndex];

            #region 初始化事件菜单
            popup.Child = EventComponent;
            popup.PlacementTarget = WrittenBookEditor;
            #endregion
        }

        /// <summary>
        /// 获取超出文本块的引用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ExceedsTextBlockLoaded(object sender, RoutedEventArgs e)
        {
            ExceedsBlock = sender as TextBlock;
        }

        /// <summary>
        /// 载入拾色器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ColorPickerLoaded(object sender, RoutedEventArgs e)
        {
            colorPicker = sender as ColorPickers;
        }

        /// <summary>
        /// 载入左箭头背景
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void LeftArrowLoaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(leftArrowFilePath))
            {
                Image image = sender as Image;
                image.Source = new BitmapImage(new Uri(leftArrowFilePath, UriKind.Absolute));
            }
        }

        /// <summary>
        /// 载入右箭头背景
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void RightArrowLoaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(rightArrowFilePath))
            {
                Image image = sender as Image;
                image.Source = new BitmapImage(new Uri(rightArrowFilePath, UriKind.Absolute));
            }
        }

        [RelayCommand]
        /// <summary>
        /// 取消署名
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SignatureCancel() => PageFrame.Content = editPage;

        /// <summary>
        /// 鼠标移到左箭头处显示高亮图像
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void LeftArrowMouseEnter(object sender, MouseEventArgs e)
        {
            if (File.Exists(leftArrowHightLightFilePath))
            {
                Image image = sender as Image;
                image.Source = new BitmapImage(new Uri(leftArrowHightLightFilePath, UriKind.Absolute));
            }
        }

        /// <summary>
        /// 鼠标移到右箭头处显示高亮图像
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void RightArrowMouseEnter(object sender, MouseEventArgs e)
        {
            if (File.Exists(rightArrowHightLightFilePath))
            {
                Image image = sender as Image;
                image.Source = new BitmapImage(new Uri(rightArrowHightLightFilePath, UriKind.Absolute));
            }
        }

        /// <summary>
        /// 鼠标移出左箭头处显示普通图像
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void LeftArrowMouseLeave(object sender, MouseEventArgs e)
        {
            if (File.Exists(leftArrowFilePath))
            {
                Image image = sender as Image;
                image.Source = new BitmapImage(new Uri(leftArrowFilePath, UriKind.Absolute));
            }
        }

        /// <summary>
        /// 鼠标移出右箭头处显示普通图像
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void RightArrowMouseLeave(object sender, MouseEventArgs e)
        {
            if (File.Exists(rightArrowFilePath))
            {
                Image image = sender as Image;
                image.Source = new BitmapImage(new Uri(rightArrowFilePath, UriKind.Absolute));
            }
        }

        [RelayCommand]
        /// <summary>
        /// 署名
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Signature() => PageFrame.Content = signaturePage;

        /// <summary>
        /// 载入署名页背景
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SignatureBackgroundLoaded(object sender, RoutedEventArgs e)
        {
            if(File.Exists(signatureBackgroundFilePath))
            {
                Image image = sender as Image;
                image.Source = new BitmapImage(new Uri(signatureBackgroundFilePath,UriKind.Absolute));
            }
        }

        [RelayCommand]
        /// <summary>
        /// 署名并关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task SignatureAndClose() => await Run();

        /// <summary>
        /// 向左翻页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void LeftArrowMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            --CurrentPageIndex;
            WrittenBookEditor.Document = WrittenBookPages[CurrentPageIndex];
        }

        /// <summary>
        /// 向右翻页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void RightArrowMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MaxPage = WrittenBookPages.Count;
            if (MaxPage <= (CurrentPageIndex + 1))
            {
                WrittenBookPages.Add(new EnabledFlowDocument() { FontFamily = new FontFamily("Bitstream Vera Sans Mono"), LineHeight = 10 });
            }
            CurrentPageIndex++;
            WrittenBookEditor.Document = WrittenBookPages[CurrentPageIndex];
        }
    }
}