using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace cbhk_environment.SettingForm
{
    /// <summary>
    /// StartupItemForm.xaml 的交互逻辑
    /// </summary>
    public partial class StartupItemForm
    {
        public StartupItemForm()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void CommonWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DialogResult = true;
        }

        public void StateItemLoaded(object sender,RoutedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            comboBox.ItemsSource = new ObservableCollection<string> { "保持不变","最小化","关闭" };
        }
    }
}
