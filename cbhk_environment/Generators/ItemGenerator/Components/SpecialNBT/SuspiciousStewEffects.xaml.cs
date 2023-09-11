using ABI.Windows.ApplicationModel.Activation;
using cbhk_environment.ControlsDataContexts;
using cbhk_environment.CustomControls;
using cbhk_environment.GeneralTools;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace cbhk_environment.Generators.ItemGenerator.Components.SpecialNBT
{
    /// <summary>
    /// SuspiciousStewEffects.xaml 的交互逻辑
    /// </summary>
    public partial class SuspiciousStewEffects : UserControl
    {
        DataTable EffectTable = null;
        #region 合并结果
        public string Result
        {
            get
            {
                string currentName = (EffectID.SelectedItem as IconComboBoxItem).ComboBoxItemText;
                string EffectIdResult = EffectTable.Select("id='" + currentName + "'").First()["num"].ToString();
                string result = "{EffectDuration:"+EffectDuration.Value+ ",EffectId:"+ EffectIdResult + "}";
                return result;
            }
        }
        #endregion

        public SuspiciousStewEffects()
        {
            InitializeComponent();
            item_datacontext context = Window.GetWindow(this).DataContext as item_datacontext;
            EffectTable = context.EffectTable;
            ObservableCollection<IconComboBoxItem> source = new();
            string currentPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
            foreach (DataRow row in EffectTable.Rows)
            {
                string id = row["id"].ToString();
                string name = row["name"].ToString();
                string imagePath = id + ".png";
                source.Add(new IconComboBoxItem()
                {
                    ComboBoxItemId = id,
                    ComboBoxItemText = name,
                    ComboBoxItemIcon = new BitmapImage(new Uri(currentPath + imagePath,UriKind.Absolute))
                });
            }
            EffectID.ItemsSource = source;
        }

        /// <summary>
        /// 删除该控件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IconTextButtons_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            StackPanel stackPanel = this.FindParent<StackPanel>();
            stackPanel.Children.Remove(this);
            stackPanel.FindParent<Accordion>().FindChild<IconButtons>().Focus();
        }
    }
}
