using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using cbhk_environment.GeneralTools.Information;

namespace cbhk_signin
{
    public class SignInDataContext: ObservableObject
    {
        /// <summary>
        /// 是否放行
        /// </summary>
        private bool through;
        /// <summary>
        /// 保存接口信息中的用户名
        /// </summary>
        private string UserNameString = "";
        /// <summary>
        /// 保存用户ID
        /// </summary>
        private string UserID = "";
        /// <summary>
        /// 保存用户的在mc中的id
        /// </summary>
        private string GameID = "";

        #region 用户账密
        private string userPassword = "";
        public string UserPassword
        {
            get => userPassword; 
            set => SetProperty(ref userPassword, value);
        }

        private string userAccount = "";
        public string UserAccount
        {
            get => userAccount; 
            set => SetProperty(ref userAccount, value);
        }
        #endregion

        #region 记住账密
        private bool saveUserPassword = false;
        public bool SaveUserPassword
        {
            get => saveUserPassword; 
            set => SetProperty(ref saveUserPassword, value);
        }

        private bool saveUserAccount = false;
        public bool SaveUserAccount
        {
            get => saveUserAccount;
            set => SetProperty(ref saveUserAccount, value);
        }
        #endregion

        #region 启用登录按钮
        private bool isOpenSignIn = true;
        public bool IsOpenSignIn
        {
            get => isOpenSignIn;
            set => SetProperty(ref isOpenSignIn, value);
        }
        #endregion

        //载入前台窗体引用
        Window FrontWindow = null;
        /// <summary>
        /// 托盘图标
        /// </summary>
        TaskbarIcon taskbarIcon = null;

        //计时器
        DispatcherTimer SignInTimer = new DispatcherTimer()
        {
            Interval = new TimeSpan(2000),
            IsEnabled = false
        };

        //登录功能
        public RelayCommand SignIn { get; set; }

        public SignInDataContext()
        {
            //登录功能
            SignIn = new RelayCommand(SignInCommand);
            //初始化用户设置
            ReadUserData();

        }

        #region 用户服务
        /// <summary>
        /// 读取用户数据
        /// </summary>
        private void ReadUserData()
        {
            Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "resources\\signin_configs");
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "resources\\signin_configs\\user_info.ini"))
            {
                string[] user_info = File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + "resources\\signin_configs\\user_info.ini");
                if (user_info != null)
                {
                    UserAccount = user_info.Length >= 1 ? Regex.Match(user_info[0], @".*").ToString() : "";
                    UserPassword = user_info.Length == 2 ? Regex.Match(user_info[1], @".*").ToString() : "";
                    SaveUserAccount = UserAccount != "";
                    SaveUserPassword = UserPassword != "";
                }
            }
        }

        /// <summary>
        /// 获取前台引用，订阅自动登录事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SignInWindowLoaded(object sender, RoutedEventArgs e)
        {
            FrontWindow = sender as Window;
            taskbarIcon = FrontWindow.Resources["cbhkTaskbar"] as TaskbarIcon;
            taskbarIcon.Visibility = Visibility.Collapsed;
            //自动登录
            //SignInTimer.Tick += ThreadTimerCallback;
            //SignInTimer.IsEnabled = SaveUserPassword;
            //IsOpenSignIn = !SaveUserPassword;
            //if (Environment.OSVersion.Version.Major < 10)
            //{
            //    MessageDisplayer messageBox = new();
            //    messageDisplayerDataContext context = messageBox.DataContext as messageDisplayerDataContext;
            //    context.DisplayInfomation = "检测到系统为win10以下，如果系统版本过旧，可能无法登录成功";
            //    messageBox.ShowDialog();
            //}

            #region 调试
            FrontWindow.ShowInTaskbar = false;
            FrontWindow.WindowState = WindowState.Minimized;
            FrontWindow.Opacity = 0;
            taskbarIcon.Visibility = Visibility.Visible;
            cbhk_environment.MainWindow CBHK = new(StatsUserInfomation(), taskbarIcon);
            CBHK.Show();
            CBHK.WindowState = WindowState.Normal;
            CBHK.Topmost = true;
            CBHK.Show();
            CBHK.Focus();
            CBHK.Topmost = false;
            #endregion
        }

        /// <summary>
        /// 自动登录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ThreadTimerCallback(object sender, EventArgs e)
        {
            SignInCommand();
            SignInTimer.IsEnabled = false;
        }

        /// <summary>
        /// 统计用户信息
        /// </summary>
        private Dictionary<string, string> StatsUserInfomation()
        {
            Dictionary<string, string> user_information = new Dictionary<string, string> { };
            if(UserNameString != "")
            user_information.Add("user_name", UserNameString);
            if(UserID != "")
            user_information.Add("UserID", UserID);
            if(GameID != "")
            user_information.Add("mc_id", GameID);
            user_information.Add("Account", UserAccount);
            user_information.Add("Password",UserPassword);
            return user_information;
        }

        /// <summary>
        /// 保存用户信息
        /// </summary>
        /// <returns></returns>
        private JObject SaveUserInfo(JObject result)
        {
            UserNameString = result["data"]["name"].ToString();
            UserID = result["data"]["id"].ToString();
            //GameID = result["data"]["mc_id"].ToString();
            return result;
        }

        /// <summary>
        /// 替换正则无法识别的特殊字符
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private string uriencode(string url)
        {
            return url.Replace("%", "%25").Replace(" ", "%20").Replace("\"", "%22").Replace("#", "%23").Replace("&", "%26").Replace("(", "%28").Replace(")", "%29").Replace("+", "%2B").Replace(",", "%2C").Replace("/", "%2F").Replace(":", "%3A").Replace(";", "%3B").Replace("<", "%3C").Replace("=", "%3D").Replace(">", "%3E").Replace("?", "%3F").Replace("@", "%40").Replace("\\", "%5C").Replace("|", "%7C");
        }

        /// <summary>
        /// 登录
        /// </summary>
        private async void SignInCommand()
        {
            IsOpenSignIn = false;
            Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "resources\\signin_configs");
            if (UserAccount.Trim() == "")
            {
                MessageBox.Show("账号不存在");
                UserAccount = "";
                return;
            }
            if (UserPassword.Trim() == "")
            {
                MessageBox.Show("密码不能为空");
                UserPassword = "";
                return;
            }

            //立刻处理Window消息
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate { }));

            #region 保存账号和密码
            if (SaveUserAccount || SaveUserPassword)
            {
                string user_info = "";
                if (SaveUserAccount)
                    user_info += UserAccount + "\r\n";
                if (SaveUserPassword)
                    user_info += Regex.Escape(UserPassword) + "\r\n";
                byte[] user_pwd_bytes = Encoding.UTF8.GetBytes(user_info);
                if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "resources\\signin_configs\\user_info.ini"))
                    File.CreateText(AppDomain.CurrentDomain.BaseDirectory + "resources\\signin_configs\\user_info.ini").Close();

                using FileStream name_pwd_stream = new(AppDomain.CurrentDomain.BaseDirectory + "resources\\signin_configs\\user_info.ini", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                name_pwd_stream.Write(user_pwd_bytes, 0, user_pwd_bytes.Length);
            }
            #endregion

            #region 进行登录
            JObject result = new();
            try
            {
                string account = Regex.Match(uriencode(Regex.Match(UserAccount, "(.*)").ToString()),@"(.*)").ToString();
                string pwd = Regex.Match(uriencode(Regex.Match(UserPassword, "(.*)").ToString()), @"(.*)").ToString();
                result = JsonConvert.DeserializeObject(resources.Tools.SignIn.GetDataByPost(account, pwd)) as JObject;
            }
            catch
            {
            }
            if (result["code"].ToString() == "200")
            {
                if (result["data"] == null)
                    MessageBox.Show("密码错误");
                else
                {
                    if (result["data"]["avatar"].ToString() != null && result["data"]["avatar"].ToString().Contains("?") && !File.Exists(AppDomain.CurrentDomain.BaseDirectory + "resources\\user_head.png"))
                        await Task.Run(() => { resources.Tools.SignIn.DownLoadUserHead(result["data"]["avatar"].ToString(), AppDomain.CurrentDomain.BaseDirectory + "resources\\user_head.png"); });

                    FrontWindow.ShowInTaskbar = false;
                    FrontWindow.WindowState = WindowState.Minimized;
                    FrontWindow.Opacity = 0;
                    FrontWindow.Visibility = Visibility.Collapsed;
                    FrontWindow.WindowState = WindowState.Minimized;
                    SaveUserInfo(result);
                    cbhk_environment.MainWindow CBHK = new(StatsUserInfomation(),taskbarIcon);

                    #region 显示管家主窗体
                    taskbarIcon.Visibility = Visibility.Visible;
                    CBHK.ShowInTaskbar = true;
                    CBHK.Opacity = 1.0;
                    CBHK.WindowState = WindowState.Normal;
                    CBHK.Topmost = true;
                    CBHK.Show();
                    CBHK.Focus();
                    CBHK.Topmost = false;
                    #endregion
                }
            }
            else
                MessageBox.Show(result["message"].ToString());
            #endregion
            IsOpenSignIn = true;
        }

        /// <summary>
        /// 忘记密码
        /// </summary>
        public void ForgotPasswordCommand(object sender, MouseButtonEventArgs e)
        {
            Process.Start("explorer.exe", "https://mc.metamo.cn/u/login/");
        }
        #endregion

        /// <summary>
        /// 启动登录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UserAccountBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && IsOpenSignIn)
                SignInCommand();
        }

        /// <summary>
        /// 启动登录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UserPasswordBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && IsOpenSignIn)
                SignInCommand();
        }
    }
}
