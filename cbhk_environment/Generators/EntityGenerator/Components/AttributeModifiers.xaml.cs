using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace cbhk.Generators.EntityGenerator.Components
{
    /// <summary>
    /// AttributeModifiers.xaml 的交互逻辑
    /// </summary>
    public partial class AttributeModifiers : UserControl
    {
        string ModifierOperationTypeFilePath = AppDomain.CurrentDomain.BaseDirectory + "resources\\configs\\Entity\\data\\AttributeModifierOperationType.ini";
        ObservableCollection<string> ModifierOperationTypeSource = new();
        public string Result
        {
            get
            {
                string result;
                result = "{Amount:" + Amount.Value + "d" + (ModifierName.Text.Length > 0?",Name:\"" + ModifierName.Text + "\"":"") + ",Operation:" + Operation.SelectedIndex + ",UUID:[" + UUID.number0.Value + "," + UUID.number1.Value + "," + UUID.number2.Value + "," + UUID.number3.Value + "]}";
                return result;
            }
        }
        public AttributeModifiers()
        {
            InitializeComponent();
            UUID.EnableButton.IsChecked = true;
            UUID.EnableButton.IsHitTestVisible = false;
        }

        /// <summary>
        /// 载入运算方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Operation_Loaded(object sender, RoutedEventArgs e)
        {
            List<string> ModifierOperationTypeList = File.ReadAllLines(ModifierOperationTypeFilePath).ToList();
            _ = ModifierOperationTypeList.All(item =>
            {
                ModifierOperationTypeSource.Add(item);
                return true;
            });
            Operation.ItemsSource = ModifierOperationTypeSource;
        }
    }
}
