using cbhk_environment.CustomControls;
using cbhk_environment.GeneralTools;
using CommunityToolkit.Mvvm.Input;
using FaviconFetcher;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace cbhk_environment.SettingForm
{
    /// <summary>
    /// SetRoatationChart.xaml 的交互逻辑
    /// </summary>
    public partial class SetRoatationChart
    {
        public RelayCommand AddItem { get; set; }
        public RelayCommand ClearItems { get; set; }

        public RelayCommand<FrameworkElement> DeleteItem { get; set; }

        private DataCommunicator dataCommunicator = DataCommunicator.GetDataCommunicator();

        public ObservableCollection<RotationChartSetItem> RotationCharts { get; set; } = new();

        DataTable chartTable = null;

        public SetRoatationChart()
        {
            InitializeComponent();
            DataContext = this;
            AddItem = new(AddRotationChartItem);
            ClearItems = new(ClearRotationChartItem);
            DeleteItem = new RelayCommand<FrameworkElement>(DeleteClick);
        }

        /// <summary>
        /// 载入轮播图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SetRoatationChart_Loaded(object sender, RoutedEventArgs e)
        {
            //载入进程锁
            object tagItemsLock = new();
            BindingOperations.EnableCollectionSynchronization(RotationCharts, tagItemsLock);
            await Task.Run(async () =>
            {
                await Dispatcher.InvokeAsync(async() =>
                {
                    chartTable = await dataCommunicator.GetData("SELECT * FROM RotationChart");
                    if (chartTable == null) return;
                    string currentPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
                    for (int i = 0; i < chartTable.Rows.Count; i++)
                    {
                        RotationChartSetItem rotationChartSetItem = new();
                        RotationCharts.Add(rotationChartSetItem);
                        string iconPath = currentPath + chartTable.Rows[i]["id"].ToString() + "Icon.png";
                        string previewPath = currentPath + chartTable.Rows[i]["id"].ToString() + ".png";
                        if (File.Exists(iconPath))
                            rotationChartSetItem.ItemIcon = new BitmapImage(new Uri(iconPath, UriKind.Absolute));
                        if (File.Exists(previewPath))
                            rotationChartSetItem.PreviewImage = new BitmapImage(new Uri(previewPath, UriKind.Absolute));
                        if (chartTable.Rows[i]["url"] is string url)
                            rotationChartSetItem.ItemUrl = url;
                        if (chartTable.Rows[i]["id"] is string id)
                            rotationChartSetItem.ItemId = id;
                        if (chartTable.Rows[i]["description"] is string description)
                            rotationChartSetItem.ItemDescription = description;
                        rotationChartSetItem.DeleteUrl = DeleteItem;
                    }
                });
            });
        }

        /// <summary>
        /// 把数据写入本地
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RotationChart_Closed(object sender, EventArgs e)
        {
            string imageBasePath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
            foreach (RotationChartSetItem item in RotationCharts)
            {
                DataTable result = await dataCommunicator.GetData("SELECT * FROM RotationChart WHERE id = '"+item.ItemId+"'");
                if (result.Rows.Count > 0)
                    await dataCommunicator.ExecuteNonQuery("UPDATE RotationChart SET url='" + item.ItemUrl + "',description='" + item.ItemDescription + "' WHERE id='"+item.ItemId+"'");
                else
                {
                    await dataCommunicator.ExecuteNonQuery("INSERT INTO RotationChart VALUES('"+item.ItemId+"','"+item.ItemUrl+"','"+item.ItemDescription+"')");
                    SaveBitmapImage(item.ItemIcon, imageBasePath + item.ItemId+"Icon.png");
                    SaveBitmapImage(item.PreviewImage, imageBasePath + item.ItemId + ".png");
                }
            }
        }

        /// <summary>
        /// 保存BitmapImage到文件
        /// </summary>
        /// <param name="bitmapImage"></param>
        /// <param name="filePath"></param>
        private void SaveBitmapImage(BitmapImage bitmapImage,string filePath)
        {
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
            using var fileStream = new FileStream(filePath, FileMode.Create);
            encoder.Save(fileStream);
        }

        /// <summary>
        /// 清空轮播图
        /// </summary>
        private void ClearRotationChartItem() => RotationCharts.Clear();

        /// <summary>
        /// 轮播图成员网址更新事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void RotationChartItemUrl_TextChanged(object sender, TextChangedEventArgs e)
        {
            //try
            //{
                TextBox currentBox = sender as TextBox;
                currentBox.IsReadOnly = true;
                Grid parent = currentBox.Parent as Grid;
                Image icon = null;
                Image preview = null;
                TextBox description = null;
                foreach (FrameworkElement item in parent.Children)
                {
                    if (item.Uid == "icon")
                        icon = item as Image;
                    if (item.Uid == "preview")
                        preview = item as Image;
                    if (item.Uid == "description")
                        description = item as TextBox;
                }
                string target = (currentBox.DataContext as RotationChartSetItem).ItemUrl;
                target ??= currentBox.Text;
                if (target == null) return;
                #region 爬取指定网站的图标
                await Task.Run(async () =>
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        Uri targetUrl = new(target);
                        Fetcher fetcher = new();
                        var result = fetcher.FetchClosest(targetUrl, new IconSize(50, 50));
                        BitmapSource bitmapSource = BitmapSource.Create(
                              result.Size.Width,
                              result.Size.Height,
                              96,
                              96,
                              PixelFormats.Bgra32,
                              null,
                              result.Bytes,
                              result.Size.Width * 4
                            );
                        icon.Source = bitmapSource;
                    });
                });
                #endregion
                #region 爬取指定网页的截图
                // 后续可以根据bitmap保存图片
                string imageName = "";
                await Task.Run(async () =>
                {
                    EdgeOptions edgeOptions = new();
                    edgeOptions.AddArgument("headless");
                    edgeOptions.AddArgument("disable-gpu");
                    var driverService = EdgeDriverService.CreateDefaultService(AppDomain.CurrentDomain.BaseDirectory);
                    driverService.HideCommandPromptWindow = true;
                    // EdgeDriver实例
                    EdgeDriver driver = new(driverService, edgeOptions);
                    driver.Manage().Window.Size = new System.Drawing.Size(int.Parse(SystemParameters.PrimaryScreenWidth.ToString()), int.Parse(SystemParameters.WorkArea.Height.ToString()));
                    driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
                    // 导航到指定网址
                    driver.Navigate().GoToUrl(target);
                    imageName = Regex.Match(driver.Title, @"[\u4e00-\u9fa5a-z]+").ToString();
                    // 获取屏幕截图
                    Screenshot screenshot = ((ITakesScreenshot)driver).GetScreenshot();
                    // 将屏幕截图保存为PNG文件
                    screenshot.SaveAsFile(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + imageName + ".png", ScreenshotImageFormat.Png);
                    //关闭浏览器窗口并释放资源
                    driver.Quit();

                    await Dispatcher.InvokeAsync(() =>
                    {
                        description.Text = imageName;
                        preview.Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\" + imageName + ".png"));
                        currentBox.IsReadOnly = false;
                    });
                });
                #endregion
            //}
            //catch (Exception error)
            //{
            //    MessageBox.Show(error.Message+"\r\n"+error.StackTrace);
            //}
        }

        private void AddRotationChartItem() => RotationCharts.Add(new RotationChartSetItem()
        {
            ItemId = Guid.NewGuid().ToString(),
            DeleteUrl = DeleteItem
        });

        /// <summary>
        /// 切换预览图可见性
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SwitchPreviewVisibility_Click(object sender, RoutedEventArgs e)
        {
            TextToggleButtons button = sender as TextToggleButtons;
            Grid parent = button.Parent as Grid;
            Image preview = parent.FindChild<Image>("preview");
            preview.Visibility = button.IsChecked.Value ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 清除所有成员
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void DeleteClick(FrameworkElement element)
        {
            Button button = element as Button;
            RotationCharts.Remove(button.DataContext as RotationChartSetItem);
        }

        //private void SetUrlCommand(object obj)
        //{
        //    RotationChartSetItem rotationChartSetItem = obj as RotationChartSetItem;
        //    System.Windows.Forms.OpenFileDialog betterFolderBrowser = new()
        //    {
        //        Multiselect = false,
        //        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
        //        Filter = "文本文件(*.txt)|*.txt",
        //        Title = "请选择一个包含链接的文本文件"
        //    };
        //    if (betterFolderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        //    {
        //        if (File.Exists(betterFolderBrowser.FileName))
        //        {
        //            if (betterFolderBrowser.FileName != AppDomain.CurrentDomain.BaseDirectory + "resources\\link_data\\" + Path.GetFileName(betterFolderBrowser.FileName))
        //                File.Copy(betterFolderBrowser.FileName, AppDomain.CurrentDomain.BaseDirectory + "resources\\link_data\\" + Path.GetFileName(betterFolderBrowser.FileName));
        //            string urlString = File.ReadAllText(betterFolderBrowser.FileName);
        //            rotationChartSetItem.ItemUrl = urlString;
        //            WebClient client = new WebClient();
        //            client.DownloadFile(urlString + "/favicon.ico", AppDomain.CurrentDomain.BaseDirectory + "resources\\link_data\\" + Path.GetFileNameWithoutExtension(betterFolderBrowser.FileName) + "Icon.png");

        //            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "resources\\link_data\\" + Path.GetFileNameWithoutExtension(betterFolderBrowser.FileName) + "Icon.png"))
        //            {
        //                rotationChartSetItem.ItemIcon = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "resources\\link_data\\" + Path.GetFileNameWithoutExtension(betterFolderBrowser.FileName) + "Icon.png", UriKind.Absolute));
        //            }
        //        }
        //    }
        //}

        public void RotationChartItemUrl_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            textBox.MouseEnter -= RotationChartItemUrl_MouseEnter;
            textBox.TextChanged += RotationChartItemUrl_TextChanged;
        }
    }

    public class RotationChartSetItem
    {
        public string ItemId { get; set; }
        public BitmapImage ItemIcon { get; set; }

        public BitmapImage PreviewImage { get; set; }

        public string ItemUrl { get; set; }

        public string ItemDescription { get; set; }

        public RelayCommand<FrameworkElement> DeleteUrl { get; set; }
    }
}
