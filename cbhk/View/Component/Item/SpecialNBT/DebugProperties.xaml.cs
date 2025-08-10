using CBHK.CustomControl;
using CBHK.Domain;
using CBHK.GeneralTool;
using CBHK.ViewModel.Generator;
using Newtonsoft.Json.Linq;
using SQLitePCL;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CBHK.View.Component.Item.SpecialNBT
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
        private static JObject BlocksPropertyObject = null;
        private CBHKDataContext _context = null;
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

        public DebugProperties(CBHKDataContext context)
        {
            _context = context;
            InitializeComponent();
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        private async void DebugProperty_Loaded(object sender,RoutedEventArgs e)
        {
            string currentPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
            ObservableCollection<IconComboBoxItem> Blocksource = [];
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var block in _context.BlockSet)
                {
                    string imagePath = currentPath + block.ID + ".png";
                    ImageSource imageSource = new BitmapImage();
                    if (File.Exists(imagePath))
                        imageSource = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
                    Blocksource.Add(new IconComboBoxItem()
                    {
                        ComboBoxItemIcon = imageSource,
                        ComboBoxItemId = block.ID,
                        ComboBoxItemText = block.Name
                    });
                }
            });
            BlockId.ItemsSource = Blocksource;

            BlockProperty.ItemsSource = PropertyList;
            string propertiesContent = _context.BlockStateSet.First().Properties;
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
                string properties = _context.BlockStateSet.First(item => item.ID == selectedBlock).Properties;
                if (properties is not null && properties.Trim().Length > 0)
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