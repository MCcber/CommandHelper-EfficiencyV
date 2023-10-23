using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using cbhk.GeneralTools.MessageTip;
using System.Data;
using cbhk.GeneralTools;
using System.Data.SQLite;
using System.Data.Common;

namespace cbhk
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
            set
            {
                SetProperty(ref saveUserPassword, value);
                if (SaveUserPassword)
                    SaveUserAccount = true;
            }
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

        //计时器
        DispatcherTimer SignInTimer = new()
        {
            Interval = new TimeSpan(2000),
            IsEnabled = false
        };

        /// <summary>
        /// 登录功能
        /// </summary>
        public RelayCommand SignIn { get; set; }

        /// <summary>
        /// 忘记密码
        /// </summary>
        public RelayCommand ForgotPassword { get; set; }

        /// <summary>
        /// 最小化窗体
        /// </summary>
        public RelayCommand<Window> MinimizeWindow { get; set; }

        /// <summary>
        /// 关闭窗体
        /// </summary>
        public RelayCommand<Window> CloseWindow { get; set; }

        /// <summary>
        /// 存储用户信息
        /// </summary>
        Dictionary<string, string> userInfomation = [];

        public SignInDataContext()
        {
            #region 链接命令
            SignIn = new(SignInCommand);
            ForgotPassword = new(ForgotPasswordCommand);
            MinimizeWindow = new RelayCommand<Window>(MinimizeWindowCommand);
            CloseWindow = new RelayCommand<Window>(CloseWindowCommand);
            #endregion
            ReadUserData();
        }

        /// <summary>
        /// 最小化窗体
        /// </summary>
        /// <param name="window"></param>
        private void MinimizeWindowCommand(Window window) => window.WindowState = WindowState.Minimized;

        /// <summary>
        /// 关闭窗体
        /// </summary>
        /// <param name="window"></param>
        private void CloseWindowCommand(Window window) => window.Close();

        #region 用户服务
        /// <summary>
        /// 读取用户数据
        /// </summary>
        private async void ReadUserData()
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Minecraft.db"))
            {
                DataCommunicator dataCommunicator = DataCommunicator.GetDataCommunicator();
                DataTable dataTable = await dataCommunicator.GetData("SELECT * FROM UserData");
                if (dataTable != null && dataTable.Rows.Count > 0)
                {
                    if (dataTable.Rows[0]["account"] is string account)
                        UserAccount = account;
                    if (dataTable.Rows[0]["password"] is string password)
                        UserPassword = password;
                    SaveUserAccount = UserAccount.Trim() != "";
                    SaveUserPassword = UserPassword.Trim() != "";
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

            #region 自动登录
            SignInTimer.Tick += ThreadTimerCallback;
            SignInTimer.IsEnabled = SaveUserPassword;
            IsOpenSignIn = !SaveUserPassword;
            if (Environment.OSVersion.Version.Major < 10)
                Message.PushMessage("检测到系统为win10以下，如果系统版本过旧，可能无法登录成功", MessageBoxImage.Error);
            #endregion

            #region 调试
            //MainWindow CBHK = new(StatsUserInfomation())
            //{
            //    WindowState = WindowState.Normal
            //};
            //CBHK.cbhkTaskbar.Visibility = Visibility.Visible;
            //CBHK.Show();
            //CBHK.Focus();
            //FrontWindow.Close();
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
            if(UserNameString != "")
            userInfomation.Add("UserName", UserNameString);
            if(UserID != "")
            userInfomation.Add("UserID", UserID);
            if(GameID != "")
            userInfomation.Add("McId", GameID);
            userInfomation.Add("Account", UserAccount);
            userInfomation.Add("Password",UserPassword);
            
            return userInfomation;
        }

        /// <summary>
        /// 保存用户信息
        /// </summary>
        /// <returns></returns>
        private JObject SaveUserInfo(JObject result)
        {
            UserNameString = result["data"]["name"].ToString();
            UserID = result["data"]["id"].ToString();
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
            if (UserAccount.Trim() == "")
            {
                Message.PushMessage("账号不存在",MessageBoxImage.Error);
                UserAccount = "";
                IsOpenSignIn = true;
                return;
            }
            if (UserPassword.Trim() == "")
            {
                Message.PushMessage("密码不能为空", MessageBoxImage.Error);
                UserPassword = "";
                IsOpenSignIn = true;
                return;
            }

            //立刻处理Window消息
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate { }));

            #region 保存账号和密码
            if (SaveUserAccount || SaveUserPassword)
            {
                SQLiteConnection connection = new("Data Source=" + AppDomain.CurrentDomain.BaseDirectory + "Minecraft.db");
                await connection.OpenAsync();
                SQLiteParameter account = new("@account", UserAccount);
                SQLiteParameter password = new("@password", UserPassword);
                DataCommunicator dataCommunicator = DataCommunicator.GetDataCommunicator();

                SQLiteCommand HaveAccountCmd = new("SELECT * FROM UserData Where account = @account", connection);
                HaveAccountCmd.Parameters.Add(account);
                SQLiteCommand UpdateAccountCmd = new("Update UserData Set account = @account", connection);
                UpdateAccountCmd.Parameters.Add(account);
                SQLiteCommand InsertAccountCmd = new("Insert Into UserData (account) Values(@account);", connection);
                InsertAccountCmd.Parameters.Add(account);
                SQLiteCommand UpdatePasswordCmd = new("Update UserData Set password = @password Where account = @account", connection);
                UpdatePasswordCmd.Parameters.Add(password);
                UpdatePasswordCmd.Parameters.Add(account);

                DbDataReader dbDataReader = await HaveAccountCmd.ExecuteReaderAsync();
                DataTable dataTable = new();
                dataTable.Load(dbDataReader);

                if (dataTable.Rows.Count > 0)
                    if (SaveUserAccount)
                        await UpdateAccountCmd.ExecuteNonQueryAsync();
                    else
                    if (SaveUserAccount && dataTable.Rows.Count == 0)
                        await InsertAccountCmd.ExecuteNonQueryAsync();

                if (SaveUserPassword && dataTable.Rows.Count > 0)
                    await UpdatePasswordCmd.ExecuteNonQueryAsync();
                await connection.CloseAsync();
            }
            #endregion

            #region 进行登录
            JObject result = [];
            try
            {
                string account = Regex.Match(uriencode(Regex.Match(UserAccount, "(.*)").ToString()),@"(.*)").ToString();
                string pwd = Regex.Match(uriencode(Regex.Match(UserPassword, "(.*)").ToString()), @"(.*)").ToString();
                result = JsonConvert.DeserializeObject(GeneralTools.SignIn.GetDataByPost(account, pwd)) as JObject;
            }
            catch(Exception e)
            {
                Message.PushMessage(e.Message, MessageBoxImage.Error);
            }
            if (result["code"].ToString() == "200")
            {
                if (result["data"] == null)
                    MessageBox.Show("密码错误");
                else
                {
                    if (result.SelectToken("data.avatar") is JObject avatar && result.SelectToken("data.avatar").ToString().Contains('?'))
                        await Task.Run(async () => { await GeneralTools.SignIn.DownLoadUserImage(avatar.ToString(), AppDomain.CurrentDomain.BaseDirectory + "resources\\userHead.png"); });
                    userInfomation.Add("description", result.SelectToken("data.intro").ToString());
                    SaveUserInfo(result);
                    MainWindow CBHK = new(StatsUserInfomation());

                    if(result.SelectToken("data.bg_bar") is JToken background)
                    await Task.Run(async () =>
                    {
                        await GeneralTools.SignIn.DownLoadUserImage(background.ToString(), AppDomain.CurrentDomain.BaseDirectory + "resources\\userBackground.png");
                    });
                    FrontWindow.Close();

                    #region 显示管家主窗体
                    CBHK.cbhkTaskbar.Visibility = Visibility.Visible;
                    CBHK.WindowState = WindowState.Normal;
                    CBHK.Show();
                    CBHK.Focus();
                    #endregion
                }
            }
            else
                Message.PushMessage(result["message"].ToString(),MessageBoxImage.Error);
            #endregion
            IsOpenSignIn = true;
        }

        /// <summary>
        /// 忘记密码
        /// </summary>
        private void ForgotPasswordCommand()
        {
            Process.Start(@"explorer.exe", "https://mc.metamo.cn/u/login/");
        }
        #endregion

        /// <summary>
        /// 启动登录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UserAccountBoxKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && IsOpenSignIn)
                SignInCommand();
        }

        /// <summary>
        /// 启动登录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UserPasswordBoxKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && IsOpenSignIn)
                SignInCommand();
        }
    }
}
