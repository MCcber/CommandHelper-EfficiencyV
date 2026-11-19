using CBHK.CustomControl.Input;
using CBHK.CustomControl.TextElement;
using CBHK.CustomControl.VectorComboBox;
using CBHK.Domain;
using CBHK.Interface.Visual;
using CBHK.Model.Data;
using CBHK.Utility.Common;
using CBHK.Utility.Data;
using CBHK.Utility.Visual.MessageTip;
using CBHK.View;
using CBHK.View.Component.Sign;
using CBHK.ViewModel.Generator;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
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

namespace CBHK.ViewModel.Component.Sign
{
    public partial class SignPageViewModel : ObservableObject, IPageViewModel
    {
        #region Field
        public string Result = "";
        private IContainerProvider container;
        private RegistryDataService dataRegistry = null;
        private string iconPath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\Sign\Image\icon.png";
        /// <summary>
        /// 需要适应版本变化的特指数据所属控件的事件
        /// </summary>
        public Dictionary<FrameworkElement, Action<FrameworkElement, RoutedEventArgs>> VersionNBTList = [];
        /// <summary>
        /// 告示牌编辑器
        /// </summary>
        private RichTextBox SignTextEditor = null;
        /// <summary>
        /// 是否有低版本样式
        /// </summary>
        private bool IsNoStyleText;
        /// <summary>
        /// 不同版本拥有的类型映射图
        /// </summary>
        private Dictionary<string, List<string>> TypeVersionMap = [];
        /// <summary>
        /// 事件设置控件
        /// </summary>
        //TextEvent EventComponent = new() { };

        #region 悬浮菜单
        Popup popup = new()
        {
            IsOpen = false,
            Placement = PlacementMode.AbsolutePoint,
            StaysOpen = false
        };
        #endregion

        #region 普通字体与混淆字体
        string commonFontFamily = "Microsoft YaHei UI";
        string obfuscatedFontFamily = "Bitstream Vera Sans Mono";
        #endregion

        #endregion

        #region Property
        private MessagePopup messagePopup;
        /// <summary>当前提示条挂在哪扇窗上；换窗口才重建（每次 new 都会往 AdornerLayer 多加一层）。</summary>
        private Window messagePopupHost;

        /// <summary>
        /// 提示条：没被赋值时用到才建（那时窗口已经激活，AdornerLayer 才挂得上；构造期建会拿不到层）。
        /// 必须保留 setter —— IPageViewModel 要求可写。
        /// </summary>
        public MessagePopup MessagePopup
        {
            get => messagePopup ??= new MessagePopup();
            set
            {
                messagePopup = value;
                messagePopupHost = null;
            }
        }

        /// <summary>
        /// 弹提示。宿主窗口：勾了"显示结果"就挂到结果窗口（那时本页可能被挡在后面），
        /// 没勾就挂本页自己的窗口。
        /// </summary>
        private void PushMessage(GeneratorMessage message, Window displayer)
        {
            Window host = displayer ?? Window.GetWindow(SignTextEditor);
            if (messagePopup is null || !ReferenceEquals(messagePopupHost, host))
            {
                messagePopup = new MessagePopup(host);
                messagePopupHost = host;
            }
            messagePopup.PushMessage(message);
        }
        [ObservableProperty]
        private RichRun currentRichRun = null;
        /// <summary>
        /// 告示牌类型
        /// </summary>
        [ObservableProperty]
        private VectorTextComboBoxItem selectedSignType;
        /// <summary>
        /// 告示牌背景
        /// </summary>
        [ObservableProperty]
        public BitmapImage signPanelSource = null;

        /// <summary>
        /// 生成方式：true = 给予（give），false = 放置（setblock）。
        /// 注意属性名必须是 Give —— XAML 上"给予"按钮绑的就是它；
        /// 以前这里叫 Setblock，绑定找不到属性直接静默失败，于是永远输出 setblock。
        /// </summary>
        [ObservableProperty]
        private bool give;

        /// <summary>
        /// 已选择的版本
        /// </summary>
        [ObservableProperty]
        private VectorTextComboBoxItem selectedVersion;

        /// <summary>
        /// 版本数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<VectorTextComboBoxItem> versionSource = [];
        /// <summary>
        /// 告示牌类型数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<VectorTextComboBoxItem> typeSource = [];

        /// <summary>
        /// 正反面文档
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<EnabledFlowDocument> signDocumentList = 
        [
            new EnabledFlowDocument(),
            new EnabledFlowDocument()
        ];

        /// <summary>
        /// 是否被裱
        /// </summary>
        [ObservableProperty]
        public bool isWaxed = true;

        /// <summary>
        /// 版本一变就把四个"能力位"通知出去：它们现在是只读计算属性，
        /// 不主动 OnPropertyChanged 的话按钮的 IsEnabled 绑定不会重算。
        /// </summary>
        partial void OnSelectedVersionChanged(VectorTextComboBoxItem value)
        {
            OnPropertyChanged(nameof(CanWaxed));
            OnPropertyChanged(nameof(HaveBackFace));
            OnPropertyChanged(nameof(CanHanging));
            OnPropertyChanged(nameof(CanGlowing));
            VersionTypesFilter(value?.Text);
        }

        /// <summary>
        /// 可被裱（1.20+）。直接由 SelectedVersion 现算，不再靠"版本切换事件里赋一次值"——
        /// 那条事件没跑到（换 ItemsSource 时的 null 选择 / XAML 触发器没挂上）按钮就永远是灰的。
        /// </summary>
        public bool CanWaxed => IsAtLeast(SelectedVersion?.Text, "1.20");

        /// <summary>
        /// 已选中的颜色
        /// </summary>
        [ObservableProperty]
        private Color selectionColor;

        /// <summary>
        /// 是否悬挂
        /// </summary>
        [ObservableProperty]
        private bool isHanging;

        /// <summary>
        /// 能否悬挂（悬挂告示牌 1.20 才有）
        /// </summary>
        public bool CanHanging => IsAtLeast(SelectedVersion?.Text, "1.20");

        /// <summary>
        /// 拥有正反面（front_text/back_text 1.20 才有）
        /// </summary>
        public bool HaveBackFace => IsAtLeast(SelectedVersion?.Text, "1.20");

        /// <summary>
        /// 能发光（荧光墨 1.17 才有）
        /// </summary>
        public bool CanGlowing => IsAtLeast(SelectedVersion?.Text, "1.17");
        /// <summary>
        /// 是否为反面
        /// </summary>
        [ObservableProperty]
        public bool isBack = false;

        /// <summary>
        /// 显示结果
        /// </summary>
        [ObservableProperty]
        private bool showResult;

        #region 正反面发光
        [ObservableProperty]
        private bool isFrontGlowing;
        [ObservableProperty]
        private bool isBackGlowing;
        #endregion

        #endregion

        #region Method
        public SignPageViewModel(IContainerProvider container,RegistryDataService dataRegistry)
        {
            this.dataRegistry = dataRegistry;
            this.container = container;

            #region 处理正反面文档
            SignPanelSource = new()
            {
                CacheOption = BitmapCacheOption.OnLoad
            };
            for (int i = 0; i < 4; i++)
            {
                SignDocumentList[0].Blocks.Add(new RichParagraph() { FontFamily = new FontFamily(commonFontFamily), FontSize = 40, TextAlignment = TextAlignment.Center, Margin = new(0, 2.5, 0, 2.5) });
                SignDocumentList[1].Blocks.Add(new RichParagraph() { FontFamily = new FontFamily(commonFontFamily), FontSize = 40, TextAlignment = TextAlignment.Center, Margin = new(0, 2.5, 0, 2.5) });
            }
            #endregion
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

                    //BindingOperations.SetBinding(EventComponent.EnableClickEvent, ToggleButton.IsCheckedProperty, HaveClickEventBinder);
                    //BindingOperations.SetBinding(EventComponent.EnableHoverEvent, ToggleButton.IsCheckedProperty, HaveHoverEventBinder);
                    //BindingOperations.SetBinding(EventComponent.EnableInsertion, ToggleButton.IsCheckedProperty, HaveInsertionBinder);

                    //EventComponent.ClickEventPanel.Visibility = CurrentRichRun.HasClickEvent ? Visibility.Visible : Visibility.Collapsed;
                    //EventComponent.HoverEventPanel.Visibility = CurrentRichRun.HasHoverEvent ? Visibility.Visible : Visibility.Collapsed;
                    //EventComponent.InsertionPanel.Visibility = CurrentRichRun.HasInsertion ? Visibility.Visible : Visibility.Collapsed;

                    //EventComponent.EnableClickEvent.IsChecked = CurrentRichRun.HasClickEvent;
                    //EventComponent.EnableHoverEvent.IsChecked = CurrentRichRun.HasHoverEvent;
                    //EventComponent.EnableInsertion.IsChecked = CurrentRichRun.HasInsertion;

                    //BindingOperations.SetBinding(EventComponent.ClickEventActionBox, Selector.SelectedItemProperty, ClickEventActionBinder);
                    //BindingOperations.SetBinding(EventComponent.HoverEventActionBox, Selector.SelectedItemProperty, HoverEventActionBinder);

                    //BindingOperations.SetBinding(EventComponent.ClickEventValueBox, TextBox.TextProperty, ClickEventValueBinder);
                    //BindingOperations.SetBinding(EventComponent.HoverEventValueBox, TextBox.TextProperty, HoverEventValueBinder);
                    //BindingOperations.SetBinding(EventComponent.InsertionValueBox, TextBox.TextProperty, InsertionValueBinder);
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
        /// 切换告示牌正反面
        /// </summary>
        private void FaceSwitcher()
        {
            if (SignTextEditor is null)
                return;
            SignTextEditor.Document = IsBack ? SignDocumentList[1] : SignDocumentList[0];
        }

        /// <summary>
        /// 版本蒙版,关闭指定版本或版本区间里不存在的功能
        /// </summary>
        /// <summary>
        /// 把 NBT 片段包成命令。三条版本线：
        /// - 1.13 以前：give 的 NBT 写在 damage 位后面，物品只能用 standing_sign
        /// - 1.13 ~ 1.20.4：give 走物品 NBT（BlockEntityTag）
        /// - 1.20.5 起：物品 NBT 被组件取代，give 必须写 [block_entity_data=…]（setblock 不受影响）
        /// </summary>
        private static string BuildSignCommand(string version, string signId, string data, bool give)
        {
            bool is1130OrLater = IsAtLeast(version, "1.13");
            if (give)
            {
                if (!is1130OrLater)
                    // 1.13 以前是 /give <玩家> <物品> [数量] [损伤值] [NBT]：NBT 前必须有空格
                    return "/give @p standing_sign 1 0" + (data.Length > 0 ? " {BlockEntityTag:" + data + "}" : "");
                if (IsAtLeast(version, "1.20.5"))
                    return "/give @p " + signId + (data.Length > 0 ? "[block_entity_data=" + data + "]" : "") + " 1";
                return "/give @p " + signId + (data.Length > 0 ? "{BlockEntityTag:" + data + "}" : "");
            }

            // 放置：方块实体的 NBT 一直是花括号写法，1.20.5 的组件改动不影响它
            if (is1130OrLater)
                return "/setblock ~ ~1 ~ " + signId + data;
            return "/setblock ~ ~1 ~ standing_sign" + (data.Length > 0 ? " " + data : "");
        }

        /// <summary>某个类型（按"首次出现的版本"分桶）在当前版本下是否可见。</summary>
        private static bool IsSignTypeVisibleInVersion(string introducedVersion, string currentVersion)
            => introducedVersion == "all" || IsAtLeast(currentVersion, introducedVersion);

        /// <summary>target ≥ minimum。走 VersionRangeChecker 的逐段比较，"26.3" 这类年份式版本也成立。</summary>
        private static bool IsAtLeast(string target, string minimum)
            => !string.IsNullOrWhiteSpace(target) && !VersionRangeChecker.IsLessThan(target, minimum);

        /// <summary>
        /// 在不同版本下过滤不应该出现的告示牌类型
        /// </summary>
        /// <param name="version"></param>
        private void VersionTypesFilter(string version)
        {
            if (TypeVersionMap.Count == 0 || string.IsNullOrWhiteSpace(version))
                return;
            TypeSource.Clear();
            List<string> Result = [];
            foreach (KeyValuePair<string, List<string>> item in TypeVersionMap)
            {
                // 桶的键 = 该类型"首次出现"的版本；当前版本 ≥ 它就该看得见（"all" 是基线桶，永远可见）
                if (!IsSignTypeVisibleInVersion(item.Key, version))
                {
                    continue;
                }
                for (int i = 0; i < item.Value.Count; i++)
                {
                    Result.Add(item.Value[i]);
                }
            }
            Result.Sort();
            foreach (var item in Result)
            {
                TypeSource.Add(new VectorTextComboBoxItem() { Text = item });
            }
            SelectedSignType = TypeSource.FirstOrDefault();   // 过滤后可能为空，[0] 会直接抛 ArgumentOutOfRange
        }

        /// <summary>
        /// 注册表 id → 告示牌类型裸名：`minecraft:acacia_sign` → `acacia`。
        /// </summary>
        private static string NormalizeSignType(string blockId)
        {
            string name = string.IsNullOrEmpty(blockId) ? string.Empty : blockId.Trim();
            int separator = name.IndexOf(':');
            if (separator >= 0)
            {
                name = name[(separator + 1)..];
            }
            return name.EndsWith("_sign", StringComparison.Ordinal) ? name[..^"_sign".Length] : name;
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
            if (currentIndex < 0) return;
            int currentBlockCount = 4 - currentIndex;
            RichParagraph currentParagraph = SignTextEditor.Selection.Start.Paragraph as RichParagraph;
            List<RichParagraph> currentParagraphs = SignTextEditor.Document.Blocks.ToList().ConvertAll(item => item as RichParagraph);
            _ = currentParagraphs.Where(item => { item.Inlines.Clear(); return true; });
            for (int i = 0; i < currentBlockCount && i < dataArray.Length; i++)
            {
                RichParagraph paragraph = currentParagraphs[currentIndex + i];
                paragraph.Inlines.Add(new Run() { Text = dataArray[i] });
            }
        }

        private void SetColor(Color color)
        {
            if (SignTextEditor is null || SignTextEditor.Selection is null)
            {
                return;
            }
            TextRange textRange = new(SignTextEditor.Selection.Start, SignTextEditor.Selection.End);
            textRange.ApplyPropertyValue(TextBlock.ForegroundProperty, new SolidColorBrush(SelectionColor));
        }
        #endregion

        #region Event
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
            BindingOperations.EnableCollectionSynchronization(TypeSource, tagItemsLock);
            await Task.Run(() =>
            {
                string currentPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
                HashSet<string> knownTypes = [];
                bool baselineLoaded = false;
                foreach (string version in dataRegistry.Versions)
                {
                    // 目录的 block 注册表同时收 wall/hanging 两种变体，告示牌编辑器的类型列表只要普通告示牌。
                    // 取到的是注册表 id（minecraft:acacia_sign），这里统一折成裸类型名（acacia）：
                    // 面板图（<类型>SignPanel.png）、命令（<类型>_sign / <类型>_hanging_sign）、图标（<类型>_sign.png）
                    // 全都按裸名拼，带着 "minecraft:" 和 "_sign" 去拼一个都命中不了。
                    List<string> currentTypes = [.. dataRegistry.GetIds("block", version)
                        .Where(id => id.EndsWith("_sign") && !id.Contains("_wall_sign") && !id.Contains("_hanging_sign"))
                        .Select(NormalizeSignType)];

                    // 只记录在该版本首次出现的类型：过滤逻辑会把命中的每个键的成员整段累加，存全量会让同一类型重复出现
                    List<string> newTypes = [];
                    foreach (string id in currentTypes)
                    {
                        if (knownTypes.Add(id))
                            newTypes.Add(id);
                    }

                    if (!baselineLoaded)
                    {
                        // 目录里最老的版本承接"一开始就有"的类型：过滤逻辑用 "all" 键表示全版本可见，
                        // 否则 1.13.0/1.14.0 这些比目录更老的版本会过滤出空列表，SelectedSignType 直接越界
                        TypeVersionMap["all"] = currentTypes;
                        baselineLoaded = true;
                    }
                    else if (newTypes.Count > 0)
                    {
                        // 键用原始版本号：判定走 VersionRangeChecker 逐段比较，"26.3" 这种年份式版本也算得对
                        // （以前是把点去掉再比整数，26.1 → 261 反而比 1.19.4 → 1194 小，只能不建桶）
                        TypeVersionMap[version] = newTypes;
                    }
                }

                List<string> typeSource = [.. knownTypes];
                typeSource.Sort();
                foreach (var item in typeSource)
                {
                    TypeSource.Add(new VectorTextComboBoxItem() { Text = item });
                }
                // 类型表载入完再按当前版本过滤一次：版本切换事件比这批数据早到，
                // 那时 TypeVersionMap 还是空的，过滤直接早退，首屏会列出所有版本的类型
                VersionTypesFilter(SelectedVersion?.Text);
            });
            #endregion
        }

        /// <summary>
        /// 文本编辑器载入事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SignTextEditor_Loaded(object sender, RoutedEventArgs e)
        {
            SignTextEditor = sender as RichTextBox;
            SignTextEditor.Document = SignDocumentList[0];
            //设置悬浮菜单
            //popup.Child = EventComponent;
            popup.PlacementTarget = SignTextEditor;
        }

        /// <summary>
        /// 正反面切换
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void IsBackUpdate_Click(object sender, RoutedEventArgs e) => FaceSwitcher();

        /// <summary>
        /// 颜色更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ColorPicker_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is VectorColorPicker colorPicker)
            {
                colorPicker.UpdateSelectedColorCallBack -= SetColor;
                colorPicker.UpdateSelectedColorCallBack += SetColor;
            }
        }

        /// <summary>
        /// 告示牌类型切换事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SignTypeSelectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedSignType is null || string.IsNullOrWhiteSpace(SelectedSignType.Text))
            {
                return;
            }
            // 贴图不一定齐（例如新版加的 pale_oak 还没有面板图）：缺就保留当前这张，绝不在这里抛 ——
            // 以前这里是 new BitmapImage(new Uri(...))，文件不存在会直接把异常抛到 SelectionChanged 里。
            string panelPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + SelectedSignType.Text + "SignPanel.png";
            if (!File.Exists(panelPath))
            {
                return;
            }
            SignPanelSource = new BitmapImage(new Uri(panelPath, UriKind.Absolute));
        }

        /// <summary>
        /// 版本切换
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void Version_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedVersion is null || string.IsNullOrWhiteSpace(SelectedVersion.Text))
            {
                // 换 ItemsSource 期间 ComboBox 会先回写一次 null
                return;
            }
            // 能力位（已裱/正反面/悬挂/荧光）已改成由 SelectedVersion 现算的只读属性，
            // 这里只负责刷新版本相关的 NBT 组件
            CancellationTokenSource cancellationTokenSource = new();
            //await Parallel.ForAsync(0, VersionComponents.Count, async (i, cancellationTokenSource) =>
            //{
            //    if (VersionComponents[i] is StylizedTextBox && CurrentMinVersion < 1130)
            //    {
            //        StylizedTextBox stylizedTextBox = VersionComponents[i] as StylizedTextBox;
            //        if (stylizedTextBox.richTextBox.Document.Blocks.Count > 0)
            //        {
            //            Paragraph paragraph = stylizedTextBox.richTextBox.Document.Blocks.FirstBlock as Paragraph;
            //            if (paragraph.Inlines.Count > 0)
            //            {
            //                RichRun richRun = paragraph.Inlines.FirstInline as RichRun;
            //                Application.Current.Dispatcher.Invoke(() =>
            //                {
            //                    IsNoStyleText = richRun.Text.Trim().Length > 0;
            //                    if (!IsNoStyleText)
            //                    {
            //                        Setblock = false;
            //                    }
            //                });
            //            }
            //            else
            //            {
            //                IsNoStyleText = false;
            //            }
            //        }
            //        else
            //        {
            //            IsNoStyleText = false;
            //        }
            //    }
            //    else
            //    {
            //        IsNoStyleText = true;
            //    }
            //    await VersionComponents[i].Upgrade(CurrentMinVersion);
            //});
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
        /// 收集一面牌面上的非空条目：一行（段落）一条，没写字的行直接不产出。
        /// - 1.20+：`'[{"text":"…"}]'`（交给 messages:[…]）
        /// - 1.20 以前：`Text1:'[{"text":"…"}]'`（行号按段落位置固定，空行也占号）
        /// </summary>
        private static List<string> CollectSignFaceEntries(FlowDocument document, bool modern)
        {
            List<string> entries = [];
            if (document is null)
            {
                return entries;
            }

            List<RichParagraph> paragraphs = document.Blocks.ToList().ConvertAll(block => block as RichParagraph);
            for (int i = 0; i < paragraphs.Count; i++)
            {
                StringBuilder payload = new();
                foreach (RichRun richRun in paragraphs[i].Inlines.OfType<RichRun>())
                {
                    if (richRun.Text.Trim().Length == 0)
                    {
                        continue;
                    }
                    payload.Append(richRun.Result);   // 每个 run 自带尾逗号
                }
                if (payload.Length == 0)
                {
                    continue;   // 空行不产出条目 —— 以前这里产出 '[]'，再靠 Replace 补救，第一条为空就漏成前导逗号
                }

                string runs = payload.ToString().TrimEnd(',');
                entries.Add(modern ? "'[" + runs + "]'" : "Text" + (i + 1) + ":'[" + runs + "]'");
            }
            return entries;
        }

        /// <summary>
        /// 一面牌面的 NBT 片段：条目之间、以及与"发光"标记之间一律用逗号 join，空集合返回空串（调用方据此不落这一组）。
        /// </summary>
        private static string BuildSignFaceGroup(List<string> entries, bool modern, bool glowing)
        {
            if (entries.Count == 0)
            {
                // 这一面一个字都没写：整组都不落 —— 没有文本可发光的空组留下只会变成 {front_text:{}} 这种噪音
                return string.Empty;
            }

            List<string> parts = [];
            parts.Add(modern ? "messages:[" + string.Join(",", entries) + "]" : string.Join(",", entries));
            if (glowing)
            {
                parts.Add(modern ? "has_glowing_text:1b" : "GlowingText:1b");
            }
            return string.Join(",", parts);
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
            bool IsOrBigger1_20 = IsAtLeast(SelectedVersion?.Text, "1.20");   // 1.20.1/1.21/26.x 都算"现代格式"

            // 先把"每一面有哪些非空条目"收集出来，再统一 join。
            // 以前是边拼边补逗号、最后再 TrimEnd(',') 补救，所以没写字的时候会留下前导/后导逗号
            // （空行产出 '[]' 再靠 Replace(",'[]'") 兜，第一条为空就直接漏出去）。
            List<string> frontEntries = CollectSignFaceEntries(SignDocumentList[0], IsOrBigger1_20);
            List<string> backEntries = IsOrBigger1_20 ? CollectSignFaceEntries(SignDocumentList[1], IsOrBigger1_20) : [];
            string frontGroup = BuildSignFaceGroup(frontEntries, IsOrBigger1_20, CanGlowing && IsFrontGlowing);
            string backGroup = BuildSignFaceGroup(backEntries, IsOrBigger1_20, CanGlowing && IsBackGlowing);

            if (IsOrBigger1_20)
            {
                List<string> parts = [];
                if (CanWaxed && IsWaxed)
                    parts.Add("is_waxed:1b");
                if (frontGroup.Length > 0)
                    parts.Add("front_text:{" + frontGroup + "}");
                if (backGroup.Length > 0)
                    parts.Add("back_text:{" + backGroup + "}");
                Result = string.Join(",", parts);
            }
            else
            {
                // 1.20 以前只有正面：Text1:'…',Text2:'…'，发光标记是一条独立键
                Result = frontGroup;
            }

            // 这个版本装不下的 NBT：上面已经按版本过滤掉了，这里只是把"去掉了什么"攒出来告诉用户
            List<string> dropped = [];
            if (!IsOrBigger1_20)
            {
                if (backEntries.Count > 0)
                    dropped.Add("反面文本（1.20 起才有）");
                if (IsWaxed)
                    dropped.Add("已裱 is_waxed（1.20 起才有）");
                if (IsHanging)
                    dropped.Add("悬挂（1.20 起才有）");
            }
            if (!CanGlowing)
            {
                if (IsFrontGlowing)
                    dropped.Add("正面荧光（1.17 起才有）");
                if (IsBackGlowing)
                    dropped.Add("反面荧光（1.17 起才有）");
            }

            string version = SelectedVersion?.Text;
            string signType = SelectedSignType?.Text ?? "oak";
            string signId = CanHanging && IsHanging ? signType + "_hanging_sign" : signType + "_sign";
            string data = Result.Length > 0 ? "{" + Result + "}" : "";   // 什么都没写就不要再挂一对空花括号

            Result = BuildSignCommand(version, signId, data, Give);

            // 结果去哪：勾了"显示结果"就进结果窗口（提示也挂那儿），没勾就复制到剪贴板（提示挂本页）
            DisplayerView displayer = null;
            if (ShowResult)
            {
                displayer = container.Resolve<DisplayerView>();
                if (displayer is not null && displayer.DataContext is DisplayerViewModel displayerViewModel)
                {
                    displayer.Show();
                    displayerViewModel.GeneratorResult(Result, "告示牌", iconPath);
                }
            }
            else
            {
                Clipboard.SetText(Result);
            }

            if (dropped.Count > 0)
            {
                PushMessage(new GeneratorMessage()
                {
                    Message = "当前版本不支持：" + string.Join("、", dropped) + "，已自动去除",
                    MessageBrush = Brushes.Red,
                    SubMessage = "告示牌生成器",
                    SubMessageBrush = Brushes.DarkGray,
                    Icon = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\sign.png", UriKind.RelativeOrAbsolute))
                }, displayer);
            }

            if (!ShowResult)
            {
                bool hanged = CanHanging && IsHanging;
                string hangingStateString = hanged ? "hanging_" : "";
                string resultTypeString = signType + '_' + hangingStateString + "sign";
                PushMessage(new GeneratorMessage()
                {
                    Message = "生成成功！告示牌已进入剪切板",
                    SubMessage = "告示牌生成器",
                    Icon = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\" + resultTypeString + ".png", UriKind.RelativeOrAbsolute))
                }, null);
            }
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
        #endregion
    }
}