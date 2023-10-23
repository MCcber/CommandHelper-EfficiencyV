using cbhk.ControlsDataContexts;
using cbhk.CustomControls;
using cbhk.GeneralTools;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace cbhk.Generators.ItemGenerator.Components
{
    /// <summary>
    /// CanDestroyItems.xaml 的交互逻辑
    /// </summary>
    public partial class CanDestroyItems : UserControl
    {
        DataTable BlockTable = null;
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
        public CanDestroyItems()
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
            ObservableCollection<CanDestroyItems> canDestroyItems = parent.ItemsSource as ObservableCollection<CanDestroyItems>;
            //删除自己
            canDestroyItems.Remove(this);
            parent.FindParent<Accordion>().FindChild<IconButtons>().Focus();
        }

        /// <summary>
        /// 加载所有子级成员
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CanDestroyItemLoaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBoxs = sender as ComboBox;
            if (comboBoxs.ItemsSource != null) return;
            ItemDataContext context = Window.GetWindow(this).DataContext as ItemDataContext;
            BlockTable = context.BlockTable;
            ObservableCollection<IconComboBoxItem> source = new();
            string currentPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
            foreach (DataRow row in BlockTable.Rows)
            {
                string id = row["id"].ToString();
                string name = row["name"].ToString();
                string imagePath = id + ".png";
                if(File.Exists(currentPath + imagePath))
                source.Add(new IconComboBoxItem()
                {
                    ComboBoxItemId = id,
                    ComboBoxItemText = name,
                    ComboBoxItemIcon = new BitmapImage(new Uri(currentPath + imagePath, UriKind.Absolute))
                });
            }
            comboBoxs.ItemsSource = source;
        }
    }
}
