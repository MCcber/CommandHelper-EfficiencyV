using cbhk_environment.CustomControls;
using System.Windows;
using System.Windows.Controls;

namespace cbhk_environment.Generators.WrittenBookGenerator.Components
{
    /// <summary>
    /// TextEventsForm.xaml 的交互逻辑
    /// </summary>
    public partial class TextEventsForm : UserControl
    {
        #region 允许编辑点击事件
        private bool enableEditClickEvent = false;
        public bool EnableEditClickEvent
        {
            get
            {
                return enableEditClickEvent;
            }
            set
            {
                enableEditClickEvent = value;
                ClickEventPanel.Visibility = EnableEditClickEvent ?Visibility.Visible:Visibility.Collapsed;
            }
        }
        #endregion

        #region 允许编辑悬浮事件
        private bool enableEditHoverEvent = false;
        public bool EnableEditHoverEvent
        {
            get
            {
                return enableEditHoverEvent;
            }
            set
            {
                enableEditHoverEvent = value;
                HoverEventPanel.Visibility = EnableEditHoverEvent ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        #endregion

        #region 允许插入文本
        private bool enableEditInsertion = false;
        public bool EnableEditInsertion
        {
            get
            {
                return enableEditInsertion;
            }
            set
            {
                enableEditInsertion = value;
                InsertionPanel.Visibility = EnableEditInsertion ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        #endregion

        public TextEventsForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 载入点击事件的成员
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClickEventsLoaded(object sender, RoutedEventArgs e)
        {
            ComboBox textComboBoxs = sender as ComboBox;
            textComboBoxs.ItemsSource = written_book_datacontext.clickEventSource;
            textComboBoxs.SelectedIndex = 0;
        }

        /// <summary>
        /// 载入悬浮事件的成员
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HoverEventsLoaded(object sender, RoutedEventArgs e)
        {
            ComboBox textComboBoxs = sender as ComboBox;
            textComboBoxs.ItemsSource = written_book_datacontext.hoverEventSource;
            textComboBoxs.SelectedIndex = 0;
        }

        /// <summary>
        /// 点击事件开关
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnableClickEventClick(object sender, RoutedEventArgs e)
        {
            ClickEventPanel.Visibility = (sender as RadiusToggleButtons).IsChecked.Value ? Visibility.Visible : Visibility.Collapsed;
        }
        /// <summary>
        /// 悬浮事件开关
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnableHoverEventClick(object sender, RoutedEventArgs e)
        {
            HoverEventPanel.Visibility = (sender as RadiusToggleButtons).IsChecked.Value ? Visibility.Visible : Visibility.Collapsed;
        }
        /// <summary>
        /// 插入事件开关
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnableInsertionClick(object sender, RoutedEventArgs e)
        {
            InsertionPanel.Visibility = (sender as RadiusToggleButtons).IsChecked.Value ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 打开所有事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AllEnableClick(object sender, RoutedEventArgs e)
        {
            EnableClickEvent.IsChecked = EnableHoverEvent.IsChecked = EnableInsertion.IsChecked = (sender as RadiusToggleButtons).IsChecked.Value;
        }

        /// <summary>
        /// 被动勾选点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnableClickEventChecked(object sender, RoutedEventArgs e)
        {
            EnableClickEventClick(sender,null);
        }

        /// <summary>
        /// 被动取消点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnableClickEventUnchecked(object sender, RoutedEventArgs e)
        {
            EnableClickEventClick(sender, null);
        }

        /// <summary>
        /// 被动勾选悬浮事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnableHoverEventChecked(object sender, RoutedEventArgs e)
        {
            EnableHoverEventClick(sender,null);
        }

        /// <summary>
        /// 被动取消悬浮事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnableHoverEventUnchecked(object sender, RoutedEventArgs e)
        {
            EnableHoverEventClick(sender, null);
        }

        /// <summary>
        /// 被动勾选插入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnableInsertionChecked(object sender, RoutedEventArgs e)
        {
            EnableInsertionClick(sender,null);
        }

        /// <summary>
        /// 被动取消插入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnableInsertionUnchecked(object sender, RoutedEventArgs e)
        {
            EnableInsertionClick(sender, null);
        }
    }
}
