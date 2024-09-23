using cbhk.ControlsDataContexts;
using cbhk.CustomControls;
using cbhk.GeneralTools;
using cbhk.ViewModel.Generators;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace cbhk.Generators.ItemGenerator.Components.SpecialNBT
{
    /// <summary>
    /// DebugProperties.xaml 的交互逻辑
    /// </summary>
    public partial class DebugProperties : UserControl
    {
        #region 方块ID集合与方块键集合
        private ObservableCollection<string> PropertyList = [];
        #endregion

        #region 字段
        DataTable BlockTable = null;
        DataTable BlockStateTable = null;
        private static JObject BlocksPropertyObject = null;
        #endregion

        #region 合并数据
        public string Result
        {
            get
            {
                string result = "";
                string selectedBlock = (BlockId.SelectedValue as IconComboBoxItem).ComboBoxItemId.Replace("minecraft:","");
                bool IsNumber = int.TryParse(BlockProperty.SelectedValue.ToString(),out int number);
                _ = bool.TryParse(BlockProperty.SelectedValue.ToString(),out bool boolean);
                string value = "";
                if (IsNumber)
                    value = number.ToString();
                else
                if (boolean)
                    value = boolean.ToString().ToLower();
                else
                    value = BlockProperty.SelectedValue.ToString();
                result = "\"" + selectedBlock + "\":" + ((IsNumber || boolean) ? value : "\"" + value + "\"");
                return result;
            }
        }
        #endregion

        public DebugProperties()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        private async void DebugProperty_Loaded(object sender,RoutedEventArgs e)
        {
            ItemViewModel context = Window.GetWindow(this).DataContext as ItemViewModel;
            BlockTable = context.BlockTable;
            BlockStateTable = context.BlockStateTable;

            string currentPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
            ObservableCollection<IconComboBoxItem> Blocksource = [];
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (DataRow row in BlockTable.Rows)
                {
                    string id = row["id"].ToString();
                    string name = row["name"].ToString();
                    string imagePath = currentPath + id + ".png";
                    ImageSource imageSource = new BitmapImage();
                    if (File.Exists(imagePath))
                        imageSource = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
                    Blocksource.Add(new IconComboBoxItem()
                    {
                        ComboBoxItemIcon = imageSource,
                        ComboBoxItemId = id,
                        ComboBoxItemText = name
                    });
                }
            });
            BlockId.ItemsSource = Blocksource;

            BlockProperty.ItemsSource = PropertyList;
            string propertiesContent = BlockStateTable.Select("id='" + BlockStateTable.Rows[0]["id"].ToString() + "'").First()["properties"].ToString();
            BlocksPropertyObject = JObject.Parse(propertiesContent);
            BlockId.SelectedIndex = 0;
            BlockProperty.SelectedIndex = 0;
        }

        /// <summary>
        /// 更新方块ID，更新右侧方块对应的key集合
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BlockId_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(BlockId.SelectedValue is IconComboBoxItem iconComboBoxItem)
            {
                string selectedBlock = iconComboBoxItem.ComboBoxItemId.Replace("minecraft:", "");
                PropertyList.Clear();
                DataRow[] dataRows = BlockStateTable.Select("id='minecraft:" + selectedBlock + "'");
                if (dataRows.Length > 0 && dataRows.First()["properties"] is string properties && properties.Trim().Length > 0)
                {
                    BlocksPropertyObject = JObject.Parse(properties);
                    foreach (JProperty item in BlocksPropertyObject.Properties())
                        PropertyList.Add(item.Name);
                    BlockProperty.SelectedIndex = 0;
                }
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            StackPanel stackPanel = Parent as StackPanel;
            stackPanel.Children.Remove(this);
            stackPanel.FindParent<Accordion>().FindChild<IconButtons>().Focus();
        }
    }
}