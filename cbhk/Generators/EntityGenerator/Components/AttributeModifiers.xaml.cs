using cbhk.CustomControls.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace cbhk.Generators.EntityGenerator.Components
{
    /// <summary>
    /// AttributeModifiers.xaml 的交互逻辑
    /// </summary>
    public partial class AttributeModifiers : UserControl,IVersionUpgrader
    {
        Random random = new();

        #region 合并结果
        int currentVersion = 0;
        string UUIDString = "";
        async Task<string> IVersionUpgrader.Result()
        {
            await Upgrade(currentVersion);
            string result = "{Amount:" + Amount.Value + "d," + (ModifierName.Text.Length > 0 ? "Name:\"" + ModifierName.Text + "\"," : ",") + "Operation:" + Operation.SelectedIndex + "," + UUIDString + "}";
            return result;
        }
        #endregion

        public AttributeModifiers()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 载入运算方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Operation_Loaded(object sender, RoutedEventArgs e)
        {
            EntityDataContext entityDataContext = Window.GetWindow(sender as ComboBox).DataContext as EntityDataContext;
            Operation.ItemsSource = entityDataContext.ModifierOperationTypeSource;
        }

        public async Task Upgrade(int version)
        {
            currentVersion = version;
            if (version < 116)
                UUIDString = "UUIDLeast:" + random.NextInt64() + ",UUIDMost:" + random.NextInt64();
            else
                UUIDString = "UUID:[I;" + random.Next() + "," + random.Next() + "," + random.Next() + "," + random.Next() + "]";
            await Task.Delay(0);
        }
    }
}