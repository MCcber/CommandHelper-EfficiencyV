using cbhk.ControlsDataContexts;
using cbhk.CustomControls;
using cbhk.GeneralTools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace cbhk.Generators.EntityGenerator.Components
{
    /// <summary>
    /// AreaEffectCloudEffects.xaml 的交互逻辑
    /// </summary>
    public partial class AreaEffectCloudEffects : UserControl
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
        private bool HaveResult = true;
        public string Result
        {
            get
            {
                if(HaveResult)
                {
                    string result = "";
                    List<string> Data = new();
                    List<string> FactorCalculationList = new();
                    foreach (FrameworkElement component in EffectListPanel.Children)
                    {
                        if(component is DockPanel)
                        {
                            DockPanel dockPanel = component as DockPanel;
                            foreach (FrameworkElement subChild in dockPanel.Children)
                            {
                                if(subChild is Slider)
                                {
                                    Slider slider = subChild as Slider;
                                    Data.Add(slider.Uid + ":" + slider.Value);
                                }
                                if(subChild is TextCheckBoxs)
                                {
                                    TextCheckBoxs textCheckBoxs = subChild as TextCheckBoxs;
                                    if (textCheckBoxs.IsChecked.Value)
                                        Data.Add(textCheckBoxs.Uid + ":1b");
                                }
                                if(subChild is ComboBox)
                                {
                                    ComboBox comboBox = subChild as ComboBox;
                                    string currentID = MobEffectTable.Select("name='" + comboBox.SelectedItem.ToString() + "'")?.First()["id"].ToString();
                                    Data.Add(comboBox.Uid + ":\"minecraft:" +currentID + "\"");
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
                return "";
            }
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
            (Parent as StackPanel).Children.Remove(this);
            HaveResult = false;
            this.FindParent<Accordion>().FindChild<IconButtons>().Focus();
        }

        /// <summary>
        /// 载入状态效果数据源
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EffectID_Loaded(object sender, RoutedEventArgs e)
        {
            ObservableCollection<IconComboBoxItem> source = new();
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
            (sender as ComboBox).ItemsSource = source;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            EffectAccordion.Fresh = new CommunityToolkit.Mvvm.Input.RelayCommand<FrameworkElement>(CloseEffectCommand);
            ObservableCollection<IconComboBoxItem> source = new();
            EntityDataContext context = Window.GetWindow(this).DataContext as EntityDataContext;
            MobEffectTable = context.MobEffectTable;
        }
    }
}
