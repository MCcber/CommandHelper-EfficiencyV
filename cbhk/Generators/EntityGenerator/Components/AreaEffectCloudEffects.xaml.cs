using cbhk.ControlsDataContexts;
using cbhk.CustomControls;
using cbhk.CustomControls.Interfaces;
using cbhk.GeneralTools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace cbhk.Generators.EntityGenerator.Components
{
    /// <summary>
    /// AreaEffectCloudEffects.xaml 的交互逻辑
    /// </summary>
    public partial class AreaEffectCloudEffects : UserControl,IVersionUpgrader
    {
        #region 浮点数取值范围
        public float FloatMinValue
        {
            get => float.MinValue;
        }
        public float FloatMaxValue
        {
            get => float.MaxValue;
        }
        #endregion

        DataTable MobEffectTable = new();

        #region 合并数据
        int currentVersion = 0;
        string id = "";
        async Task<string> IVersionUpgrader.Result()
        {
            await Upgrade(currentVersion);
            string result = "";
            List<string> Data = [];
            List<string> FactorCalculationList = [];
            foreach (FrameworkElement component in EffectListPanel.Children)
            {
                if (component is DockPanel)
                {
                    DockPanel dockPanel = component as DockPanel;
                    foreach (FrameworkElement subChild in dockPanel.Children)
                    {
                        if (subChild is Slider)
                        {
                            Slider slider = subChild as Slider;
                            Data.Add(slider.Uid + ":" + slider.Value);
                        }
                        if (subChild is TextCheckBoxs)
                        {
                            TextCheckBoxs textCheckBoxs = subChild as TextCheckBoxs;
                            if (textCheckBoxs.IsChecked.Value)
                                Data.Add(textCheckBoxs.Uid + ":1b");
                        }
                        if (subChild is ComboBox comboBox)
                        {
                            Data.Add(comboBox.Uid + ":" + id);
                        }
                    }
                }
                else
                if (component is TextCheckBoxs)
                {
                    TextCheckBoxs textCheckBoxs = component as TextCheckBoxs;
                    if (textCheckBoxs.IsChecked.Value)
                        Data.Add(textCheckBoxs.Uid + ":1b");
                }
                else
                if (component is Slider)
                {
                    Slider slider = component as Slider;
                    if (slider.Value > 0)
                        Data.Add(slider.Uid + ":" + slider.Value + (Equals(slider.Maximum, float.MaxValue) ? "f" : ""));
                }
            }
            foreach (FrameworkElement component in FactorCalculationDataGrid.Children)
            {
                if (component is TextCheckBoxs)
                {
                    TextCheckBoxs textCheckBoxs = component as TextCheckBoxs;
                    if (textCheckBoxs.IsChecked.Value)
                        FactorCalculationList.Add(textCheckBoxs.Uid + ":1b");
                }
                else
                if (component is Slider)
                {
                    Slider slider = component as Slider;
                    if (slider.Value > 0)
                        FactorCalculationList.Add(slider.Uid + ":" + slider.Value + (Equals(slider.Maximum, float.MaxValue) ? "f" : ""));
                }
            }
            result = "{" + string.Join(",", Data) + (FactorCalculationList.Count > 0 ? ",FactorCalculationData:{" + string.Join(",", FactorCalculationList) + "}" : "") + "}";
            if (result == "{}")
                result = "";
            return result;
        }
        #endregion

        public AreaEffectCloudEffects()
        {
            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        /// 删除此状态效果
        /// </summary>
        /// <param name="obj"></param>
        private void CloseEffectCommand(FrameworkElement obj)
        {
            this.FindParent<Accordion>().FindChild<IconButtons>().Focus();
            (Parent as StackPanel).Children.Remove(this);
        }

        /// <summary>
        /// 载入状态效果数据源
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void EffectID_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            ObservableCollection<IconComboBoxItem> source = [];
            string currentPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
            foreach (DataRow item in MobEffectTable.Rows)
            {
                string id = item["id"].ToString();
                string name = item["name"].ToString();
                string imagePath = "";
                if (File.Exists(currentPath + id + ".png"))
                    imagePath = currentPath + id + ".png";
                source.Add(new IconComboBoxItem()
                {
                    ComboBoxItemIcon = new BitmapImage(new Uri(imagePath, UriKind.Absolute)),
                    ComboBoxItemId = id,
                    ComboBoxItemText = name
                });
            }
            comboBox.ItemsSource = source;
            comboBox.SelectedIndex = 0;

            await Upgrade(1202);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            EffectAccordion.Fresh = new CommunityToolkit.Mvvm.Input.RelayCommand<FrameworkElement>(CloseEffectCommand);
            ObservableCollection<IconComboBoxItem> source = [];
            EntityDataContext context = Window.GetWindow(this).DataContext as EntityDataContext;
            MobEffectTable = context.MobEffectTable;
        }

        public async Task Upgrade(int version)
        {
            currentVersion = version;
            await Task.Delay(0);
            if(version < 116)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    string number = MobEffectTable.Select("id='" + (ID.SelectedItem as IconComboBoxItem).ComboBoxItemId + "'")?.First()["number"].ToString();
                    if (number is not null)
                        id = number;
                });
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    id = "\"minecraft:" + (ID.SelectedItem as IconComboBoxItem).ComboBoxItemId + "\"";
                });
            }
        }
    }
}