using CBHK.CustomControl.Container;
using CBHK.CustomControl.VectorComboBox;
using CBHK.Domain;
using CBHK.Model.Data;
using CBHK.Utility.Visual.MessageTip;
using CBHK.View;
using CBHK.View.Component.Sign;
using CBHK.ViewModel.Component.Sign;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Ioc;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CBHK.ViewModel.Generator
{
    public partial class SignViewModel : ObservableObject
    {
        #region Field
        private MessagePopup messagePopup = new();
        /// <summary>
        /// 主页
        /// </summary>
        private Window home = null;
        private ImageSource icon = new BitmapImage();
        private IContainerProvider container;
        string iconPath = "pack://application:,,,/CBHK;component/Resource/Common/Image/SpawnerIcon/IconSign.png";
        #endregion

        #region Property
        /// <summary>
        /// 告示牌数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<VectorRichTabItem> signList = [
            new VectorRichTabItem()
            {
                Title = "acacia",
                Foreground = new SolidColorBrush(Colors.White),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
                Style = Application.Current.Resources["VectorRichTabItemStyle"] as Style
            }
        ];

        /// <summary>
        /// 已选中的告示牌
        /// </summary>
        [ObservableProperty]
        public VectorRichTabItem selectedItem;

        [ObservableProperty]
        private bool showResult;

        /// <summary>
        /// 版本数据源。真实列表在构造函数里按注册表目录填（只有它知道这台机器上有哪些版本）；
        /// 下面这份内置清单只是"注册表读不到"时的兜底，注意它只覆盖 1.13~1.20，别当默认来源用。
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<VectorTextComboBoxItem> versionSource = [
            new VectorTextComboBoxItem() { ItemID = "1.20.0", Text = "1.20.0" },
            new VectorTextComboBoxItem() { ItemID = "1.19.4", Text = "1.19.4" },
            new VectorTextComboBoxItem() { ItemID = "1.19.3", Text = "1.19.3" },
            new VectorTextComboBoxItem() { ItemID = "1.17.0", Text = "1.17.0" },
            new VectorTextComboBoxItem() { ItemID = "1.16.0", Text = "1.16.0" },
            new VectorTextComboBoxItem() { ItemID = "1.14.0", Text = "1.14.0" },
            new VectorTextComboBoxItem() { ItemID = "1.13.0", Text = "1.13.0" }
        ];
        #endregion

        #region Method
        public SignViewModel(IContainerProvider Container,MainView mainView)
        {
            container = Container;
            home = mainView;
            LoadVersionSource();
            foreach (VectorRichTabItem tab in SignList)
            {
                AttachCloseCommand(tab);
            }
            VectorRichTabItem firstTab = SignList.FirstOrDefault();
            if (firstTab is not null)
            {
                Task.Run(async () =>
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        // 标签页现在可以关掉了：异步回来时这一页可能已经被用户关掉，别再抢 SignList[0]
                        if (firstTab is null || !SignList.Contains(firstTab))
                        {
                            return;
                        }
                        SignPageView signPageView = container.Resolve<SignPageView>();
                        signPageView.DataContext = container.Resolve<SignPageViewModel>();
                        firstTab.Content = signPageView;
                        firstTab.FontWeight = FontWeights.Normal;
                    });
                });
            }
            if (File.Exists(iconPath))
            {
                icon = new BitmapImage(new Uri(iconPath, UriKind.Absolute));
            }
        }
        /// <summary>
        /// 版本列表以注册表目录为准（Resource/Registries 下的 *.json，文件名就是版本键）。
        /// 以前这里是写死的 7 个版本（1.13~1.20），1.20.1 以后和 26.x 全都没有 ——
        /// 选不到版本，版本能力位（已裱/正反面/悬挂/荧光）自然就一直是灰的。
        /// </summary>
        private void LoadVersionSource()
        {
            RegistryDataService registryData = container.Resolve<RegistryDataService>();
            if (registryData is not { IsLoaded: true })
            {
                return;   // 注册表读不到：保留内置兜底清单，至少控件不是空的
            }

            VersionSource = [.. registryData.Versions
                .Reverse()   // Versions 是升序，下拉里新的放前面
                .Select(version => new VectorTextComboBoxItem { ItemID = version, Text = version })];
        }
        #endregion

        #region Event
        [RelayCommand]
        /// <summary>
        /// 返回主页
        /// </summary>
        /// <param name="win"></param>
        private void Return(Window win)
        {
            home.WindowState = WindowState.Normal;
            home.Show();
            home.ShowInTaskbar = true;
            home.Focus();
            win.Close();
        }

        [RelayCommand]
        /// <summary>
        /// 生成所有告示牌
        /// </summary>
        private async Task Run()
        {
            DisplayerView displayer = container.Resolve<DisplayerView>();
            if (displayer is not null && displayer.DataContext is DisplayerViewModel displayerViewModel)
            {
                StringBuilder result = new();
                await Task.Run(async () =>
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        foreach (var item in SignList)
                        {
                            SignPageView signPage = item.Content as SignPageView;
                            SignPageViewModel signPageDataContext = signPage.DataContext as SignPageViewModel;
                            signPageDataContext.Run();
                            if (!ShowResult)
                                result.Append(signPageDataContext.Result + "\r\n");
                            displayerViewModel.GeneratorResult(signPageDataContext.Result, "告示牌", iconPath);
                        }
                    });
                });
                if (ShowResult)
                {
                    displayer.Show();
                }
                else
                {
                    Clipboard.SetText(result.ToString());
                    messagePopup.PushMessage(new GeneratorMessage()
                    {
                        Message = "生成成功！告示牌已进入剪切板",
                        SubMessage = "告示牌生成器",
                        Icon = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\sign.png", UriKind.RelativeOrAbsolute))
                    });
                }
            }
        }

        [RelayCommand]
        /// <summary>
        /// 添加告示牌
        /// </summary>
        /// <param name="element"></param>
        private void AddSign()
        {
            SignPageView signPage = container.Resolve<SignPageView>();
            // Prism 的 AutoWireViewModel 是"继承到 DataContext 才建"的，这里先兜住 null，
            // 否则下面一取 SignPanelSource 就是 NullReferenceException
            SignPageViewModel pageContext = signPage.DataContext as SignPageViewModel
                ?? container.Resolve<SignPageViewModel>();
            signPage.DataContext = pageContext;
            string signPanelPath = AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\acaciaSignPanel.png";
            if (File.Exists(signPanelPath))
            {
                pageContext.SignPanelSource = new BitmapImage(new Uri(signPanelPath, UriKind.Absolute));
            }
            VectorRichTabItem richTabItems = new()
            {
                Content = signPage,
                // 仓里只有 VectorRichTabItemStyle；以前这里写的 RichTabItemStyle 拿不到样式，
                // 新标签页退化成默认 TabItem —— 连关闭按钮都没有
                Style = Application.Current.Resources["VectorRichTabItemStyle"] as Style,
                Header = "acacia"
            };
            AttachCloseCommand(richTabItems);
            SignList.Add(richTabItems);
            SelectedItem = richTabItems;
        }

        /// <summary>
        /// 给标签页装上关闭命令：tab 模板里的 × 绑的就是这个命令，
        /// 以前没人赋值 → 悬停能看到 × 却点不动（关闭按钮是死按钮）。
        /// </summary>
        private void AttachCloseCommand(VectorRichTabItem tab)
        {
            if (tab is null)
            {
                return;
            }
            tab.CloseCommand = new RelayCommand(() => CloseSignTab(tab));
        }

        /// <summary>关掉一个告示牌标签页；关的是当前选中页就把选中项顺延到下一张。</summary>
        private void CloseSignTab(VectorRichTabItem tab)
        {
            if (tab is null || !SignList.Contains(tab))
            {
                return;
            }
            bool wasSelected = ReferenceEquals(SelectedItem, tab);
            SignList.Remove(tab);
            if (wasSelected)
            {
                SelectedItem = SignList.FirstOrDefault();
            }
        }

        [RelayCommand]
        /// <summary>
        /// 清空告示牌
        /// </summary>
        private void ClearSigns() => SignList.Clear();

        [RelayCommand]
        /// <summary>
        /// 保存所有告示牌
        /// </summary>
        private void SaveAll()
        {

        }

        [RelayCommand]
        /// <summary>
        /// 从剪切板导入数据
        /// </summary>
        private void ImportFromClipboard()
        {

        }

        [RelayCommand]
        /// <summary>
        /// 从文件导入数据
        /// </summary>
        private void ImportFromFile()
        {

        }
        #endregion
    }
}