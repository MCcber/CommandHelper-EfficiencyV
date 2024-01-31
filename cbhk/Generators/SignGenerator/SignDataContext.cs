using cbhk.CustomControls;
using cbhk.GenerateResultDisplayer;
using cbhk.Generators.SignGenerator.Components;
using cbhk.WindowDictionaries;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace cbhk.Generators.SignGenerator
{
    public partial class SignDataContext : ObservableObject
    {
        #region 字段
        /// <summary>
        /// 主页
        /// </summary>
        public Window home = null;

        /// <summary>
        /// 告示牌数据源
        /// </summary>
        public ObservableCollection<RichTabItems> Signs { get; set; } = [
            new RichTabItems()
            {
                Header = "acacia",
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
        ];

        /// <summary>
        /// 已选中的告示牌
        /// </summary>
        public RichTabItems SelectedItem { get; set; }

        ImageSource icon = new BitmapImage();

        string iconPath = AppDomain.CurrentDomain.BaseDirectory + @"resources\configs\Sign\images\icon.png";

        private bool showResult;

        public bool ShowResult
        {
            get => showResult;
            set => SetProperty(ref showResult, value);
        }

        public ObservableCollection<TextComboBoxItem> VersionSource { get; set; } = [
            new TextComboBoxItem() { Text = "1.20.0" },
            new TextComboBoxItem() { Text = "1.19.4" },
            new TextComboBoxItem() { Text = "1.19.3" },
            new TextComboBoxItem() { Text = "1.17.0" },
            new TextComboBoxItem() { Text = "1.16.0" },
            new TextComboBoxItem() { Text = "1.14.0" },
            new TextComboBoxItem() { Text = "1.13.0" }
        ];


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
            if (File.Exists(iconPath))
                icon = new BitmapImage(new Uri(iconPath, UriKind.Absolute));
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
        /// 生成所有告示牌
        /// </summary>
        private async Task Run()
        {
            Displayer displayer = Displayer.GetContentDisplayer();
            StringBuilder result = new();
            await Task.Run(async () =>
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var item in Signs)
                    {
                        SignPage signPage = item.Content as SignPage;
                        SignPageDataContext signPageDataContext = signPage.DataContext as SignPageDataContext;
                        signPageDataContext.Run();
                        if (!ShowResult)
                            result.Append(signPageDataContext.Result + "\r\n");
                        displayer.GeneratorResult(signPageDataContext.Result, "告示牌", iconPath);
                    }
                });
            });
            if (ShowResult)
                displayer.Show();
            else
                Clipboard.SetText(result.ToString());
        }

        [RelayCommand]
        /// <summary>
        /// 添加告示牌
        /// </summary>
        /// <param name="element"></param>
        private void AddSign()
        {
            string signPanelPath = AppDomain.CurrentDomain.BaseDirectory + @"ImageSet\acaciaSignPanel.png";
            SignPage signPage = new() { FontWeight = FontWeights.Normal };
            SignPageDataContext pageContext = signPage.DataContext as SignPageDataContext;
            pageContext.SignPanelSource = new BitmapImage(new Uri(signPanelPath, UriKind.Absolute));
            RichTabItems richTabItems = new()
            {
                Content = signPage,
                Style = Application.Current.Resources["RichTabItemStyle"] as Style,
                Header = "acacia",
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

        [RelayCommand]
        /// <summary>
        /// 清空告示牌
        /// </summary>
        private void ClearSigns() => Signs.Clear();

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