using CBHK.ControlDataContext;
using CBHK.CustomControl;
using CBHK.CustomControl.Interfaces;
using CBHK.Domain;
using CBHK.GeneralTool;
using CBHK.ViewModel.Component.Item;
using CBHK.ViewModel.Generator;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace CBHK.View.Component.Item.SpecialNBT
{
    /// <summary>
    /// SuspiciousStewEffect.xaml 的交互逻辑
    /// </summary>
    public partial class SuspiciousStewEffect : UserControl,IVersionUpgrader
    {
        private CBHKDataContext _context = null;

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

        public SuspiciousStewEffect(CBHKDataContext context)
        {
            _context = context;
            InitializeComponent();
        }

        /// <summary>
        /// 删除该控件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IconTextButtons_Click(object sender, RoutedEventArgs e)
        {
            ItemPageViewModel itemPageDataContext = (sender as Button).FindParent<ItemPageView>().DataContext as ItemPageViewModel;
            StackPanel stackPanel = this.FindParent<StackPanel>();
            stackPanel.Children.Remove(this);
            stackPanel.FindParent<Accordion>().FindChild<IconButtons>().Focus();
            itemPageDataContext.VersionComponents.Remove(this);
        }

        private async void EffectID_Loaded(object sender, RoutedEventArgs e)
        {
            ItemViewModel context = Window.GetWindow(this).DataContext as ItemViewModel;
            ObservableCollection<IconComboBoxItem> source = [];
            string currentPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
            foreach (var item in _context.MobEffectSet)
            {
                string imagePath = item.ID + ".png";
                source.Add(new IconComboBoxItem()
                {
                    ComboBoxItemId = item.ID,
                    ComboBoxItemText = item.Name,
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
                            id = _context.MobEffectSet.First(item=>item.Name == currentName).Number.ToString();
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