using cbhk_environment.CustomControls;
using cbhk_environment.CustomControls.ColorPickers;
using cbhk_environment.GeneralTools;
using cbhk_environment.GeneralTools.MessageTip;
using cbhk_environment.Generators.WrittenBookGenerator.Components;
using cbhk_environment.WindowDictionaries;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using EditPage = cbhk_environment.Generators.WrittenBookGenerator.Components.EditPage;
using Image = System.Windows.Controls.Image;

namespace cbhk_environment.Generators.WrittenBookGenerator
{
    public class written_book_datacontext: ObservableObject
    {
        #region 生成与返回
        public RelayCommand RunCommand { get; set; }

        public RelayCommand<CommonWindow> ReturnCommand { get; set; }
        #endregion

        #region 字段
        //成书编辑框引用
        public RichTextBox written_box = null;

        //成书背景文件路径
        string backgroundFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\WrittenBook\\images\\written_book_background.png";
        //成书背景控件引用
        Border written_book_background = null;
        //左箭头背景文件路径
        string leftArrowFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\WrittenBook\\images\\left_arrow.png";
        //右箭头背景文件路径
        string rightArrowFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\WrittenBook\\images\\right_arrow.png";
        //左箭头高亮背景文件路径
        string leftArrowHightLightFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\WrittenBook\\images\\enter_left_arrow.png";
        //右箭头高亮背景文件路径
        string rightArrowHightLightFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\WrittenBook\\images\\enter_right_arrow.png";
        //署名按钮背景文件路径
        string signatureFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\WrittenBook\\images\\signature_button.png";
        //署名背景文件路径
        string signatureBackgroundFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\WrittenBook\\images\\signature_page.png";
        //署名并关闭背景文件路径
        string signatureAndCloseFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\WrittenBook\\images\\setting_signature.png";
        //署名完毕背景文件路径
        string sureToSignatureFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\WrittenBook\\images\\sure_to_signature.png";
        //署名完毕按钮引用
        Button sureToSignatureButton = null;
        //取消署名背景文件路径
        string signatureCancelFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\WrittenBook\\images\\cancel_signature.png";
        //点击事件数据源
        public static ObservableCollection<string> clickEventSource { get; set; } = new();
        //悬浮事件数据源
        public static ObservableCollection<string> hoverEventSource { get; set; } = new();
        //点击事件数据源文件路径
        string clickEventSourceFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\WrittenBook\\data\\clickEventActions.ini";
        //悬浮事件数据源文件路径
        string hoverEventSourceFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\WrittenBook\\data\\hoverEventActions.ini";
        //事件数据库
        public static Dictionary<string, string> EventDataBase = new Dictionary<string, string> { };
        //混淆文本配置文件路径
        string obfuscateFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\WrittenBook\\data\\obfuscateChars.ini";
        //混淆字体类型
        string obfuscatedFontFamily = "Bitstream Vera Sans Mono";
        //普通字体类型
        string commonFontFamily = "Minecraft AE Pixel";
        //混淆文本迭代链表
        public static List<char> obfuscates = new();
        #endregion

        //流文档链表,每个成员代表成书中的一页
        public List<EnabledFlowDocument> WrittenBookPages = new();

        /// <summary>
        /// 一页总字符数
        /// </summary>
        int PageMaxCharCount = 377;

        //字符数量超出提示
        TextBlock ExceedsBlock = null;

        #region 字符超出数量
        string exceedsCount = "0";
        public string ExceedsCount
        {
            get { return exceedsCount; }
            set
            {
                exceedsCount = value;
                OnPropertyChanged();
            }
        }
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
            get { return maxPage; }
            set
            {
                maxPage = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region 当前页码下标
        int currentPageIndex = 0;
        int CurrentPageIndex
        {
            get
            {
                return currentPageIndex;
            }
            set
            {
                currentPageIndex = value;
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
            get { return currentRichRun; }
            set
            {
                currentRichRun = value;
                OnPropertyChanged();
            }
        }
        #endregion

        /// <summary>
        /// 事件设置窗体
        /// </summary>
        public static Window EventForm = new()
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F2F2F")),
            SizeToContent = SizeToContent.WidthAndHeight,
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.None,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        /// <summary>
        /// 事件设置控件
        /// </summary>
        TextEventsForm EventComponent = new TextEventsForm();

        //控制事件窗体的显示
        bool DisplayEventForm = false;

        #region 设置选定文本样式指令
        /// <summary>
        /// 粗体
        /// </summary>
        public RelayCommand BoldTextCommand { get; set; }
        /// <summary>
        /// 斜体
        /// </summary>
        public RelayCommand ItalicTextCommand { get; set; }
        /// <summary>
        /// 下划线
        /// </summary>
        public RelayCommand UnderlinedTextCommand { get; set; }
        /// <summary>
        /// 删除线
        /// </summary>
        public RelayCommand StrikethroughTextCommand { get; set; }
        /// <summary>
        /// 混淆文本
        /// </summary>
        public RelayCommand ObfuscateTextCommand { get; set; }
        /// <summary>
        /// 重置文本
        /// </summary>
        public RelayCommand ResetTextCommand { get; set; }
        #endregion

        #region 开启点击事件
        private bool enableClickEvent = false;
        public bool EnableClickEvent
        {
            get { return enableClickEvent; }
            set
            {
                enableClickEvent = value;
                OnPropertyChanged();
            }
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
        ColorPickers LeftColorPicker = null;
        ColorPickers RightColorPicker = null;
        #endregion

        #region 被选择文本的字体颜色
        private SolidColorBrush selectionColor = new SolidColorBrush(Color.FromRgb(0,0,0));
        public SolidColorBrush SelectionColor
        {
            get { return selectionColor; }
            set
            {
                selectionColor = value;
                OnPropertyChanged();
            }
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

        #region 保存作者
        //标记当前背景样式
        bool HaveAuthor = false;
        private string author = "cber";
        public string Author
        {
            get { return author; }
            set
            {
                author = value;
                OnPropertyChanged();
                if(author.Trim() != "" && File.Exists(sureToSignatureFilePath) && !HaveAuthor)
                {
                    HaveAuthor = true;
                }
                else
                    if(author.Trim() == "" && File.Exists(signatureAndCloseFilePath) && HaveAuthor)
                {
                    HaveAuthor = false;
                }
            }
        }
        private string AuthorString
        {
            get
            {
                return "author:\"" + Author + "\",";
            }
        }
        #endregion

        #region 保存标题
        private string title = "我是标题";
        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                OnPropertyChanged();
            }
        }
        private string TitleString
        {
            get
            {
                return "title:\"" + Title + "\",";
            }
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

        #region 控制上方样式切换面板显示
        private Visibility displayStylePanel = Visibility.Visible;
        public Visibility DisplayStylePanel
        {
            get { return displayStylePanel; }
            set
            {
                displayStylePanel = value;
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

        //本生成器的图标路径
        string icon_path = "pack://application:,,,/cbhk_environment;component/resources/common/images/spawnerIcons/IconWrittenBook.png";

        //当前光标选中的文本对象链表
        List<RichRun> CurrentSelectedRichRunList = new List<RichRun> { };

        //作为内部工具被调用
        public static bool AsInternalTool = false;

        /// <summary>
        /// 保存最终结果
        /// </summary>
        public string result = "";

        /// <summary>
        /// 保存最终结果的首个json对象
        /// </summary>
        public string object_result = "";

        //取消署名按钮引用
        Button signatureCancelButton = null;
        //署名按钮
        Button SignatureButton = null;

        //保存成书的文档链表，用于显示和编辑
        public List<EnabledFlowDocument> HistroyFlowDocumentList = null;

        #region 初始化两个页面
        Frame PageFrame = new Frame() { NavigationUIVisibility = NavigationUIVisibility.Hidden };
        public static EditPage editPage = new EditPage();
        public static SignaturePage signaturePage = new SignaturePage();
        #endregion

        /// <summary>
        /// 是否作为内部工具被调用
        /// </summary>
        /// <param name="AsAnInternalTool"></param>
        public written_book_datacontext()
        {
            #region 链接指令
            RunCommand = new RelayCommand(run_command);
            ReturnCommand = new RelayCommand<CommonWindow>(return_command);
            BoldTextCommand = new RelayCommand(boldTextCommand);
            ItalicTextCommand = new RelayCommand(italicTextCommand);
            UnderlinedTextCommand = new RelayCommand(underlinedTextCommand);
            StrikethroughTextCommand = new RelayCommand(strikethroughTextCommand);
            ObfuscateTextCommand = new RelayCommand(obfuscateTextCommand);
            ResetTextCommand = new RelayCommand(resetTextCommand);
            #endregion

            #region 读取点击事件
            if (File.Exists(clickEventSourceFilePath) && clickEventSource.Count == 0)
            {
                string[] source = File.ReadAllLines(clickEventSourceFilePath);
                for (int i = 0; i < source.Length; i++)
                {
                    string[] data = source[i].Split('.');
                    clickEventSource.Add(data[1]);
                    if (!EventDataBase.ContainsKey(data[0]))
                        EventDataBase.Add(data[0], data[1]);
                }
            }
            #endregion

            #region 读取悬浮事件
            if (File.Exists(hoverEventSourceFilePath) && hoverEventSource.Count == 0)
            {
                string[] source = File.ReadAllLines(hoverEventSourceFilePath);
                for (int i = 0; i < source.Length; i++)
                {
                    string[] data = source[i].Split('.');
                    hoverEventSource.Add(data[1]);
                    if (!EventDataBase.ContainsKey(data[0]))
                        EventDataBase.Add(data[0], data[1]);
                }
            }
            #endregion

            #region 读取混淆文本配置
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\WrittenBook\\data\\obfuscateChars.ini"))
            {
                obfuscates = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\WrittenBook\\data\\obfuscateChars.ini").ToCharArray().ToList();
            }
            #endregion

            #region 初始化事件设置窗体
            EventForm.Content = EventComponent;
            EventForm.Closing += (o, e) => { e.Cancel = true; EventForm.Hide(); };
            EventForm.DataContext = this;
            #endregion
        }

        /// <summary>
        /// 载入编辑页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void LoadedEditPage(object sender, RoutedEventArgs e)
        {
            ContentControl contentControl = sender as ContentControl;
            editPage.DataContext = this;
            PageFrame.Content = editPage;
            contentControl.Content = PageFrame;
        }

        /// <summary>
        /// 重置选中文本的所有属性
        /// </summary>
        private void resetTextCommand()
        {
            RichRun start_run = written_box.Selection.Start.Parent as RichRun;
            RichRun end_run = written_box.Selection.End.Parent as RichRun;

            if (end_run != null && start_run != null)
            {
                RichParagraph StartParagraph = start_run.Parent as RichParagraph;
                RichParagraph EndParagraph = end_run.Parent as RichParagraph;
                List<RichRun> StartRuns = new List<RichRun> { };
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
                        List<RichRun> EndRuns = new List<RichRun> { };
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
                    item.TextDecorations = new TextDecorationCollection();
                    item.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                    item.IsObfuscated = false;
                    item.ObfuscateTimer.Enabled = false;
                    if (item.UID.Trim() != "")
                        item.Text = item.UID;
                }
            }
        }

        /// <summary>
        /// 为混淆文字效果提供分割首尾文本块的功能
        /// </summary>
        private void obfuscateSpiltRichRunHelper(RichRun StartRichRun,RichRun EndRichRun,RichParagraph startRichParagraph,RichParagraph endRichParagraph)
        {
            #region 切割选区起始处到起始文本块之间的内容和选区末尾处到末尾文本块之间的内容
            if(!Equals(StartRichRun, EndRichRun))
            {
                if (!StartRichRun.ObfuscateTimer.Enabled)
                {
                    RichRun PreviousRichRun = new RichRun();
                    TextRange StartPartRange = new TextRange(StartRichRun.ContentStart, written_box.Selection.Start);
                    PreviousRichRun.Text = StartPartRange.Text;
                    startRichParagraph.Inlines.InsertBefore(StartRichRun, PreviousRichRun);
                    StartRichRun.Text = StartRichRun.Text.Substring(StartPartRange.Text.Length);
                }
                if (!EndRichRun.ObfuscateTimer.Enabled)
                {
                    RichRun NextRichRun = new RichRun();
                    TextRange EndPartRange = new TextRange(EndRichRun.ContentEnd, written_box.Selection.End);
                    NextRichRun.Text = EndPartRange.Text;
                    endRichParagraph.Inlines.InsertAfter(EndRichRun, NextRichRun);
                    EndRichRun.Text = EndRichRun.Text.Substring(0, EndRichRun.Text.Length - EndPartRange.Text.Length);
                }
            }
            else
            {
                if (!StartRichRun.ObfuscateTimer.Enabled)
                {
                    RichRun PreviousRichRun = new RichRun();
                    RichRun NextRichRun = new RichRun();
                    TextRange StartPartRange = new TextRange(StartRichRun.ContentStart, written_box.Selection.Start);
                    TextRange EndPartRange = new TextRange(EndRichRun.ContentEnd, written_box.Selection.End);
                    PreviousRichRun.Text = StartPartRange.Text;
                    NextRichRun.Text = EndPartRange.Text;
                    startRichParagraph.Inlines.InsertBefore(StartRichRun, PreviousRichRun);
                    endRichParagraph.Inlines.InsertAfter(EndRichRun, NextRichRun);
                    StartRichRun.Text = written_box.Selection.Text;
                }
            }
            #endregion
        }

        /// <summary>
        /// 选中文本切换混淆文字
        /// </summary>
        private void obfuscateTextCommand()
        {
            if (written_box.Selection == null) return;
            if (written_box.Selection.Text.Length == 0) return;

            RichParagraph startRichParagraph = written_box.Selection.Start.Paragraph as RichParagraph;
            RichParagraph endRichParagraph = written_box.Selection.End.Paragraph as RichParagraph;
            if (startRichParagraph == null || endRichParagraph == null) return;
            //获取选区头部所在的文本块以及它所在段落中的索引
            RichRun StartRichRun = written_box.Selection.Start.Parent as RichRun;
            RichRun EndRichRun = written_box.Selection.End.Parent as RichRun;
            List<RichRun> CurrentRichRuns = written_box.Selection.Start.Paragraph.Inlines.ToList().ConvertAll(item => item as RichRun);

            //判断选区首尾是否在同一个段落中
            if (Equals(startRichParagraph,endRichParagraph))
            {
                if (Equals(StartRichRun,EndRichRun))
                {
                    //关闭混淆
                    if(StartRichRun.ObfuscateTimer.Enabled)
                    {
                        StartRichRun.IsObfuscated = false;
                        StartRichRun.ObfuscateTimer.Enabled = false;
                        StartRichRun.Text = StartRichRun.UID;
                        StartRichRun.FontFamily = new FontFamily(commonFontFamily);
                    }
                    else//开启混淆
                    {
                        obfuscateSpiltRichRunHelper(StartRichRun,StartRichRun,startRichParagraph,startRichParagraph);
                        StartRichRun.UID = StartRichRun.Text;
                        StartRichRun.FontFamily = new FontFamily(obfuscatedFontFamily);
                        StartRichRun.IsObfuscated = true;
                        StartRichRun.ObfuscateTimer.Enabled = true;
                    }
                }
                else
                {
                    int StartRichRunIndex = CurrentRichRuns.IndexOf(StartRichRun);
                    int EndRichRunIndex = CurrentRichRuns.IndexOf(EndRichRun);
                    //检查未开启混淆的文本块数量
                    int IsNotEnableCount = 0;
                    //未混淆文本块链表
                    List<RichRun> IsNotEnableRichRuns = new List<RichRun> { };
                    CurrentRichRuns.All(item =>
                    {
                        if(!item.ObfuscateTimer.Enabled)
                        {
                            IsNotEnableRichRuns.Add(item);
                            IsNotEnableCount++;
                        }
                        return true;
                    });
                    //大于0则把未开启混淆的文本块开启混淆
                    if(IsNotEnableCount > 0)
                    {
                        obfuscateSpiltRichRunHelper(StartRichRun, EndRichRun, startRichParagraph, startRichParagraph);
                        IsNotEnableRichRuns.All(item =>
                        {
                            item.UID = item.Text;
                            item.FontFamily = new FontFamily(obfuscatedFontFamily);
                            item.IsObfuscated = true;
                            item.ObfuscateTimer.Enabled = true;
                            return true;
                        });
                    }
                    else//否则把所有选中的文本块去掉混淆
                    {
                        foreach (RichRun item in CurrentRichRuns)
                        {
                            item.IsObfuscated = false;
                            item.ObfuscateTimer.Enabled = false;
                            item.Text = item.UID;
                            item.FontFamily = new FontFamily(commonFontFamily);
                        }
                    }
                }
            }
            else//不在同一个段落中则把在选区中的两个段落之间的所有文本块合并处理
            {
                #region 获取起始与末尾两个段落之间所有段落的文本块
                List<RichParagraph> richParagraphs = written_box.Document.Blocks.ToList().ConvertAll(item=>item as RichParagraph);
                int StartRichParaIndex = richParagraphs.IndexOf(startRichParagraph);
                int EndRichParaIndex = richParagraphs.IndexOf(endRichParagraph);
                for (int i = StartRichParaIndex + 1; i <= (EndRichParaIndex - 1); i++)
                    CurrentRichRuns.AddRange(richParagraphs[i].Inlines.ToList().ConvertAll(item => item as RichRun));
                #endregion

                List<RichRun> endRichRuns = endRichParagraph.Inlines.ToList().ConvertAll(item => item as RichRun);
                int StartRichRunIndex = CurrentRichRuns.IndexOf(StartRichRun);
                int EndRichRunIndex = endRichRuns.IndexOf(EndRichRun);
                int StartUnableCount = 0;
                int EndUnableCount = 0;
                List<RichRun> UnableRichRuns = new List<RichRun> { };
                //记录未开启混淆的文本块数量
                for (int i = StartRichRunIndex; i < CurrentRichRuns.Count; i++)
                {
                    if (!CurrentRichRuns[i].ObfuscateTimer.Enabled)
                    {
                        UnableRichRuns.Add(CurrentRichRuns[i]);
                        StartUnableCount++;
                    }
                }
                for (int i = 0; i <= EndRichRunIndex; i++)
                {
                    if (!endRichRuns[i].ObfuscateTimer.Enabled)
                    {
                        UnableRichRuns.Add(endRichRuns[i]);
                        EndUnableCount++;
                    }
                }
                //总数量为0则说明选区内文本块已全部开启混淆,则需要把所有文本块关闭混淆
                if(StartUnableCount + EndUnableCount == 0)
                {
                    foreach (var item in CurrentRichRuns)
                    {
                        item.IsObfuscated = false;
                        item.ObfuscateTimer.Enabled = false;
                        item.UID = item.Text;
                        item.FontFamily = new FontFamily(commonFontFamily);
                    }
                }
                else//否则把未开启混淆的文本块开启混淆
                {
                    obfuscateSpiltRichRunHelper(StartRichRun, EndRichRun, startRichParagraph, endRichParagraph);
                    foreach (var item in UnableRichRuns)
                    {
                        item.UID = item.Text;
                        item.FontFamily = new FontFamily(obfuscatedFontFamily);
                        item.IsObfuscated = true;
                        item.ObfuscateTimer.Enabled = true;
                    }
                }
            }
        }

        /// <summary>
        /// 选中文本切换删除线
        /// </summary>
        private void strikethroughTextCommand()
        {
            if (written_box.Selection == null)
                return;
            if (written_box.Selection.Text.Length == 0)
                return;
            TextRange textRange = new TextRange(written_box.Selection.Start, written_box.Selection.End);
            TextDecorationCollection current_decorations = textRange.GetPropertyValue(TextBlock.TextDecorationsProperty) as TextDecorationCollection;

            if(current_decorations != null)
            {
                int underline_index = current_decorations.IndexOf(TextDecorations.Baseline.First());
                if (!current_decorations.Contains(TextDecorations.Strikethrough.First()))
                {
                    TextDecorationCollection textDecorations = new TextDecorationCollection();
                    if (underline_index != -1)
                        textDecorations.Add(TextDecorations.Baseline);
                    textDecorations.Add(TextDecorations.Strikethrough);
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

        /// <summary>
        /// 选中文本切换下划线
        /// </summary>
        private void underlinedTextCommand()
        {
            if (written_box.Selection == null)
                return;
            if (written_box.Selection.Text.Length == 0)
                return;
            TextRange textRange = new TextRange(written_box.Selection.Start, written_box.Selection.End);
            TextDecorationCollection current_decorations = textRange.GetPropertyValue(TextBlock.TextDecorationsProperty) as TextDecorationCollection;

            if (current_decorations != null)
            {
                int strikethrough_index = current_decorations.IndexOf(TextDecorations.Strikethrough.First());
                if (!current_decorations.Contains(TextDecorations.Baseline.First()))
                {
                    TextDecorationCollection textDecorations = new TextDecorationCollection();
                    if (strikethrough_index != -1)
                        textDecorations.Add(TextDecorations.Strikethrough);
                    textDecorations.Add(TextDecorations.Baseline);
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

        /// <summary>
        /// 选中文本切换斜体
        /// </summary>
        private void italicTextCommand()
        {
            if (written_box.Selection == null)
                return;
            if (written_box.Selection.Text.Length == 0)
                return;
            TextRange textRange = new TextRange(written_box.Selection.Start, written_box.Selection.End);
            if (Equals(textRange.GetPropertyValue(TextBlock.FontStyleProperty), FontStyles.Normal))
                textRange.ApplyPropertyValue(TextBlock.FontStyleProperty, FontStyles.Italic);
            else
                textRange.ApplyPropertyValue(TextBlock.FontStyleProperty, FontStyles.Normal);
        }

        /// <summary>
        /// 选中文本切换粗体
        /// </summary>
        private void boldTextCommand()
        {
            if (written_box.Selection == null)
                return;
            if (written_box.Selection.Text.Length == 0)
                return;
            TextRange textRange = new TextRange(written_box.Selection.Start, written_box.Selection.End);
            if(Equals(textRange.GetPropertyValue(TextBlock.FontWeightProperty),FontWeights.Normal))
            textRange.ApplyPropertyValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            else
                textRange.ApplyPropertyValue(TextBlock.FontWeightProperty, FontWeights.Normal);
        }

        /// <summary>
        /// 返回主页
        /// </summary>
        /// <param name="win"></param>
        private void return_command(CommonWindow win)
        {
            WrittenBook.cbhk.Topmost = true;
            WrittenBook.cbhk.WindowState = WindowState.Normal;
            WrittenBook.cbhk.Show();
            WrittenBook.cbhk.Topmost = false;
            WrittenBook.cbhk.ShowInTaskbar = true;
            win.Close();
        }

        /// <summary>
        /// 执行生成
        /// </summary>
        private void run_command()
        {
            if (!AsInternalTool)
            {
                //最终结果
                string result = "/give @p written_book";
                //合并所有页数据
                string pages_string = "pages:[";
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
                pages_string += page_string.TrimEnd(',') + "]";
                pages_string = pages_string.Trim() == "pages:['[\"\"]']" || pages_string.Trim() == "pages:['[\"\",{\"text\":\"\"}]']" ? "" : pages_string;
                string NBTData = "";
                NBTData += TitleString + AuthorString + pages_string;
                NBTData = "{" + NBTData.TrimEnd(',') + "}";
                result += NBTData;

                if(ShowGeneratorResult)
                {
                GenerateResultDisplayer.Displayer displayer = GenerateResultDisplayer.Displayer.GetContentDisplayer();
                displayer.GeneratorResult(result,"成书",icon_path);
                displayer.Show();
                }
                else
                {
                    Clipboard.SetText(result);
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
                CommonWindow window = Window.GetWindow(written_box) as CommonWindow;
                window.DialogResult = true;
            }
        }

        /// <summary>
        /// 弹出事件设置窗体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OpenEventForm(object sender, MouseButtonEventArgs e)
        {
            //CurrentSelectedRichRunList
            //EventComponent
            DisplayEventForm = !DisplayEventForm;
            if (!DisplayEventForm)
            {
                EventForm.Close();
                return;
            }
            RichRun start_run = written_box.Selection.Start.Parent as RichRun;
            RichRun end_run = written_box.Selection.End.Parent as RichRun;
            Paragraph start_paragraph = start_run.Parent as Paragraph;
            Paragraph end_paragraph = end_run.Parent as Paragraph;
            bool SameRun = Equals(start_run,end_run);
            if(SameRun)
            {
                #region 同步数据
                CurrentRichRun = start_run;
                #region 事件的开关
                Binding HaveClickEventBinder = new Binding()
                {
                    Path = new PropertyPath("CurrentRichRun.HasClickEvent"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                Binding HaveHoverEventBinder = new Binding()
                {
                    Path = new PropertyPath("CurrentRichRun.HasHoverEvent"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                Binding HaveInsertionBinder = new Binding()
                {
                    Path = new PropertyPath("CurrentRichRun.HasInsertion"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                #endregion

                #region 事件的类型和值
                Binding ClickEventActionBinder = new Binding()
                {
                    Path = new PropertyPath("CurrentRichRun.ClickEventActionItem"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                Binding HoverEventActionBinder = new Binding()
                {
                    Path = new PropertyPath("CurrentRichRun.HoverEventActionItem"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                Binding ClickEventValueBinder = new Binding()
                {
                    Path = new PropertyPath("CurrentRichRun.ClickEventValue"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                Binding HoverEventValueBinder = new Binding()
                {
                    Path = new PropertyPath("CurrentRichRun.HoverEventValue"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                Binding InsertionValueBinder = new Binding()
                {
                    Path = new PropertyPath("CurrentRichRun.InsertionValue"),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                #endregion

                BindingOperations.SetBinding(EventComponent.EnableClickEvent, System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, HaveClickEventBinder);
                BindingOperations.SetBinding(EventComponent.EnableHoverEvent, System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, HaveHoverEventBinder);
                BindingOperations.SetBinding(EventComponent.EnableInsertion, System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, HaveInsertionBinder);

                EventComponent.ClickEventPanel.Visibility = CurrentRichRun.HasClickEvent ? Visibility.Visible : Visibility.Collapsed;
                EventComponent.HoverEventPanel.Visibility = CurrentRichRun.HasHoverEvent ? Visibility.Visible : Visibility.Collapsed;
                EventComponent.InsertionPanel.Visibility = CurrentRichRun.HasInsertion ? Visibility.Visible : Visibility.Collapsed;

                BindingOperations.SetBinding(EventComponent.ClickEventActionBox, System.Windows.Controls.Primitives.Selector.SelectedItemProperty, ClickEventActionBinder);
                BindingOperations.SetBinding(EventComponent.HoverEventActionBox, System.Windows.Controls.Primitives.Selector.SelectedItemProperty, HoverEventActionBinder);

                #region 在视觉上更新事件类型
                //EventComponent.ClickEventActionBox.ApplyTemplate();
                //EventComponent.HoverEventActionBox.ApplyTemplate();
                //TextBox clickEventActionBox = EventComponent.ClickEventActionBox.Template.FindName("EditableTextBox", EventComponent.ClickEventActionBox) as TextBox;
                //clickEventActionBox.Text = CurrentRichRun.ClickEventActionItem;

                //TextBox hoverEventActionBox = EventComponent.HoverEventActionBox.Template.FindName("EditableTextBox", EventComponent.HoverEventActionBox) as TextBox;
                //hoverEventActionBox.Text = CurrentRichRun.HoverEventActionItem;
                #endregion

                BindingOperations.SetBinding(EventComponent.ClickEventValueBox, TextBox.TextProperty, ClickEventValueBinder);
                BindingOperations.SetBinding(EventComponent.HoverEventValueBox, TextBox.TextProperty, HoverEventValueBinder);
                BindingOperations.SetBinding(EventComponent.InsertionValueBox, TextBox.TextProperty, InsertionValueBinder);
                #endregion
            }
            else//选区首尾文本块不同时更新它们之间的所有文本块
            {

            }

            EventForm.Show();
            EventForm.Topmost = true;
        }

        /// <summary>
        /// 检测按回车键换行和粘贴
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void WrittenBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Escape)
            {
                List<RichParagraph> richParagraphs = written_box.Document.Blocks.ToList().ConvertAll(item=>item as RichParagraph);
                foreach (var richpara in richParagraphs)
                {
                    foreach (var item in richpara.Inlines)
                    {
                        RichRun richRun = item as RichRun;
                        MessageBox.Show(richRun.Text);
                    }
                }
            }

            //处理粘贴的数据,合并为RichRun以适配混淆效果
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.V)
            {
                RichRun richRun = written_box.CaretPosition.Parent as RichRun;
                TextRange textRange = new TextRange(written_box.Selection.Start,written_box.Selection.End);
                textRange.Text = Clipboard.GetText();
                //if (written_box.Selection.Text.Length == 0)
                //{
                //    TextPointer start = richRun.ElementStart;
                //    TextPointer select = written_box.CaretPosition;
                //    int index = start.GetOffsetToPosition(select);
                //    richRun.Text = richRun.Text.Insert(index - 1, Clipboard.GetText());
                //    e.Handled = true;
                //}
                //else
                //{
                //    if (written_box.Selection.Start != null && written_box.Selection.Text.Length > 0)
                //    {
                //        TextPointer nextPointer = written_box.Selection.Start.GetPositionAtOffset(written_box.Selection.Text.Length - 2);
                //        TextRange textRange = new TextRange(written_box.CaretPosition,nextPointer);
                //        textRange.Text = Clipboard.GetText();
                //    }
                //}
            }

            if (e.Key == Key.Enter)
            {
                RichParagraph richPara = new RichParagraph();
                if (written_box.CaretPosition.IsEndOfBlock())
                {
                    written_box.Document.Blocks.InsertAfter(written_box.CaretPosition.Paragraph, richPara);
                    written_box.CaretPosition = richPara.ContentStart;
                }
                else
                {
                    if (written_box.CaretPosition.IsAtLineStartPosition)
                    {
                        RichParagraph current = written_box.CaretPosition.Paragraph as RichParagraph;
                        written_box.Document.Blocks.InsertBefore(written_box.CaretPosition.Paragraph, richPara);
                        written_box.CaretPosition = current.ContentStart;
                    }
                    else//分离光标所在文本块左右侧的文本，并继承左侧属性，把分离出来的文本对象加入新段落中，再将新段落插入到当前段落后，更新光标位置
                    {
                        RichRun richRun = written_box.CaretPosition.Parent as RichRun;
                        TextRange CutDownStartRange = new TextRange(written_box.CaretPosition,richRun.ContentStart);
                        TextRange CutDownEndRange = new TextRange(written_box.CaretPosition, richRun.ContentEnd);
                        string StartText = CutDownStartRange.Text;
                        string EndText = CutDownEndRange.Text;
                        richRun.Text = richRun.UID = StartText;
                        RichRun SpiltRichRun = new RichRun();
                        SpiltRichRun.UID = SpiltRichRun.Text = EndText;
                        SpiltRichRun.ObfuscateTimer.Enabled = richRun.ObfuscateTimer.Enabled;
                        written_box.CaretPosition.Paragraph.Inlines.InsertAfter(richRun,SpiltRichRun);
                        List<RichRun> CurrentRuns = written_box.CaretPosition.Paragraph.Inlines.ToList().ConvertAll(item=>item as RichRun);
                        int CurrentIndex = CurrentRuns.IndexOf(richRun);
                        for (int i = CurrentIndex + 1; i < CurrentRuns.Count; i++)
                        {
                            richPara.Inlines.Add(CurrentRuns[i]);
                            written_box.CaretPosition.Paragraph.Inlines.Remove(CurrentRuns[i]);
                        }
                        written_box.Document.Blocks.InsertAfter(written_box.CaretPosition.Paragraph, richPara);
                        written_box.CaretPosition = richPara.ContentStart;
                    }
                }
                written_box.Focus();
                e.Handled = true;
            }
        }

        /// <summary>
        /// 处理空文档内容
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void WrittenBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            EnabledFlowDocument enabledFlowDocument = written_box.Document as EnabledFlowDocument;
            if (enabledFlowDocument.Blocks.Count == 0)
            {
                Paragraph paragraph = new Paragraph();
                paragraph.Inlines.Add(new RichRun());
                enabledFlowDocument.Blocks.Add(paragraph);
            }

            #region 更新超出的字符数量
            TextRange AllRange = new TextRange(written_box.Document.ContentStart, written_box.Document.ContentEnd);
            int exceedCount = AllRange.Text.Length - PageMaxCharCount;
            if (exceedCount > 0)
            {
                ExceedsBlock.ToolTip = "当前超出" + exceedCount.ToString() + "个字符(仅作参考)";
                ToolTipService.SetInitialShowDelay(ExceedsBlock, 0);
                ToolTipService.SetShowDuration(ExceedsBlock, 5000);
                if (exceedCount > 100)
                {
                    ExceedsCount = "";
                    ExceedsBlock.Text = "查看超出的字符数";
                }
                else
                {
                    ExceedsBlock.Text = "查看超出的字符数:";
                    ExceedsCount = exceedCount.ToString();
                }
                DisplayExceedsCount = Visibility.Visible;
            }
            else
                DisplayExceedsCount = Visibility.Collapsed;
            #endregion
        }

        /// <summary>
        /// 设置被选择文本的字体颜色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SetSelectionColor(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ColorPickers colorPickers = sender as ColorPickers;
            TextRange textRange = new TextRange(written_box.Selection.Start, written_box.Selection.End);
            textRange.ApplyPropertyValue(TextBlock.ForegroundProperty, colorPickers.SelectColor);
        }

        /// <summary>
        /// 获取成书编辑框的引用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void WrittenBoxBackgroundLoaded(object sender, RoutedEventArgs e)
        {
            written_book_background = sender as Border;
            written_book_background.Background = new ImageBrush(new BitmapImage(new Uri(backgroundFilePath, UriKind.Absolute)));
        }

        /// <summary>
        /// 获取成书编辑框的引用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void WrittenBoxLoaded(object sender, RoutedEventArgs e)
        {
            //初始化
            if(written_box == null)
            {
                written_box = sender as RichTextBox;
                //初始化文档链表
                if (HistroyFlowDocumentList == null)
                    WrittenBookPages.Add(written_box.Document as EnabledFlowDocument);
                else
                {
                    HistroyFlowDocumentList.All(item => { item.FontFamily = new FontFamily(commonFontFamily); return true; });
                    WrittenBookPages = HistroyFlowDocumentList;
                    written_box.Document = WrittenBookPages[0];
                }
            }
            else//刚返回编辑页
                written_box.Document = WrittenBookPages[CurrentPageIndex];
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
            ColorPickers colorPickers = sender as ColorPickers;
            if (LeftColorPicker == null && colorPickers.Uid == "Left")
                LeftColorPicker = colorPickers;
            if (RightColorPicker == null && colorPickers.Uid == "Right")
                RightColorPicker = colorPickers;
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

        /// <summary>
        /// 载入署名并关闭背景
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SignatureAndCloseLoaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(signatureAndCloseFilePath))
            {
                sureToSignatureButton = sender as Button;
                sureToSignatureButton.Click += SignatureAndCloseClicked;
            }
        }

        /// <summary>
        /// 取消署名背景
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SignatureCancelLoaded(object sender, RoutedEventArgs e)
        {
            signatureCancelButton = sender as Button;
            signatureCancelButton.Click += SignatureCancelClicked;
        }

        /// <summary>
        /// 取消署名
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SignatureCancelClicked(object sender, RoutedEventArgs e)
        {
            DisplayStylePanel = SignatureButton.Visibility = Visibility.Visible;
            sureToSignatureButton.Visibility = signatureCancelButton.Visibility = Visibility.Collapsed;
            PageFrame.Content = editPage;
        }

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

        /// <summary>
        /// 载入署名背景
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SignatureButtonBackgroundLoaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(signatureFilePath))
            {
                SignatureButton = sender as Button;
                //SignatureButton.Background = new ImageBrush(new BitmapImage(new Uri(signatureFilePath, UriKind.Absolute)));
            }
        }

        /// <summary>
        /// 署名
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SignatureClick(object sender, RoutedEventArgs e)
        {
            signaturePage ??= new SignaturePage();
            signaturePage.DataContext ??= this;
            PageFrame.Content = signaturePage;
            DisplayStylePanel = SignatureButton.Visibility = Visibility.Collapsed;
            signatureCancelButton.Visibility = sureToSignatureButton.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 载入署名页背景
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SignatureBackgroundLoaded(object sender, RoutedEventArgs e)
        {
            if(File.Exists(signatureBackgroundFilePath))
            {
                Border border = sender as Border;
                border.Background = new ImageBrush(new BitmapImage(new Uri(signatureBackgroundFilePath,UriKind.Absolute)));
            }
        }

        /// <summary>
        /// 署名并关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SignatureAndCloseClicked(object sender, RoutedEventArgs e)
        {
            run_command();
        }

        /// <summary>
        /// 向左翻页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void LeftArrowMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            --CurrentPageIndex;
            written_box.Document = WrittenBookPages[CurrentPageIndex];
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
                WrittenBookPages.Add(new EnabledFlowDocument() { FontFamily = new FontFamily(commonFontFamily), FontSize = 25, LineHeight = 10 });
            }
            CurrentPageIndex++;
            written_box.Document = WrittenBookPages[CurrentPageIndex];
        }
    }
}
