using cbhk_environment.CustomControls;
using cbhk_environment.GeneralTools;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace cbhk_environment.Generators.EntityGenerator.Components
{
    /// <summary>
    /// Attributes.xaml 的交互逻辑
    /// </summary>
    public partial class Attributes : UserControl
    {
        #region 字段
        DataTable MobAttributeTable = null;
        ObservableCollection<string> AttributeNames { get; set; } = new();
        public ObservableCollection<AttributeModifiers> AttributeModifiersSource { get; set; } = new();
        #endregion

        #region 合并数据
        public string Result
        {
            get
            {
                string result = "";
                string SelectedName = AttributeName.SelectedValue.ToString();
                if(SelectedName.Length > 0)
                result = "{Base: " + Base.Value + "d" + (AttributeModifiersSource.Count > 0 ? ",Modifiers:[" + string.Join(',', AttributeModifiersSource.Select(item => item.Result)) + "]" : "") + ",Name:\"" + SelectedName[(SelectedName.IndexOf(':') + 1)..SelectedName.LastIndexOf(':')] + "\"}";
                return result;
            }
        }
        #endregion

        public Attributes()
        {
            InitializeComponent();
            entity_datacontext context = Window.GetWindow(this).DataContext as entity_datacontext;
            MobAttributeTable = context.MobAttributeTable;
        }

        /// <summary>
        /// 载入属性名称
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AttributeName_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            foreach (DataRow row in MobAttributeTable.Rows)
            {
                string id = row["id"].ToString();
                string name = row["name"].ToString();
                AttributeNames.Add(id+":"+name);
            }
            comboBox.ItemsSource = AttributeNames;
        }

        /// <summary>
        /// 切换选中的属性名后更新对应的值范围
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AttributeName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string[] range = MobAttributeTable.Rows[AttributeName.SelectedIndex]["DataRange"].ToString().Split('-');
            Base.Minimum = double.Parse(range[0].ToString());
            Base.Maximum = double.Parse(range[1].ToString());
        }

        /// <summary>
        /// 载入修饰符添加与清空行为
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Modifiers_Loaded(object sender, RoutedEventArgs e)
        {
            Modifiers.Modify = new RelayCommand<FrameworkElement>(AddModifierCommand);
            Modifiers.Fresh = new RelayCommand<FrameworkElement>(ClearModifierCommand);
            ItemsControl itemsControl = (Modifiers.Content as ScrollViewer).Content as ItemsControl;
            itemsControl.ItemsSource = AttributeModifiersSource;
        }

        /// <summary>
        /// 添加修饰成员
        /// </summary>
        /// <param name="obj"></param>
        public void AddModifierCommand(FrameworkElement obj) => AttributeModifiersSource.Add(new AttributeModifiers());

        /// <summary>
        /// 清空修饰成员
        /// </summary>
        /// <param name="obj"></param>
        private void ClearModifierCommand(FrameworkElement obj) => AttributeModifiersSource.Clear();

        /// <summary>
        /// 删除该控件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteButtons_Click(object sender, RoutedEventArgs e)
        {
            StackPanel stackPanel = Parent as StackPanel;
            stackPanel.Children.Remove(this);
            Accordion accordion = stackPanel.FindParent<Accordion>();
            accordion.FindChild<IconButtons>().Focus();
        }
    }
}
