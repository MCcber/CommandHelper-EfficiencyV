using CBHK.CustomControl.Interfaces;
using CBHK.ViewModel.Generator;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CBHK.View.Component.Entity
{
    /// <summary>
    /// AttributeModifiers.xaml 的交互逻辑
    /// </summary>
    public partial class AttributeModifiers : UserControl,IVersionUpgrader
    {
        Random random = new();

        #region 合并结果
        int CurrentVersion = 1202;
        string UUIDString = "";
        async Task<string> IVersionUpgrader.Result()
        {
            await Upgrade(CurrentVersion);
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
            EntityViewModel entityDataContext = Window.GetWindow(sender as ComboBox).DataContext as EntityViewModel;
            Operation.ItemsSource = entityDataContext.ModifierOperationTypeSource;
        }

        public async Task Upgrade(int version)
        {
            CurrentVersion = version;
            if (version < 116)
                UUIDString = "UUIDLeast:" + random.NextInt64() + ",UUIDMost:" + random.NextInt64();
            else
                UUIDString = "UUID:[I;" + random.Next() + "," + random.Next() + "," + random.Next() + "," + random.Next() + "]";
            await Task.Delay(0);
        }
    }
}