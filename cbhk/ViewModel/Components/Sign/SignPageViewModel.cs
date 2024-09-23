using cbhk.CustomControls;
using cbhk.CustomControls.Interfaces;
using cbhk.GeneralTools;
using cbhk.GeneralTools.MessageTip;
using cbhk.View;
using cbhk.ViewModel;
using cbhk.ViewModel.Generators;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace cbhk.Generators.SignGenerator.Components
{
    public partial class SignPageViewModel : ObservableObject
    {
        #region 字段
        public ObservableCollection<string> SignTypeSource { get; set; } = [];

        public string Result { get; set; } = "";

        #region 告示牌类型
        private TextComboBoxItem selectedSignType;
        public TextComboBoxItem SelectedSignType
        {
            get => selectedSignType;
            set
            {
                SetProperty(ref selectedSignType, value);
                if (value is null)
                    return;
                SignPanelSource = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\"+SelectedSignType.Text+"SignPanel.png",UriKind.Absolute));
            }
        }
        #endregion

        #region 告示牌背景
        [ObservableProperty]
        public BitmapImage _signPanelSource = null;
        #endregion

        #region 生成方式
        [ObservableProperty]
        private bool _setblock;
        #endregion

        #region 已选择的版本
        private TextComboBoxItem selectedVersion;
        public TextComboBoxItem SelectedVersion
        {
            get => selectedVersion;
            set
            {
                SetProperty(ref selectedVersion,value);
                CurrentMinVersion = int.Parse(selectedVersion.Text.Replace(".", "").Replace("+", "").Split('-')[0]);
                VersionMask();
            }
        }

        private int currentMinVersion = 1202;
        public int CurrentMinVersion
        {
            get => currentMinVersion;
            set => currentMinVersion = int.Parse(SelectedVersion.Text.Replace(".", "").Replace("+", "").Split('-')[0]);
        }
        #endregion

        #region 是否有低版本样式
        private bool isNoStyleText;

        public bool IsNoStyleText
        {
            get => isNoStyleText;
            set
            {
                SetProperty(ref isNoStyleText, value);
                if (!IsNoStyleText)
                    Setblock = false;
            }
        }
        #endregion

        #region 版本数据源
        [ObservableProperty]
        public ObservableCollection<TextComboBoxItem> _versionSource = [];
        #endregion

        /// <summary>
        /// 不同版本拥有的类型映射图
        /// </summary>
        private Dictionary<string, List<string>> TypeVersionMap = [];

        #region 告示牌类型数据源
        public ObservableCollection<TextComboBoxItem> TypeSource { get; set; } = [];
        #endregion

        /// <summary>
        /// 正反面文档
        /// </summary>
        public ObservableCollection<EnabledFlowDocument> SignDocuments { get; set; } = [new EnabledFlowDocument(),new EnabledFlowDocument()];

        /// <summary>
        /// 需要适应版本变化的特指数据所属控件的事件
        /// </summary>
        public Dictionary<FrameworkElement, Action<FrameworkElement, RoutedEventArgs>> VersionNBTList = [];
        /// <summary>
        /// 版本控件
        /// </summary>
        public List<IVersionUpgrader> VersionComponents { get; set; } = [];

        /// <summary>
        /// 告示牌编辑器
        /// </summary>
        private RichTextBox SignTextEditor = null;

        /// <summary>
        /// 事件设置控件
        /// </summary>
        TextEvent EventComponent = new() { };

        #region 是否被裱
        public bool isWaxed = true;
        public bool IsWaxed
        {
            get => isWaxed;
            set => SetProperty(ref isWaxed,value);
        }
        #endregion

        #region 可被裱
        private bool canWaxed = false;

        public bool CanWaxed
        {
            get => canWaxed;
            set => SetProperty(ref canWaxed, value);
        }
        #endregion

        #region 已选中的颜色
        private SolidColorBrush selectionColor;

        public SolidColorBrush SelectionColor
        {
            get => selectionColor;
            set
            {
                SetProperty(ref selectionColor, value);
                SetColor();
            }
        }
        #endregion

        #region 是否悬挂
        private bool isHanging;

        public bool IsHanging
        {
            get => isHanging;
            set
            {
                SetProperty(ref isHanging, value);
                UpdateSignPanelSource();
            }
        }
        #endregion

        #region 能否悬挂
        private bool canHanging = true;
        public bool CanHanging
        {
            get => canHanging;
            set => SetProperty(ref canHanging,value);
        }
        #endregion

        #region 拥有正反面
        private bool haveBackFace = true;

        public bool HaveBackFace
        {
            get => haveBackFace;
            set => SetProperty(ref haveBackFace, value);
        }
        #endregion

        #region 能发光
        private bool canGlowing = true;

        public bool CanGlowing
        {
            get => canGlowing;
            set => SetProperty(ref canGlowing, value);
        }
        #endregion

        #region 是否为反面
        public bool isBack = false;
        public bool IsBack
        {
            get => isBack;
            set
            {
                SetProperty(ref isBack, value);
                FaceSwitcher();
            }
        }
        #endregion

        #region 正反面发光
        private bool isFrontGlowing;

        public bool IsFrontGlowing
        {
            get => isFrontGlowing;
            set => SetProperty(ref isFrontGlowing,value);
        }
        private bool isBackGlowing;

        public bool IsBackGlowing
        {
            get => isBackGlowing;
            set => SetProperty(ref isBackGlowing, value);
        }
        #endregion

        #region 显示结果
        private bool showResult;

        public bool ShowResult
        {
            get => showResult;
            set => SetProperty(ref showResult, value);
        }

        #endregion

        #region 普通字体与混淆字体
        string commonFontFamily = "Microsoft YaHei UI";
        string obfuscatedFontFamily = "Bitstream Vera Sans Mono";
        #endregion

        #region 悬浮菜单
        Popup popup = new()
        {
            IsOpen = false,
            Placement = PlacementMode.AbsolutePoint,
            StaysOpen = false
        };
        #endregion

        #region 当前光标所在的文本对象引用
        private RichRun currentRichRun = null;
        private IContainerProvider _container;
        private string icon_path = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\SignView\\images\\icon.png";

        public RichRun CurrentRichRun
        {
            get => currentRichRun;
            set => SetProperty(ref currentRichRun, value);
        }
        #endregion

        #endregion

        public SignPageViewModel(IContainerProvider container)
        {
            #region 处理正反面文档
            SignPanelSource = new()
            {
                CacheOption = BitmapCacheOption.OnLoad
            };
            for (int i = 0; i < 4; i++)
            {
                SignDocuments[0].Blocks.Add(new RichParagraph() { FontFamily = new FontFamily(commonFontFamily), FontSize = 40, TextAlignment = TextAlignment.Center, Margin = new(0, 2.5, 0, 2.5) });
                SignDocuments[1].Blocks.Add(new RichParagraph() { FontFamily = new FontFamily(commonFontFamily), FontSize = 40, TextAlignment = TextAlignment.Center, Margin = new(0, 2.5, 0, 2.5) });
            }
            #endregion
            _container = container;
        }

        /// <summary>
        /// 载入告示牌页面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void SignPage_Loaded(object sender,RoutedEventArgs e)
        {
            SignViewModel signDataContext = Window.GetWindow(sender as SignPageView).DataContext as SignViewModel;
            VersionSource = signDataContext.VersionSource;

            #region 异步载入告示牌类型和版本映射图
            //载入进程锁
            object tagItemsLock = new();
            BindingOperations.EnableCollectionSynchronization(SignTypeSource, tagItemsLock);
            await Task.Run(async () =>
            {
                DataCommunicator dataCommunicator = DataCommunicator.GetDataCommunicator();
                DataTable ItemTable = await dataCommunicator.GetData("SELECT * FROM SignTypes");
                string currentPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
                List<string> typeSource = [];
                foreach (DataRow item in ItemTable.Rows)
                {
                    string id = item["id"].ToString();
                    string version = item["version"].ToString();
                    if (!TypeVersionMap.TryGetValue(version, out List<string> value))
                        TypeVersionMap.Add(version, [id]);
                    else
                        value.Add(id);
                    typeSource.Add(id);
                }
                typeSource.Sort();
                foreach (var item in typeSource)
                    TypeSource.Add(new TextComboBoxItem() { Text = item });
            });
            #endregion
        }

        public void SignID_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FrameworkElement frameworkElement = sender as FrameworkElement;
            RichTabItems richTabItems = frameworkElement.FindParent<SignPageView>().Parent as RichTabItems;
            richTabItems.Header = SelectedSignType.Text;
        }

        public async void Version_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CancellationTokenSource cancellationTokenSource = new();
            CancellationToken cancellationToken = new();
            await Parallel.ForAsync(0, VersionComponents.Count, async (i, cancellationTokenSource) =>
            {
                if (VersionComponents[i] is StylizedTextBox && CurrentMinVersion < 1130)
                {
                    StylizedTextBox stylizedTextBox = VersionComponents[i] as StylizedTextBox;
                    if (stylizedTextBox.richTextBox.Document.Blocks.Count > 0)
                    {
                        Paragraph paragraph = stylizedTextBox.richTextBox.Document.Blocks.FirstBlock as Paragraph;
                        if (paragraph.Inlines.Count > 0)
                        {
                            RichRun richRun = paragraph.Inlines.FirstInline as RichRun;
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                IsNoStyleText = richRun.Text.Trim().Length > 0;
                            });
                        }
                        else
                            IsNoStyleText = false;
                    }
                    else
                        IsNoStyleText = false;
                }
                else
                    IsNoStyleText = true;
                await VersionComponents[i].Upgrade(CurrentMinVersion);
            });
            await Parallel.ForEachAsync(VersionNBTList, async (item, cancellationToken) =>
            {
                await Task.Delay(0, cancellationToken);
                item.Value.Invoke(item.Key, null);
            });
        }

        [RelayCommand]
        private void SetBold()
        {
            if (SignTextEditor.Selection is null || SignTextEditor.Selection.Text.Length == 0)
                return;
            TextRange textRange = new(SignTextEditor.Selection.Start, SignTextEditor.Selection.End);
            if (Equals(textRange.GetPropertyValue(TextBlock.FontWeightProperty), FontWeights.Normal))
                textRange.ApplyPropertyValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            else
                textRange.ApplyPropertyValue(TextBlock.FontWeightProperty, FontWeights.Normal);
        }

        [RelayCommand]
        private void SetItalic()
        {
            if (SignTextEditor.Selection is null || SignTextEditor.Selection.Text.Length == 0)
                return;
            TextRange textRange = new(SignTextEditor.Selection.Start, SignTextEditor.Selection.End);
            if (Equals(textRange.GetPropertyValue(TextBlock.FontStyleProperty), FontStyles.Normal))
                textRange.ApplyPropertyValue(TextBlock.FontStyleProperty, FontStyles.Italic);
            else
                textRange.ApplyPropertyValue(TextBlock.FontStyleProperty, FontStyles.Normal);
        }

        [RelayCommand]
        private void SetUnderlined()
        {
            if (SignTextEditor.Selection is null || SignTextEditor.Selection.Text.Length == 0)
                return;
            TextRange textRange = new(SignTextEditor.Selection.Start, SignTextEditor.Selection.End);

            if (textRange.GetPropertyValue(TextBlock.TextDecorationsProperty) is TextDecorationCollection current_decorations)
            {
                int strikethrough_index = current_decorations.IndexOf(TextDecorations.Strikethrough.First());
                if (!current_decorations.Contains(TextDecorations.Baseline.First()))
                {
                    TextDecorationCollection textDecorations =
                    [
                        ..(strikethrough_index != -1 ? TextDecorations.Strikethrough : []),
                        TextDecorations.Baseline[0]
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
        private void SetStrikethough()
        {
            if (SignTextEditor.Selection is null || SignTextEditor.Selection.Text.Length == 0)
                return;
            TextRange textRange = new(SignTextEditor.Selection.Start, SignTextEditor.Selection.End);

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

        [RelayCommand]
        private void SetObfuscated()
        {
            if (SignTextEditor.Selection is null) return;
            if (SignTextEditor.Selection.Text.Length == 0) return;

            if (SignTextEditor.Selection.Start.Paragraph is not RichParagraph startRichParagraph || SignTextEditor.Selection.End.Paragraph is not RichParagraph endRichParagraph)
                return;
            //获取选区头部所在的文本块以及它所在段落中的索引
            RichRun StartRichRun = SignTextEditor.Selection.Start.Parent as RichRun;
            RichRun EndRichRun = SignTextEditor.Selection.End.Parent as RichRun;
            List<RichRun> CurrentRichRuns = SignTextEditor.Selection.Start.Paragraph.Inlines.ToList().ConvertAll(item => item as RichRun);

            //判断选区首尾是否在同一个段落中
            if (Equals(startRichParagraph, endRichParagraph))
            {
                if (Equals(StartRichRun, EndRichRun))
                {
                    //关闭混淆
                    if (StartRichRun.ObfuscateTimer.IsEnabled)
                    {
                        StartRichRun.IsObfuscated = false;
                        StartRichRun.ObfuscateTimer.IsEnabled = false;
                        StartRichRun.Text = StartRichRun.UID;
                        StartRichRun.FontFamily = new FontFamily(commonFontFamily);
                    }
                    else//开启混淆
                    {
                        ObfuscateRunHelper.Run(ref SignTextEditor);
                        StartRichRun.UID = StartRichRun.Text;
                        FontFamily fontFamily = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + "resources\\"), "./#"+ obfuscatedFontFamily);
                        StartRichRun.FontFamily = fontFamily;
                        StartRichRun.IsObfuscated = true;
                        StartRichRun.ObfuscateTimer.IsEnabled = true;
                    }
                }
                else
                {
                    int StartRichRunIndex = CurrentRichRuns.IndexOf(StartRichRun);
                    int EndRichRunIndex = CurrentRichRuns.IndexOf(EndRichRun);
                    //检查未开启混淆的文本块数量
                    int IsNotEnableCount = 0;
                    //未混淆文本块链表
                    List<RichRun> IsNotEnableRichRuns = [];
                    CurrentRichRuns.All(item =>
                    {
                        if (!item.ObfuscateTimer.IsEnabled)
                        {
                            IsNotEnableRichRuns.Add(item);
                            IsNotEnableCount++;
                        }
                        return true;
                    });
                    //大于0则把未开启混淆的文本块开启混淆
                    if (IsNotEnableCount > 0)
                    {
                        ObfuscateRunHelper.Run(ref SignTextEditor);
                        IsNotEnableRichRuns.All(item =>
                        {
                            item.UID = item.Text;
                            FontFamily fontFamily = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + "resources\\"), "./#"+ obfuscatedFontFamily);
                            item.FontFamily = fontFamily;
                            item.IsObfuscated = true;
                            item.ObfuscateTimer.IsEnabled = true;
                            return true;
                        });
                    }
                    else//否则把所有选中的文本块去掉混淆
                    {
                        foreach (RichRun item in CurrentRichRuns)
                        {
                            item.IsObfuscated = false;
                            item.ObfuscateTimer.IsEnabled = false;
                            item.Text = item.UID;
                            item.FontFamily = new FontFamily(commonFontFamily);
                        }
                    }
                }
            }
            else//不在同一个段落中则把在选区中的两个段落之间的所有文本块合并处理
            {
                #region 获取起始与末尾两个段落之间所有段落的文本块
                List<RichParagraph> richParagraphs = SignTextEditor.Document.Blocks.ToList().ConvertAll(item => item as RichParagraph);
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
                List<RichRun> UnableRichRuns = [];
                //记录未开启混淆的文本块数量
                for (int i = StartRichRunIndex; i < CurrentRichRuns.Count; i++)
                {
                    if (!CurrentRichRuns[i].ObfuscateTimer.IsEnabled)
                    {
                        UnableRichRuns.Add(CurrentRichRuns[i]);
                        StartUnableCount++;
                    }
                }
                for (int i = 0; i <= EndRichRunIndex; i++)
                {
                    if (!endRichRuns[i].ObfuscateTimer.IsEnabled)
                    {
                        UnableRichRuns.Add(endRichRuns[i]);
                        EndUnableCount++;
                    }
                }
                //总数量为0则说明选区内文本块已全部开启混淆,则需要把所有文本块关闭混淆
                if (StartUnableCount + EndUnableCount == 0)
                {
                    foreach (var item in CurrentRichRuns)
                    {
                        item.IsObfuscated = false;
                        item.ObfuscateTimer.IsEnabled = false;
                        item.UID = item.Text;
                        item.FontFamily = new FontFamily(commonFontFamily);
                    }
                }
                else//否则把未开启混淆的文本块开启混淆
                {
                    ObfuscateRunHelper.Run(ref SignTextEditor);
                    foreach (var item in UnableRichRuns)
                    {
                        item.UID = item.Text;
                        FontFamily fontFamily = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + "resources\\"), "./#"+ obfuscatedFontFamily);
                        item.FontFamily = fontFamily;
                        item.IsObfuscated = true;
                        item.ObfuscateTimer.IsEnabled = true;
                    }
                }
            }
        }

        [RelayCommand]
        private void Reset()
        {
            if (SignTextEditor.Selection.End.Parent is RichRun end_run && SignTextEditor.Selection.Start.Parent is RichRun start_run)
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
        private void SetColor()
        {
            TextRange textRange = new(SignTextEditor.Selection.Start, SignTextEditor.Selection.End);
            textRange.ApplyPropertyValue(TextBlock.ForegroundProperty, SelectionColor);
        }

        /// <summary>
        /// 文本编辑器载入事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SignTextEditor_Loaded(object sender, RoutedEventArgs e)
        {
            SignTextEditor = sender as RichTextBox;
            SignTextEditor.Document = SignDocuments[0];
            //设置悬浮菜单
            popup.Child = EventComponent;
            popup.PlacementTarget = SignTextEditor;
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
        public void SignTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            #region 如果内容被全部删除,则更新空白文档的排版
            if ((Keyboard.IsKeyUp(Key.Back) || Keyboard.IsKeyUp(Key.Delete)) && SignTextEditor.Document.Blocks.Count <= 1)
            {
                SignTextEditor.CaretBrush = Brushes.Transparent;
                for (int i = 0; i < 4; i++)
                {
                    RichParagraph richParagraph = new() { FontFamily = new FontFamily(commonFontFamily), FontSize = 40, TextAlignment = TextAlignment.Center,Margin = new(0, 2.5, 0, 2.5) };
                    richParagraph.Inlines.Add(new RichRun());
                    SignTextEditor.Document.Blocks.Add(richParagraph);
                }
                SignTextEditor.CaretBrush = Brushes.White;
            }
            #endregion
            ShowHoverMenu();
        }

        /// <summary>
        /// 弹出悬浮菜单
        /// </summary>
        private void ShowHoverMenu()
        {
            bool SameRun = false;
            if (SignTextEditor.Selection.Text.Trim().Length > 0)
            {
                Point relativePoint = SignTextEditor.Selection.End.GetCharacterRect(LogicalDirection.Forward).TopLeft;
                Point absolutePoint = SignTextEditor.PointToScreen(relativePoint);
                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;
                bool isBottom = absolutePoint.Y > screenHeight / 2;
                bool isLeft = absolutePoint.X < screenWidth / 2;

                popup.HorizontalOffset = absolutePoint.X;
                popup.VerticalOffset = absolutePoint.Y;

                if (isBottom)
                {
                    // 显示在光标上方
                    //popup.VerticalOffset -= 50;
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
                    //popup.VerticalOffset -= 50;
                }

                #region 设置数据
                RichRun start_run = SignTextEditor.Selection.Start.Parent as RichRun;
                RichRun end_run = SignTextEditor.Selection.End.Parent as RichRun;
                //Paragraph start_paragraph = start_run.Parent as Paragraph;
                //Paragraph end_paragraph = end_run.Parent as Paragraph;
                SameRun = Equals(start_run, end_run);
                if (SameRun)
                {
                    #region 同步数据
                    CurrentRichRun = start_run;

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

                    //EventComponent.EnableClickEvent.IsChecked = CurrentRichRun.HasClickEvent;
                    //EventComponent.EnableHoverEvent.IsChecked = CurrentRichRun.HasHoverEvent;
                    //EventComponent.EnableInsertion.IsChecked = CurrentRichRun.HasInsertion;

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
            popup.StaysOpen = popup.IsOpen = SignTextEditor.Selection.Text.Trim().Length > 0 && SameRun;
        }

        /// <summary>
        /// 把告示牌面板改成悬挂式
        /// </summary>
        private void UpdateSignPanelSource()
        {
        }

        /// <summary>
        /// 切换告示牌正反面
        /// </summary>
        private void FaceSwitcher()
        {
            if (SignTextEditor is null) 
                return;
            SignTextEditor.Document = IsBack ? SignDocuments[1] : SignDocuments[0];
        }

        [RelayCommand]
        /// <summary>
        /// 保存告示牌
        /// </summary>
        private void Save()
        {
        }

        [RelayCommand]
        /// <summary>
        /// 生成告示牌
        /// </summary>
        public void Run()
        {
            Result = "";
            StringBuilder frontResult = new();
            StringBuilder backResult = new();
            bool IsOrBigger1_20 = SelectedVersion.Text == "1.20.0";
            if (IsOrBigger1_20)
            {
                frontResult.Append("messages:[");
                backResult.Append("messages:[");
            }

            #region 正面文档
            for (int i = 0; i < SignDocuments[0].Blocks.Count; i++)
            {
                List<RichParagraph> richParagraphs = SignDocuments[0].Blocks.ToList().ConvertAll(item => item as RichParagraph);
                RichParagraph currentRichParagraph = richParagraphs[i];
                if (!IsOrBigger1_20)
                    frontResult.Append("Text" + (i + 1) + ":'[");
                else
                    frontResult.Append("'[");
                foreach (RichRun richRun in currentRichParagraph.Inlines.Cast<RichRun>())
                {
                    if (richRun.Text.Trim().Length > 0)
                        frontResult.Append(richRun.Result);
                }
                if (frontResult.ToString().EndsWith(','))
                    frontResult = frontResult.Remove(frontResult.Length - 1, 1);
                frontResult.Append("]',");
            }
            frontResult = frontResult.Replace("'[]'", "").Replace(",", "");
            if (frontResult.ToString() == "messages:[")
                frontResult.Clear();
            #endregion
            #region 反面文档
            if (IsOrBigger1_20)
            {
                for (int i = 0; i < SignDocuments[1].Blocks.Count; i++)
                {
                    List<RichParagraph> richParagraphs = SignDocuments[1].Blocks.ToList().ConvertAll(item => item as RichParagraph);
                    RichParagraph currentRichParagraph = richParagraphs[i];
                    backResult.Append("'[");
                    foreach (RichRun richRun in currentRichParagraph.Inlines.Cast<RichRun>())
                    {
                        if (richRun.Text.Trim().Length > 0)
                            backResult.Append(richRun.Result);
                    }
                    if (backResult.ToString().EndsWith(','))
                        backResult = backResult.Remove(backResult.Length - 1, 1);
                    backResult.Append("]',");
                }
            }
            backResult = backResult.Replace("'[]'", "").Replace(",", "");
            if (backResult.ToString() == "messages:[")
                backResult.Clear();
            #endregion

            if (IsOrBigger1_20)
            {
                if (frontResult.Length > 0)
                    frontResult.Append(']');
                if (backResult.Length > 0)
                    backResult.Append(']');
            }

            if (CanGlowing && IsFrontGlowing && IsOrBigger1_20)
                frontResult.Append(",has_glowing_text:1b");
            else
                if (CanGlowing && IsFrontGlowing && !IsOrBigger1_20)
                frontResult.Append(",GlowingText:1b");
            else
                if(frontResult.Length > 0)
                frontResult = frontResult.Remove(frontResult.Length - 1, 1);

            if (CanGlowing && IsBackGlowing && IsOrBigger1_20)
                backResult.Append(",has_glowing_text:1b");
            else
                if (CanGlowing && IsBackGlowing && !IsOrBigger1_20)
                backResult.Append(",GlowingText:1b");
            else
                if(backResult.Length > 0)
                backResult = backResult.Remove(backResult.Length - 1, 1);

            if (CanWaxed && IsWaxed)
                Result += "is_waxed:1b,";

            string signId = CanHanging && IsHanging ?SelectedSignType.Text+"_hanging_sign": SelectedSignType.Text + "_sign";

            if (IsOrBigger1_20)
                Result = Result + "front_text:{" + frontResult.ToString().TrimEnd(',') + "},back_text:{" + backResult.ToString().TrimEnd(',') + "}";
            else
                Result = frontResult.ToString();

            if(Setblock)
            {
                if (CurrentMinVersion >= 1130)
                    Result = "/give @p " + signId + "{BlockEntityTag:{" + Result + "}}";
                else
                    Result = "/give @p standing_sign 1 0" + "{BlockEntityTag:{" + Result + "}}";
            }
            else
            {
                if (CurrentMinVersion >= 1130)
                    Result = "/setblock ~ ~1 ~ " + signId + "{" + Result + "}";
                else
                    Result = "/setblock ~ ~1 ~ standing_sign {" + Result + "}";
            }
            if(ShowResult)
            {
                DisplayerView displayer = _container.Resolve<DisplayerView>();
                if (displayer is not null && displayer.DataContext is DisplayerViewModel displayerViewModel)
                {
                    displayer.Show();
                    displayerViewModel.GeneratorResult(Result, "告示牌", icon_path);
                }
            }
            else
            {
                Clipboard.SetText(Result);
                Message.PushMessage("生成成功！告示牌已进入剪切板",MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 版本蒙版,关闭指定版本或版本区间里不存在的功能
        /// </summary>
        private void VersionMask()
        {
            CanWaxed = HaveBackFace = CanGlowing = CanHanging = false;
            if(CurrentMinVersion >= 1193)
            CanGlowing = CanHanging = true;
            if(CurrentMinVersion >= 117)
            CanGlowing = true;
            if(SelectedVersion == VersionSource[0])
                CanWaxed = HaveBackFace = CanGlowing = CanHanging = true;
            VersionTypesFilter(SelectedVersion.Text);
        }

        /// <summary>
        /// 在不同版本下过滤不应该出现的告示牌类型
        /// </summary>
        /// <param name="version"></param>
        private void VersionTypesFilter(string version)
        {
            if (TypeVersionMap.Count == 0)
                return;
            TypeSource.Clear();
            List<string> Result = [];
            string currentVersion = version.Replace("+", "").Replace("-", "").Replace(".","");
            foreach (KeyValuePair<string, List<string>> item in TypeVersionMap)
            {
                string itemCopy = item.Key.Replace("+", "").Replace("-", "").Replace(".", "");
                if (itemCopy == "all" || itemCopy.Contains(currentVersion) || (!itemCopy.Contains('~') && !currentVersion.Contains('~') && (int.Parse(itemCopy) <= int.Parse(currentVersion))))
                    for (int i = 0; i < item.Value.Count; i++)
                        Result.Add(item.Value[i]);
                else
                {
                    if (itemCopy.Contains('~'))
                        itemCopy = itemCopy.Replace(".", "").Split('~')[1];
                    if (currentVersion.Contains('~'))
                        currentVersion = currentVersion.Split('~')[1];
                    if (int.Parse(itemCopy) <= int.Parse(currentVersion) || VersionSource[^1].Text.Contains(version))
                        for (int i = 0; i < item.Value.Count; i++)
                            Result.Add(item.Value[i]);
                }
            }
            Result.Sort();
            foreach (var item in Result)
                TypeSource.Add(new TextComboBoxItem(){ Text = item });
            SelectedSignType = TypeSource[0];
        }

        /// <summary>
        /// 处理告示牌键盘抬起事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SignTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            #region 控制正反面文档段落数量与按下回车的处理
            if (SignTextEditor.Document.Blocks.Count >= 4 && Keyboard.IsKeyDown(Key.Enter))
                e.Handled = true;
            else
            if(SignTextEditor.Document.Blocks.Count < 4 && Keyboard.IsKeyDown(Key.Enter))
            {
                RichParagraph richParagraph = new()
                {
                    FontFamily = new FontFamily(commonFontFamily),
                    FontSize = 40,
                    TextAlignment = TextAlignment.Center,
                    Margin = new(0, 0.25, 0, 0.25)
                };
                if (SignTextEditor.CaretPosition.IsEndOfBlock())
                {
                    SignTextEditor.Document.Blocks.InsertAfter(SignTextEditor.CaretPosition.Paragraph, richParagraph);
                    SignTextEditor.CaretPosition = richParagraph.ContentStart;
                }
                else
                {
                    if (SignTextEditor.CaretPosition.IsAtLineStartPosition)
                    {
                        RichParagraph current = SignTextEditor.CaretPosition.Paragraph as RichParagraph;
                        SignTextEditor.Document.Blocks.InsertBefore(SignTextEditor.CaretPosition.Paragraph, richParagraph);
                        SignTextEditor.CaretPosition = current.ContentStart;
                    }
                    else//分离光标所在文本块左右侧的文本，并继承左侧属性，把分离出来的文本对象加入新段落中，再将新段落插入到当前段落后，更新光标位置
                    {
                        RichRun richRun = SignTextEditor.CaretPosition.Parent as RichRun;
                        TextRange CutDownStartRange = new(SignTextEditor.CaretPosition, richRun.ContentStart);
                        TextRange CutDownEndRange = new(SignTextEditor.CaretPosition, richRun.ContentEnd);
                        string StartText = CutDownStartRange.Text;
                        string EndText = CutDownEndRange.Text;
                        richRun.Text = richRun.UID = StartText;
                        RichRun SpiltRichRun = new RichRun();
                        SpiltRichRun.UID = SpiltRichRun.Text = EndText;
                        SpiltRichRun.ObfuscateTimer.IsEnabled = richRun.ObfuscateTimer.IsEnabled;
                        SignTextEditor.CaretPosition.Paragraph.Inlines.InsertAfter(richRun, SpiltRichRun);
                        List<RichRun> CurrentRuns = SignTextEditor.CaretPosition.Paragraph.Inlines.ToList().ConvertAll(item => item as RichRun);
                        int CurrentIndex = CurrentRuns.IndexOf(richRun);
                        for (int i = CurrentIndex + 1; i < CurrentRuns.Count; i++)
                        {
                            richParagraph.Inlines.Add(CurrentRuns[i]);
                            SignTextEditor.CaretPosition.Paragraph.Inlines.Remove(CurrentRuns[i]);
                        }
                        SignTextEditor.Document.Blocks.InsertAfter(SignTextEditor.CaretPosition.Paragraph, richParagraph);
                        SignTextEditor.CaretPosition = richParagraph.ContentStart;
                    }
                }
                SignTextEditor.Focus();
                e.Handled = true;
            }
            #endregion
            #region 控制粘贴
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.V)
            {
                e.Handled = true;
                ClipboardDataHandler();
            }
            #endregion
        }

        /// <summary>
        /// 处理粘贴
        /// </summary>
        private void ClipboardDataHandler()
        {
            string data = Clipboard.GetText();
            string[] dataArray = data.Split('\n');
            while (SignTextEditor.Document.Blocks.Count < 4)
                SignTextEditor.Document.Blocks.Add(new Paragraph());
            int currentIndex = SignTextEditor.Document.Blocks.ToList().IndexOf(SignTextEditor.Selection.Start.Paragraph);
            int currentBlockCount = 4 - currentIndex;
            RichParagraph currentParagraph = SignTextEditor.Selection.Start.Paragraph as RichParagraph;
            List<RichParagraph> currentParagraphs = SignTextEditor.Document.Blocks.ToList().ConvertAll(item => item as RichParagraph);
            _ = currentParagraphs.Where(item => { item.Inlines.Clear(); return true; });
            for (int i = 0; i < currentBlockCount; i++)
            {
                RichParagraph paragraph = currentParagraphs[currentIndex + i];
                paragraph.Inlines.Add(new Run() { Text = dataArray[i] });
            }
        }
    }
}