using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.IO.Compression;
using System.Reflection;

namespace CBHK_Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public class UpdateInfoForcast
    {
        public string tag_name { get; set; }
        public string body { get; set; }
        public string created_at { get; set; }
    }
    public partial class MainWindow : Window
    {
        public static UpdateInfoForcast updateInfo;
        public static string nowVersion = "Unknown";
        public static bool isSilent = false;
        private long _totalBytes;
        private string _currentFile = "cbhk.zip"; // 下载文件名
        private readonly HttpClient _httpClient = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length >= 1)
            {
                if (args[0] == "--silent")
                {
                    isSilent = true;
                }
            }
            try
            {
                string str = File.ReadAllText(@"version.txt");
                TipLabel.Content = "检查更新。当前版本：" + str;
                nowVersion = str;
            }
            catch (FileNotFoundException e)
            {
                TipLabel.Content = "下载最新版 CBHK";
                CheckButton.Content = "下载";

            }
            catch (Exception e)
            {
                TipLabel.Content = "检查更新失败。";
                CheckButton.Visibility = Visibility.Hidden;
                CancelButton.Visibility = Visibility.Visible;
            }
            if (isSilent)
            {
                this.Visibility = Visibility.Hidden;
                CheckButton_Click(null, null);
            }

        }
        public void UnzipFile(string zipFilePath, string path)
        {

            // 获取要解压到的文件夹路径
            string destinationDirectory = path;
            Dispatcher.Invoke(() =>
            {
                TipLabel.Content = "正在解压文件...";
                progressBar.IsIndeterminate = true;
            });

            // 解压 ZIP 文件
            ZipFile.ExtractToDirectory(zipFilePath, destinationDirectory);

        }
        private async void CheckButton_Click(object sender, RoutedEventArgs e)
        {

            CheckButton.Visibility = Visibility.Hidden;
            string URL = "https://gitee.com/api/v5/repos/honghuangtaichu/minecraft-correlation/releases/latest";
            try
            {
                HttpResponseMessage result = _httpClient.GetAsync(URL).Result;
                var data = result.Content.ReadAsStringAsync().Result;

                updateInfo = JsonSerializer.Deserialize<UpdateInfoForcast>(data);
                var win = new UpdateInfoWindow();
                if (updateInfo.tag_name == nowVersion)
                {
                    if (isSilent) { Environment.Exit(0); return; }
                    MessageBox.Show("当前已经是最新版本！无需更新！");
                    Environment.Exit(0);

                }

                win.ShowDialog();
                progressBar.Value = 0;
                progressBar.IsIndeterminate = false;
                TipLabel.Content = "正在下载文件...";
                CancelButton.Visibility = Visibility.Visible;
                await Task.Run(() => StartDownload());

                Environment.Exit(0);

            }
            catch (Exception ex)
            {
                MessageBox.Show("检查更新失败！" + ex.Message);
            }
        }
        private void DownloadComplete()
        {
            string directoryPath = @".\bin";
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true); // 递归删除目录及其内容
            }
            // 创建目录
            Directory.CreateDirectory(directoryPath);
            UnzipFile("cbhk.zip", directoryPath);
            File.WriteAllText(@"version.txt", updateInfo.tag_name);
        }
        private void DownloadFile(string FileUrl)
        {
            try
            {
                var response = _httpClient.GetAsync(FileUrl, HttpCompletionOption.ResponseHeadersRead).Result;

                if (response.IsSuccessStatusCode)
                {
                    var contentLength = response.Content.Headers.ContentLength;
                    if (contentLength.HasValue)
                    {
                        _totalBytes = contentLength.Value;

                        using (var stream = response.Content.ReadAsStreamAsync().Result)
                        using (var fileStream = new FileStream(_currentFile, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            var buffer = new byte[8192];
                            int bytesRead;
                            long totalBytesRead = 0;

                            while ((bytesRead = stream.ReadAsync(buffer, 0, buffer.Length).Result) > 0)
                            {
                                fileStream.WriteAsync(buffer, 0, bytesRead).Wait();
                                totalBytesRead += bytesRead;

                                // 更新进度条
                                UpdateProgressBar(totalBytesRead);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("未能获取文件大小");
                    }
                }
                else if (response.StatusCode == HttpStatusCode.Found) // 302重定向
                {
                    String fileUrl = response.Headers.Location.AbsoluteUri;
                    DownloadFile(fileUrl); // 重新发起下载请求
                }
                else
                {
                    MessageBox.Show("下载失败：" + response.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("下载出现异常：" + ex.Message);
            }
        }
        private void UpdateProgressBar(long bytesRead)
        {
            Dispatcher.Invoke(() =>
            {
                progressBar.Value = (double)bytesRead / _totalBytes * 100;
            });
        }
        private async Task StartDownload()
        {
            var FileUrl = "https://gitee.com/honghuangtaichu/minecraft-correlation/releases/download/" + updateInfo.tag_name + "/CommandHelper-EfficiencyV.zip";

            DownloadFile(FileUrl);
            DownloadComplete();
        }


        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}