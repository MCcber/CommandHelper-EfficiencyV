using CBHK.ControlsDataContexts;
using CBHK.CustomControls;
using CBHK.CustomControls.Interfaces;
using CBHK.GeneralTools;
using CBHK.ViewModel.Generators;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace CBHK.Generators.ItemGenerator.Components.SpecialNBT
{
    /// <summary>
    /// SuspiciousStewEffects.xaml 的交互逻辑
    /// </summary>
    public partial class SuspiciousStewEffects : UserControl,IVersionUpgrader
    {
        DataTable EffectTable = null;

        #region 合并结果
        string id = "";
        int currentVersion = 0;

        async Task<string> IVersionUpgrader.Result()
        {
            await Upgrade(currentVersion);
            string result = "{duration:" + int.Parse(EffectDuration.Value.ToString()) + ",id:" + id + "}";
            return result;
        }
        #endregion

        public SuspiciousStewEffects()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 删除该控件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IconTextButtons_Click(object sender, RoutedEventArgs e)
        {
            ItemPageViewModel itemPageDataContext = (sender as Button).FindParent<ItemPagesView>().DataContext as ItemPageViewModel;
            StackPanel stackPanel = this.FindParent<StackPanel>();
            stackPanel.Children.Remove(this);
            stackPanel.FindParent<Accordion>().FindChild<IconButtons>().Focus();
            itemPageDataContext.VersionComponents.Remove(this);
        }

        private async void EffectID_Loaded(object sender, RoutedEventArgs e)
        {
            ItemViewModel context = Window.GetWindow(this).DataContext as ItemViewModel;
            EffectTable = context.EffectTable;
            ObservableCollection<IconComboBoxItem> source = [];
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
                    ComboBoxItemIcon = new BitmapImage(new Uri(currentPath + imagePath, UriKind.Absolute))
                });
            }
            EffectID.ItemsSource = source;
            id = "\"minecraft:" + source[0].ComboBoxItemId + "\"";
            await Upgrade(1202);
        }

        public async Task Upgrade(int version)
        {
            currentVersion = version;
            await Task.Run(() =>
            {
                id = "";
                IconComboBoxItem iconComboBoxItem = null;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    iconComboBoxItem = EffectID.SelectedItem as IconComboBoxItem;
                });

                if (iconComboBoxItem is not null)
                {
                    string currentName = iconComboBoxItem.ComboBoxItemText;
                    if (version < 116)
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            id = EffectTable.Select("id='" + currentName + "'").First()["number"].ToString();
                        });
                    else
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            id = "\"minecraft:" + iconComboBoxItem.ComboBoxItemId + "\"";
                        });
                }
            });
        }
    }
}