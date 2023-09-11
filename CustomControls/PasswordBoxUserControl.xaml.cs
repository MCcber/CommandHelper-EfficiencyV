using System.Windows;
using System.Windows.Controls;

namespace cbhk_signin.CustomControls
{
    /// <summary>
    /// PasswordBoxUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class PasswordBoxUserControl : UserControl
    {
        public PasswordBoxUserControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 控制TextBox显示或者隐藏----TextBox来显示明文
        /// </summary>
        public Visibility TbVisibility
        {
            get { return (Visibility)GetValue(TbVisibilityProperty); }
            set { SetValue(TbVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TbVisibility.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TbVisibilityProperty =
            DependencyProperty.Register("TbVisibility", typeof(Visibility), typeof(PasswordBoxUserControl));

        /// <summary>
        /// 控制PassworBox显示或者隐藏----PasswordBox控件来显密文
        /// </summary>
        public Visibility PwVisibility
        {
            get { return (Visibility)GetValue(PwVisibilityProperty); }
            set { SetValue(PwVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PwVisibility.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PwVisibilityProperty =
            DependencyProperty.Register("PwVisibility", typeof(Visibility), typeof(PasswordBoxUserControl));

        /// <summary>
        /// 和“眼睛”进行绑定
        /// </summary>
        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Check.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register("IsChecked", typeof(bool), typeof(PasswordBoxUserControl), new PropertyMetadata((s, e) =>
            {
                var dp = s as PasswordBoxUserControl;
                if ((bool)e.NewValue)
                {
                    dp.TbVisibility = Visibility.Visible;
                    dp.PwVisibility = Visibility.Collapsed;
                }
                else
                {
                    dp.TbVisibility = Visibility.Collapsed;
                    dp.PwVisibility = Visibility.Visible;
                }
            }));

        /// <summary>
        /// 点击图标“x”,使密码框清空
        /// </summary>
        public bool IsCleared
        {
            get { return (bool)GetValue(IsClearedProperty); }
            set { SetValue(IsClearedProperty, value); }
        }
        public static readonly DependencyProperty IsClearedProperty =
            DependencyProperty.Register("IsCleared", typeof(bool), typeof(PasswordBoxUserControl), new PropertyMetadata((s, e) =>
            {
                var c = s as PasswordBoxUserControl;
                c.PasswordString = "";
            }));

        /// <summary>
        /// 控制显示符号“x”
        /// </summary>
        public Visibility ClearVisibility
        {
            get { return (Visibility)GetValue(ClearVisibilityProperty); }
            set { SetValue(ClearVisibilityProperty, value); }
        }
        public static readonly DependencyProperty ClearVisibilityProperty =
            DependencyProperty.Register("ClearVisibility", typeof(Visibility), typeof(PasswordBoxUserControl), new PropertyMetadata(Visibility.Collapsed));

        /// <summary>
        /// 密码
        /// </summary>
        public string PasswordString
        {
            get { return (string)GetValue(PasswordStringProperty); }
            set { SetValue(PasswordStringProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Password.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PasswordStringProperty =
            DependencyProperty.Register("PasswordString", typeof(string), typeof(PasswordBoxUserControl), new PropertyMetadata((s, e) =>
            {
                var pw = s as PasswordBoxUserControl;
                if (!string.IsNullOrEmpty(pw.PasswordString))  //根据密码框是否有内容来显示符号“x”
                {
                    pw.ClearVisibility = Visibility.Visible;
                }
                else
                {
                    pw.ClearVisibility = Visibility.Collapsed;
                }
            }));
    }

    public static class PasswordBoxHelper
    {
        public static readonly DependencyProperty PasswordProperty = DependencyProperty.RegisterAttached("Password", typeof(string), typeof(PasswordBoxHelper),
                                                                                                          new FrameworkPropertyMetadata(string.Empty, OnPasswordPropertyChanged));
        public static readonly DependencyProperty AttachProperty = DependencyProperty.RegisterAttached("Attach", typeof(bool), typeof(PasswordBoxHelper), new PropertyMetadata(false, OnAttachPropertyChanged));

        private static readonly DependencyProperty IsUpdatingProperty = DependencyProperty.RegisterAttached("IsUpdating", typeof(bool), typeof(PasswordBoxHelper));


        public static void SetAttach(DependencyObject dp, bool value)
        {
            dp.SetValue(AttachProperty, value);
        }

        public static bool GetAttach(DependencyObject dp)
        {
            return (bool)dp.GetValue(AttachProperty);
        }

        public static string GetPassword(DependencyObject dp)
        {
            return (string)dp.GetValue(PasswordProperty);
        }

        public static void SetPassword(DependencyObject dp, string value)
        {
            dp.SetValue(PasswordProperty, value);
        }

        private static bool GetIsUpdating(DependencyObject dp)
        {
            return (bool)dp.GetValue(IsUpdatingProperty);
        }

        private static void SetIsUpdating(DependencyObject dp, bool value)
        {
            dp.SetValue(IsUpdatingProperty, value);
        }

        private static void OnPasswordPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if(sender is PasswordBox)
            {
                PasswordBox passwordBox = sender as PasswordBox;
                passwordBox.PasswordChanged -= PasswordChanged;
                if (!(bool)GetIsUpdating(passwordBox))
                {
                    passwordBox.Password = (string)e.NewValue;
                }
                passwordBox.PasswordChanged += PasswordChanged;
            }
        }

        private static void OnAttachPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            if (passwordBox == null)
            {
                return;
            }
            if ((bool)e.OldValue)
            {
                passwordBox.PasswordChanged -= PasswordChanged;
            }
            if ((bool)e.NewValue)
            {
                passwordBox.PasswordChanged += PasswordChanged;
            }
        }

        private static void PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            SetIsUpdating(passwordBox, true);
            SetPassword(passwordBox, passwordBox.Password);
            SetIsUpdating(passwordBox, false);
        }
    }
}
