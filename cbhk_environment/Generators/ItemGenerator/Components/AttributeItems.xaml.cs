using cbhk_environment.GeneralTools;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace cbhk_environment.Generators.ItemGenerator.Components
{
    /// <summary>
    /// AttributeItems.xaml 的交互逻辑
    /// </summary>
    public partial class AttributeItems : UserControl
    {
        #region 属性分量
        private string attributeID;
        private string attributeIDString = "";
        public string AttributeID
        {
            get => attributeID;
            set
            {
                attributeID = value;
                attributeIDString = AttributeTable.Select("name='" + value + "'").First()["id"].ToString();
            }
        }

        private double attributeValue = 0;
        public double AttributeValue
        {
            get => attributeValue;
            set => attributeValue = value;
        }

        private string attributeName = "";
        public string AttributeName
        {
            get => attributeName;
            set => attributeName = value;
        }

        public string attributeSlot = "";
        public string AttributeSlot
        {
            get => attributeSlot;
            set => attributeSlot = value;
        }
        #endregion

        #region 数据表
        DataTable AttributeTable = null;
        DataTable AttributeSlotTable = null;
        DataTable AttributeValueTypeTable = null;
        #endregion

        /// <summary>
        /// 合并数据
        /// </summary>
        public string Result
        {
            get
            {
                Random random = new();
                string uid0 = random.Next(1000,10000).ToString();
                string uid1 = random.Next(1000, 10000).ToString();
                string uid2 = random.Next(1000, 10000).ToString();
                string uid3 = random.Next(1000, 10000).ToString();
                string attributeSlotString = AttributeSlotTable.Select("value='" + AttributeSlot + "'").First()["id"].ToString();
                string slotData = attributeSlotString == "all" ? "" : ",Slot:\""+ attributeSlotString+"\"";
                string result = "{AttributeName:\""+attributeIDString+"\",Name:\""+AttributeName+"\",Amount:"+AttributeValue+"d,Operation:"+Operations.SelectedIndex+",UUID:[I;"+uid0+","+uid1+","+uid2+","+uid3+"]"+slotData + "}";
                return result;
            }
        }

        public AttributeItems()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void AttributeItems_Loaded(object sender, RoutedEventArgs e)
        {
            item_datacontext context = Window.GetWindow(this).DataContext as item_datacontext;
            AttributeTable = context.AttributeTable;
            AttributeSlotTable = context.AttributeSlotTable;
            AttributeValueTypeTable = context.AttributeValueTypeTable;
        }

        /// <summary>
        /// 载入属性ID列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AttributeIdsLoaded(object sender, RoutedEventArgs e)
        {
            ComboBox ComboBox = sender as ComboBox;
            ObservableCollection<string> source = new();
            foreach (DataRow row in AttributeTable.Rows)
            {
                source.Add(row["id"].ToString());
            }
            ComboBox.ItemsSource = source;
        }

        /// <summary>
        /// 载入属性生效槽位ID列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AttributeSlotsLoaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            ObservableCollection<string> source = new();
            foreach (DataRow row in AttributeSlotTable.Rows)
                source.Add(row["value"].ToString());
            comboBox.ItemsSource = source;
        }

        /// <summary>
        /// 载入属性值类型ID列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AttributeValueTypesLoaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            ObservableCollection<string> source = new();
            foreach (DataRow row in AttributeValueTypeTable.Rows)
                source.Add(row["value"].ToString());
            comboBox.ItemsSource = source;
        }

        /// <summary>
        /// 删除当前属性成员
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<AttributeItems> attributeItems = this.FindParent<Data>().AttributeSource;
            attributeItems.Remove(this);
        }
    }
}
