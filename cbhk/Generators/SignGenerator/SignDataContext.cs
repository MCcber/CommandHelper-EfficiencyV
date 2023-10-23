using cbhk.CustomControls;
using cbhk.Generators.SignGenerator.Components;
using cbhk.WindowDictionaries;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace cbhk.Generators.SignGenerator
{
    public class SignDataContext:ObservableObject
    {
        #region 字段
        /// <summary>
        /// 主页
        /// </summary>
        public Window home = null;

        /// <summary>
        /// 告示牌数据源
        /// </summary>
        public ObservableCollection<RichTabItems> Signs { get; set; } = new() {
            new RichTabItems()
            {
                Uid = "oak",
                Header = "oak",
                IsContentSaved = true,
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
            }
        };

        /// <summary>
        /// 已选中的告示牌
        /// </summary>
        public RichTabItems SelectedItem { get; set; }
        #endregion

        #region 运行、返回等命令
        public RelayCommand Run { get; set; }
        public RelayCommand<CommonWindow> Return { get; set; }

        /// <summary>
        /// 添加告示牌
        /// </summary>
        public RelayCommand<FrameworkElement> AddSign { get; set; }

        /// <summary>
        /// 清空告示牌
        /// </summary>
        public RelayCommand ClearSigns { get; set; }

        /// <summary>
        /// 清除指定告示牌
        /// </summary>
        public RelayCommand<FrameworkElement> ClearSign { get; set; }
        #endregion

        public SignDataContext()
        {
            Task.Run(async () =>
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Signs[0].Content = new SignPage() { FontWeight = FontWeights.Normal };
                });
            });
            AddSign = new RelayCommand<FrameworkElement>(AddSignCommand);
            ClearSigns = new(ClearSignsCommand);
            ClearSign = new RelayCommand<FrameworkElement>(ClearSignCommand);
            Run = new(runCommand);
            Return = new RelayCommand<CommonWindow>(ReturnCommand);
        }

        /// <summary>
        /// 返回主页
        /// </summary>
        /// <param name="win"></param>
        private void ReturnCommand(CommonWindow win)
        {
            home.WindowState = WindowState.Normal;
            home.Show();
            home.ShowInTaskbar = true;
            home.Focus();
            win.Close();
        }

        /// <summary>
        /// 生成所有告示牌
        /// </summary>
        private void runCommand()
        {
        }

        /// <summary>
        /// 添加告示牌
        /// </summary>
        /// <param name="element"></param>
        private void AddSignCommand(FrameworkElement element)
        {
            MenuItem menuItem = element as MenuItem;
            string uid = menuItem.Uid;
            int index = uid.IndexOf('_');
            if (index != -1)
            {
                string target = uid[(index + 1)..(index + 2)];
                _ = uid.Replace('_' + target, target.ToUpper());
            }

            string signPanelPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + uid + "SignPanel.png";
            SignPage signPage = new() { FontWeight = FontWeights.Normal };
            SignPageDataContext pageContext = signPage.DataContext as SignPageDataContext;
            pageContext.SignPanelSource = new BitmapImage(new Uri(signPanelPath, UriKind.Absolute));
            RichTabItems richTabItems = new()
            {
                Uid = menuItem.Uid,
                Content = signPage,
                Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                Header = uid,
                //HeaderImage = new BitmapImage(new Uri(signPanelPath, UriKind.Absolute)),
                IsContentSaved = true,
                BorderThickness = new(4, 3, 4, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
                SelectedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC6B23")),
                LeftBorderTexture = Application.Current.Resources["TabItemLeft"] as ImageBrush,
                RightBorderTexture = Application.Current.Resources["TabItemRight"] as ImageBrush,
                TopBorderTexture = Application.Current.Resources["TabItemTop"] as ImageBrush,
                SelectedLeftBorderTexture = Application.Current.Resources["SelectedTabItemLeft"] as ImageBrush,
                SelectedRightBorderTexture = Application.Current.Resources["SelectedTabItemRight"] as ImageBrush,
                SelectedTopBorderTexture = Application.Current.Resources["SelectedTabItemTop"] as ImageBrush
            };
            Signs.Add(richTabItems);
        }

        /// <summary>
        /// 清除指定类型的告示牌
        /// </summary>
        /// <param name="element"></param>
        private void ClearSignCommand(FrameworkElement element)
        {
            if(element is MenuItem menuItem)
            {
                string uid = menuItem.Uid;
                for (int i = 0; i < Signs.Count; i++)
                {
                    if (Signs[i].Uid == uid)
                    {
                        Signs.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        /// <summary>
        /// 清空告示牌
        /// </summary>
        private void ClearSignsCommand() => Signs.Clear();


        /// <summary>
        /// 添加告示牌操作图标
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void AddSignMenuIconLoaded(object sender,RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            string path = item.Parent is not MenuItem ?"oak_sign.png":item.Uid + "_sign.png";
            BitmapImage bitmapImage = new(new Uri(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\"+ path, UriKind.Absolute));
            Image image = new()
            {
                Source = bitmapImage
            };
            item.Icon = image;
        }
    }
}
