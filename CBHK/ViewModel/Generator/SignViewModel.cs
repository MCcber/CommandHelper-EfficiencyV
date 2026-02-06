using CBHK.CustomControl.Container;
using CBHK.CustomControl.VectorComboBox;
using CBHK.Model.Common;
using CBHK.Utility.MessageTip;
using CBHK.View;
using CBHK.View.Component.Sign;
using CBHK.ViewModel.Component.Sign;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Ioc;
using System;
using System.Collections.ObjectModel;
using System.IO;
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
        /// <summary>
        /// 主页
        /// </summary>
        private Window home = null;
        private ImageSource icon = new BitmapImage();
        private IContainerProvider container;
        string iconPath = AppDomain.CurrentDomain.BaseDirectory + @"Resource\Configs\SignView\images\icon.png";
        #endregion

        #region Property
        /// <summary>
        /// 告示牌数据源
        /// </summary>
        [ObservableProperty]
        public ObservableCollection<VectorRichTabItem> _signList = [
            new VectorRichTabItem()
            {
                Header = "acacia",
                Foreground = new SolidColorBrush(Colors.White),
                Style = Application.Current.Resources["RichTabItemStyle"] as Style
            }
        ];

        /// <summary>
        /// 已选中的告示牌
        /// </summary>
        [ObservableProperty]
        public VectorRichTabItem _selectedItem;

        [ObservableProperty]
        private bool _showResult;

        [ObservableProperty]
        public ObservableCollection<VectorTextComboBoxItem> _versionSource = [
            new VectorTextComboBoxItem() { Text = "1.20.0" },
            new VectorTextComboBoxItem() { Text = "1.19.4" },
            new VectorTextComboBoxItem() { Text = "1.19.3" },
            new VectorTextComboBoxItem() { Text = "1.17.0" },
            new VectorTextComboBoxItem() { Text = "1.16.0" },
            new VectorTextComboBoxItem() { Text = "1.14.0" },
            new VectorTextComboBoxItem() { Text = "1.13.0" }
        ];
        #endregion

        #region Method
        public SignViewModel(IContainerProvider Container,MainView mainView)
        {
            container = Container;
            home = mainView;
            Task.Run(async () =>
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SignPageView signPageView = container.Resolve<SignPageView>();
                    SignList[0].Content = signPageView;
                    signPageView.DataContext = container.Resolve<SignPageViewModel>();
                    SignList[0].FontWeight = FontWeights.Normal;
                });
            });
            if (File.Exists(iconPath))
            {
                icon = new BitmapImage(new Uri(iconPath, UriKind.Absolute));
            }
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
                    Message.PushMessage(new GeneratorMessage()
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
            string signPanelPath = AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\acaciaSignPanel.png";
            SignPageView signPage = container.Resolve<SignPageView>();
            SignPageViewModel pageContext = signPage.DataContext as SignPageViewModel;
            pageContext.SignPanelSource = new BitmapImage(new Uri(signPanelPath, UriKind.Absolute));
            VectorRichTabItem richTabItems = new()
            {
                Content = signPage,
                Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                Header = "acacia"
            };
            SignList.Add(richTabItems);
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