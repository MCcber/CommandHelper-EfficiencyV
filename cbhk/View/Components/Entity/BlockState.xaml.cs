using CBHK.ControlsDataContexts;
using CBHK.CustomControls;
using CBHK.ViewModel.Generators;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CBHK.Generators.EntityGenerator.Components
{
    /// <summary>
    /// BlockState.xaml 的交互逻辑
    /// </summary>
    public partial class BlockState : UserControl
    {
        public BlockState()
        {
            InitializeComponent();
        }

        #region 添加与清空属性
        public RelayCommand<FrameworkElement> AddAttribute { get; set; }
        public RelayCommand<FrameworkElement> ClearAttribute { get; set; }
        #endregion

        #region 字段
        private ImageBrush buttonNormal = new(new BitmapImage(new Uri("pack://application:,,,/CBHK;component/Resource/Common/Image/ButtonNormal.png", UriKind.RelativeOrAbsolute)));
        private ImageBrush buttonPressed = new(new BitmapImage(new Uri("pack://application:,,,/CBHK;component/Resource/Common/Image/ButtonPressed.png", UriKind.RelativeOrAbsolute)));
        DataTable BlockTable = null;
        DataTable BlockStateTable = null;
        #endregion

        #region 数据源
        /// <summary>
        /// 存储方块的所有属性
        /// </summary>
        public ObservableCollection<string> BlockAttributes { get; set; } = [];
        /// <summary>
        /// 存储方块属性对应的键与值集合
        /// </summary>
        public ObservableCollection<Dictionary<string, List<string>>> AttributeKeyValuePairs { get; set; } = [];
        /// <summary>
        /// 存储方块的所有属性键
        /// </summary>
        public ObservableCollection<string> AttributeKeys { get; set; } = [];
        /// <summary>
        /// 方块列表
        /// </summary>
        public ObservableCollection<IconComboBoxItem> BlockList { get; set; } = [];
        /// <summary>
        /// 已选中的方块键集合
        /// </summary>
        public ObservableCollection<string> SelectedAttributeKeys { get; set; } = [];
        /// <summary>
        /// 已选中的方块值集合
        /// </summary>
        public ObservableCollection<string> SelectedAttributeValues { get; set; } = [];

        /// <summary>
        /// 选中的方块属性
        /// </summary>
        public string SelectedBlockAttribute { get; set; }
        /// <summary>
        /// 选中的方块属性值
        /// </summary>
        public string SelectedAttributeValue { get; set; }
        #endregion

        /// <summary>
        /// 方块状态载入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BlockStateLoaded(object sender, RoutedEventArgs e)
        {
            AddAttribute = new RelayCommand<FrameworkElement>(AddAttributeCommand);
            ClearAttribute = new RelayCommand<FrameworkElement>(ClearAttributeCommand);
            AttributeAccordion.Fresh = ClearAttribute;
            AttributeAccordion.Modify = AddAttribute;
            EntityViewModel context = Window.GetWindow(sender as UserControl).DataContext as EntityViewModel;
            BlockTable = context.BlockTable;
            BlockStateTable = context.BlockStateTable;
            string currentPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
            foreach (DataRow row in BlockTable.Rows)
            {
                string id = row["id"].ToString();
                string name = row["name"].ToString();
                string imagePath = currentPath + id + ".png";
                BitmapImage bitmapImage = null;
                if (File.Exists(imagePath))
                    bitmapImage = new(new Uri(imagePath, UriKind.Absolute));
                BlockList.Add(new IconComboBoxItem()
                {
                    ComboBoxItemIcon = bitmapImage,
                    ComboBoxItemId = id,
                    ComboBoxItemText = name
                });
            }
            BlockIdBox.ItemsSource = BlockList;
            BlockIdBox.SelectionChanged += BlockIdBox_SelectionChanged;
            UpdateBlockAttributes();
        }

        /// <summary>
        /// 更新方块属性数据源
        /// </summary>
        private void UpdateBlockAttributes()
        {
            if (BlockIdBox.SelectedItem is IconComboBoxItem)
            {
                AttributeKeyValuePairs.Clear();
                AttributeKeys.Clear();
                #region 解析方块状态JSON文件,找到对应的属性集合
                string blockId = (BlockIdBox.SelectedItem as IconComboBoxItem).ComboBoxItemId;
                string properties = BlockStateTable.Select("id='minecraft:" + blockId + "'").First()["properties"].ToString();
                if (properties != null && properties.Length > 0)
                {
                    JObject currentProperties = JObject.Parse(properties);
                    SelectedAttributeKeys.Clear();
                    foreach (JProperty property in currentProperties.Properties())
                    {
                        SelectedAttributeKeys.Add(property.Name);
                        AttributeKeyValuePairs.Add([]);
                        JArray valueArray = JArray.Parse(currentProperties[property.Name].ToString());
                        AttributeKeys.Add(property.Name);
                        AttributeKeyValuePairs[^1].Add(property.Name, []);
                        for (int i = 0; i < valueArray.Count; i++)
                            AttributeKeyValuePairs[^1][property.Name].Add(valueArray[i].ToString());
                    }
                }
                #endregion
            }
        }

        /// <summary>
        /// 更新方块属性值数据源
        /// </summary>
        private void UpdateBlockAttributeValues(ComboBox comboBox)
        {
            if (comboBox.Parent is not Grid grid) return;
            if (SelectedAttributeKeys.Count == 0) return;
            int rowIndex = Grid.GetRow(comboBox);
            ComboBox keyBox = new();
            foreach (FrameworkElement item in grid.Children)
            {
                if(item is ComboBox target && Grid.GetColumn(item) == 0)
                {
                    keyBox = target;
                    break;
                }
            }
            ComboBox valueBox = grid.Children.Cast<UIElement>().First(e => Grid.GetRow(e) == rowIndex && Grid.GetColumn(e) == 1) as ComboBox;
            if (AttributeKeys.Count == 0)
            {
                grid.Children.Clear();
                valueBox.ItemsSource = null;
                valueBox.Items.Clear();
                return;
            }
            int keyIndex = keyBox.SelectedIndex;
            if (keyIndex < 0)
                keyIndex = 0;
            List<string> currentValueList = AttributeKeyValuePairs[keyIndex][SelectedAttributeKeys[rowIndex].ToString()];
            valueBox.ItemsSource = currentValueList;
            valueBox.SelectedIndex = 0;
        }

        /// <summary>
        /// 清空属性
        /// </summary>
        private void ClearAttributeCommand(FrameworkElement ele)
        {
            Accordion accordion = ele as Accordion;
            Grid grid = (accordion.Content as ScrollViewer).Content as Grid;
            grid.Children.Clear();
            grid.RowDefinitions.Clear();
            SelectedAttributeKeys.Clear();
            SelectedAttributeValues.Clear();
        }

        /// <summary>
        /// 添加属性
        /// </summary>
        public void AddAttributeCommand(FrameworkElement ele)
        {
            if (AttributeKeyValuePairs.Count == 0) return;
            Accordion accordion = ele as Accordion;
            ScrollViewer scrollViewer = accordion.Content as ScrollViewer;
            Grid grid = scrollViewer.Content as Grid;
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            int currentIndex = grid.RowDefinitions.Count - 1;
            Binding keyBinder = new()
            {
                Path = new PropertyPath("SelectedAttributeKeys[" + currentIndex + "]"),
                Mode = BindingMode.TwoWay,
                Source = this
            };
            Binding valueSelectedBinder = new()
            {
                Path = new PropertyPath("SelectedAttributeValues[" + currentIndex + "]"),
                Mode = BindingMode.TwoWay,
                Source = this
            };
            ComboBox AttributeKey = new()
            {
                Style = Application.Current.Resources["TextComboBoxStyle"] as Style,
                Uid = currentIndex.ToString(),
                Name = "key",
                DataContext = this,
                ItemsSource = AttributeKeys,
                Height = 25
            };
            SelectedAttributeKeys.Add("");
            AttributeKey.SetBinding(Selector.SelectedItemProperty, keyBinder);
            AttributeKey.SelectionChanged += AttributeKey_SelectionChanged;
            AttributeKey.SelectedIndex = 0;
            ComboBox AttributeValue = new()
            {
                Style = Application.Current.Resources["TextComboBoxStyle"] as Style,
                Uid = currentIndex.ToString(),
                Name = "value",
                DataContext = this,
                ItemsSource = AttributeKeyValuePairs[0][AttributeKeys[0]],
                Height = 25
            };
            IconTextButtons deleteButton = new()
            {
                Style = Application.Current.Resources["IconTextButton"] as Style,
                Background = buttonNormal,
                Width = 25,
                Content = "X",
                PressedBackground = buttonPressed
            };
            SelectedAttributeValues.Add("");
            AttributeValue.SetBinding(Selector.SelectedItemProperty, valueSelectedBinder);
            deleteButton.Click += (a, b) =>
            {
                grid.Children.Remove(AttributeKey);
                grid.Children.Remove(AttributeValue);
                grid.Children.Remove(a as UIElement);
            };
            grid.Children.Add(AttributeKey);
            grid.Children.Add(AttributeValue);
            grid.Children.Add(deleteButton);
            Grid.SetRow(AttributeKey, grid.RowDefinitions.Count - 1);
            Grid.SetRow(AttributeValue, grid.RowDefinitions.Count - 1);
            Grid.SetRow(deleteButton, grid.RowDefinitions.Count - 1);
            Grid.SetColumn(AttributeKey, 0);
            Grid.SetColumn(AttributeValue, 1);
            Grid.SetColumn(deleteButton, 2);
            AttributeValue.SelectedIndex = 0;
        }

        /// <summary>
        /// 属性键切换事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void AttributeKey_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateBlockAttributeValues(sender as ComboBox);
        }

        /// <summary>
        /// 方块ID更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BlockIdBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateBlockAttributes();
        }
    }
}
