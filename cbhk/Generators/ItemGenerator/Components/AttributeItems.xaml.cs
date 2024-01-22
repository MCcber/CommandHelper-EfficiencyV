using cbhk.CustomControls.Interfaces;
using cbhk.GeneralTools;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace cbhk.Generators.ItemGenerator.Components
{
    /// <summary>
    /// AttributeItems.xaml 的交互逻辑
    /// </summary>
    public partial class AttributeItems : UserControl,IVersionUpgrader
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

        #region 合并数据
        int currentVersion = 0;
        async Task<string> IVersionUpgrader.Result()
        {
            await Upgrade(currentVersion);
            string attributeSlotString = AttributeSlotTable.Select("value='" + AttributeSlot + "'").First()["id"].ToString();
            string slotData = attributeSlotString == "all" ? "" : ",Slot:\"" + attributeSlotString + "\"";
            string result = "{AttributeName:\"" + attributeIDString + "\",Name:\"" + AttributeName + "\",Amount:" + AttributeValue + "d,Operation:" + Operations.SelectedIndex + UUIDString + slotData + "}";
            return result;
        }
        #endregion

        private string UUIDString = "";

        public AttributeItems()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void AttributeItems_Loaded(object sender, RoutedEventArgs e)
        {
            ItemDataContext context = Window.GetWindow(this)?.DataContext as ItemDataContext;
            if (context is not null)
            {
                AttributeTable = context.AttributeTable;
                AttributeSlotTable = context.AttributeSlotTable;
                AttributeValueTypeTable = context.AttributeValueTypeTable;
            }
        }

        /// <summary>
        /// 载入属性ID列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AttributeIdsLoaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            ObservableCollection<string> source = [];
            foreach (DataRow row in AttributeTable.Rows)
            {
                source.Add(row["name"].ToString());
            }
            comboBox.ItemsSource = source;
        }

        /// <summary>
        /// 载入属性生效槽位ID列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AttributeSlotsLoaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            ObservableCollection<string> source = [];
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
            ObservableCollection<string> source = [];
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

        public async Task Upgrade(int version)
        {
            currentVersion = version;
            await Task.Delay(0);
            Random random = new();
            if (version < 116)
                UUIDString = ",UUIDLeast:" + random.NextInt64() + ",UUIDMost:" + random.NextInt64();
            else
            {
                UUIDString = ",UUID:[I;" + random.Next(1000, 10000).ToString() + "," + random.Next(1000, 10000).ToString() + "," + random.Next(1000, 10000).ToString() + "," + random.Next(1000, 10000).ToString() + "]";
            }
        }
    }
}