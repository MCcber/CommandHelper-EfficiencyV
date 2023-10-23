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
    /// EnchantmentItems.xaml 的交互逻辑
    /// </summary>
    public partial class EnchantmentItems : UserControl
    {
        DataTable EnchantmentTable = null;
        public string Result
        {
            get
            {
                string result;
                string id = EnchantmentTable.Select("name='" + ID.SelectedValue.ToString() + "'").First()["id"].ToString();
                result = "{id:\"minecraft:"+id+"\",lvl:"+Level.Value+"s}";
                return result;
            }
        }

        public EnchantmentItems()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 载入附魔列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnchantmentIdLoaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBoxs = sender as ComboBox;
            if (comboBoxs.ItemsSource != null) return;
            ItemDataContext context = Window.GetWindow(this).DataContext as ItemDataContext;
            EnchantmentTable = context.EnchantmentTable;
            ObservableCollection<IconComboBoxItem> source = new();
            string currentPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
            foreach (DataRow row in EnchantmentTable.Rows)
            {
                string id = row["id"].ToString();
                string name = row["name"].ToString();
                string imagePath = id + ".png";
                if (File.Exists(currentPath + imagePath))
                    source.Add(new IconComboBoxItem()
                    {
                        ComboBoxItemIcon = new BitmapImage(new Uri(currentPath + imagePath, UriKind.Absolute)),
                        ComboBoxItemId = id,
                        ComboBoxItemText = name
                    });
            }
            comboBoxs.ItemsSource = source;
        }

        /// <summary>
        /// 删除该控件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            StackPanel parent = this.FindParent<StackPanel>();
            //删除自己
            parent.Children.Remove(this);
            parent.FindParent<Accordion>().FindChild<IconButtons>().Focus();
        }
    }
}
