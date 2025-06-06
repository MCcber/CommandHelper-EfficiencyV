﻿using CBHK.ControlDataContext;
using CBHK.CustomControl;
using CBHK.CustomControl.Interfaces;
using CBHK.GeneralTool;
using CBHK.ViewModel.Component.Item;
using CBHK.ViewModel.Generator;
using System;
using System.Collections.Generic;
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
    /// CustomPotionEffects.xaml 的交互逻辑
    /// </summary>
    public partial class CustomPotionEffects : UserControl,IVersionUpgrader
    {
        DataTable EffectTable = null;

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

        #region 合并数据
        int currentVersion = 0;
        private string id = "";
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
                        if (subChild is ComboBox)
                        {
                            Data.Add("Id:" + id);
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

        public CustomPotionEffects()
        {
            InitializeComponent();
            EffectAccordion.Fresh = new CommunityToolkit.Mvvm.Input.RelayCommand<FrameworkElement>(CloseEffectCommand);
        }

        /// <summary>
        /// 删除此状态效果
        /// </summary>
        /// <param name="obj"></param>
        private void CloseEffectCommand(FrameworkElement obj)
        {
            ItemPageViewModel itemPageDataContext = obj.FindParent<ItemPageView>().DataContext as ItemPageViewModel;
            StackPanel stackPanel = Parent as StackPanel;
            stackPanel.Children.Remove(this);
            stackPanel.FindParent<Accordion>().FindChild<IconButtons>().Focus();
            itemPageDataContext.VersionComponents.Remove(this);
        }

        /// <summary>
        /// 载入状态效果数据源
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void EffectID_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            ObservableCollection<IconComboBoxItem> source = [];
            string currentPath = AppDomain.CurrentDomain.BaseDirectory + "ImageSet\\";
            EffectTable = (Window.GetWindow(this).DataContext as ItemViewModel).EffectTable;
            foreach (DataRow row in EffectTable.Rows)
            {
                string id = row["id"].ToString();
                string name = row["name"].ToString();
                string imagePath = currentPath + id + ".png";
                source.Add(new IconComboBoxItem()
                {
                    ComboBoxItemIcon = new BitmapImage(new Uri(imagePath,UriKind.Absolute)),
                    ComboBoxItemId = id,
                    ComboBoxItemText = name
                });
            }
            comboBox.ItemsSource = source;
            comboBox.SelectedIndex = 0;

            await Upgrade(1202);
        }

        public async Task Upgrade(int version)
        {
            currentVersion = version;
            await Task.Delay(0);
            id = "";
            if (version < 116)
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (ID.SelectedItem is not null)
                        id = EffectTable.Select("id='" + (ID.SelectedItem as IconComboBoxItem).ComboBoxItemId + "'").First()["number"].ToString();
                });
            else
                id = "\"minecraft:" + id + "\"";
        }
    }
}