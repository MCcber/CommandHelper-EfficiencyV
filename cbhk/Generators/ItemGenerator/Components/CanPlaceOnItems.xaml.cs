using cbhk.ControlsDataContexts;
using cbhk.CustomControls;
using cbhk.GeneralTools;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace cbhk.Generators.ItemGenerator.Components
{
    /// <summary>
    /// CanPlaceOnItems.xaml 的交互逻辑
    /// </summary>
    public partial class CanPlaceOnItems : UserControl
    {
        private IconComboBoxItem block;
        public IconComboBoxItem Block
        {
            get => block;
            set => block = value;
        }

        public string Result
        {
            get => "\"" + Block.ComboBoxItemId.Replace("minecraft:", "") + "\"";
        }

        public CanPlaceOnItems()
        {
            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        /// 删除该控件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            ItemsControl parent = this.FindParent<ItemsControl>();
            ObservableCollection<CanPlaceOnItems> canDestroyItems = parent.ItemsSource as ObservableCollection<CanPlaceOnItems>;
            //删除自己
            canDestroyItems.Remove(this);
            parent.FindParent<Accordion>().FindChild<IconButtons>().Focus();
        }

        /// <summary>
        /// 加载所有子级成员
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CanPlaceOnItemLoaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBoxs = sender as ComboBox;
            ItemPageDataContext itemPageDataContext = this.FindParent<ItemPages>().DataContext as ItemPageDataContext;
            comboBoxs.ItemsSource = itemPageDataContext.BlockList;
        }
    }
}