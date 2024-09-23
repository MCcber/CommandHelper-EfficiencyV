using cbhk.CustomControls;
using cbhk.CustomControls.Interfaces;
using cbhk.GeneralTools;
using cbhk.ViewModel.Generators;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace cbhk.Generators.EntityGenerator.Components
{
    /// <summary>
    /// Attributes.xaml 的交互逻辑
    /// </summary>
    public partial class Attributes : UserControl
    {
        #region 字段
        DataTable MobAttributeTable = null;
        ObservableCollection<TextComboBoxItem> AttributeNames { get; set; } = [];
        public ObservableCollection<AttributeModifiers> AttributeModifiersSource { get; set; } = [];
        #endregion

        #region 合并数据
        public string Result
        {
            get
            {
                string result = "";
                string SelectedName = (AttributeName.SelectedItem as TextComboBoxItem).Text.ToString();
                if(SelectedName.Length > 0)
                {
                    result = "{Base: " + Base.Value + "d" + (AttributeModifiersSource.Count > 0 ? ",Modifiers:[" + string.Join(',', AttributeModifiersSource.Select(item => (item as IVersionUpgrader).Result().Result)) + "]" : "") + ",Name:\"" + SelectedName[(SelectedName.IndexOf(':') + 1)..SelectedName.LastIndexOf(':')] + "\"}";
                }
                return result;
            }
        }
        #endregion

        public Attributes()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 载入属性名称
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AttributeName_Loaded(object sender, RoutedEventArgs e)
        {
            EntityViewModel context = Window.GetWindow(this).DataContext as EntityViewModel;
            MobAttributeTable = context.MobAttributeTable;
            ComboBox comboBox = sender as ComboBox;
            foreach (DataRow row in MobAttributeTable.Rows)
            {
                string id = row["id"].ToString();
                string name = row["name"].ToString();
                AttributeNames.Add(new TextComboBoxItem() { Text = id + ":" + name });
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
        public void AddModifierCommand(FrameworkElement obj)
        {
            AttributeModifiers attributeModifiers = new();
            AttributeModifiersSource.Add(attributeModifiers);
            EntityPagesViewModel entityPagesDataContext = obj.FindParent<EntityPagesView>().DataContext as EntityPagesViewModel;
            entityPagesDataContext.VersionComponents.Add(attributeModifiers);
        }

        /// <summary>
        /// 清空修饰成员
        /// </summary>
        /// <param name="obj"></param>
        private void ClearModifierCommand(FrameworkElement obj)
        {
            EntityPagesViewModel entityPagesDataContext = obj.FindParent<EntityPagesView>().DataContext as EntityPagesViewModel;
            for (int i = 0; i < AttributeModifiersSource.Count; i++)
            {
                entityPagesDataContext.VersionComponents.Remove(AttributeModifiersSource[i]);
                AttributeModifiersSource.RemoveAt(i);
            }
        }

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
