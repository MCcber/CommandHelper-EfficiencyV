using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System;
using System.Text.RegularExpressions;
using CBHK.CustomControl;

namespace CBHK.ControlDataContext
{
    public class ComboBoxSearchDataContext
    {
        public Popup pop = new();

        ComboBox current_box;
        public void ItemSearcher(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox box) return;
            if (box.Text.Trim().Length == 0)
            {
                pop.IsOpen = false;
                return;
            }

            if(box.TemplatedParent is not null)
            {
                current_box = box.TemplatedParent as ComboBox;
                current_box.IsDropDownOpen = false;

                #region 打开下拉框
                ObservableCollection<IconComboBoxItem> dataGroup = current_box.ItemsSource as ObservableCollection<IconComboBoxItem>;
                string itemName = Regex.Match(box.Text, @"(?<=name:)\w+").ToString();
                string itemId = Regex.Match(box.Text, @"(?<=oldID:)[a-zA-Z0-9_\-]+").ToString();
                bool HaveIDAndName = itemName.Length > 0 && itemId.Length > 0;
                IEnumerable<IconComboBoxItem> targetDataGroups = [];

                if (HaveIDAndName)
                {
                    targetDataGroups = dataGroup.Where(item => (item.ComboBoxItemId == itemId && item.ComboBoxItemText == itemName) || (item.ComboBoxItemId.StartsWith(itemId) && item.ComboBoxItemText.StartsWith(itemName)));
                }
                else
                if (itemId.Length > 0)
                {
                    targetDataGroups = dataGroup.Where(item => item.ComboBoxItemId == itemId || item.ComboBoxItemId.StartsWith(itemId));
                }
                else
                {
                    if (itemName.Length == 0)
                        itemName = box.Text;
                    targetDataGroups = dataGroup.Where(item => item.ComboBoxItemText == itemName || item.ComboBoxItemText.StartsWith(itemName));
                }

                if (targetDataGroups.Any() && box.Text.Trim().Length > 0)
                {
                    pop = CreatePop(pop, targetDataGroups, current_box, current_box.ItemTemplate);
                    pop.IsOpen = true;
                }
                #endregion
            }
        }

        /// <summary>
        /// 展开搜索视图
        /// </summary>
        /// <param name="pop"></param>
        /// <param name="listSource"></param>
        /// <param name="element"></param>
        /// <param name="display_template"></param>
        /// <returns></returns>
        public Popup CreatePop(Popup pop, IEnumerable<IconComboBoxItem> listSource, FrameworkElement element, DataTemplate display_template)
        {
            Border border = new()
            {
                Background = new SolidColorBrush(Colors.Transparent),
                BorderBrush = new SolidColorBrush(Colors.Black),
                BorderThickness = new Thickness(0)
            };

            ScrollViewer viewer = new()
            {
                Background = new SolidColorBrush(Colors.Transparent),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            ListBox listbox = new()
            {
                Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/CBHK;component/Resource/Common/Image/Frame.png"))),
                Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                MinWidth = 200,
                MaxHeight = 250,
                ItemsSource = listSource,
                ItemTemplate = display_template
            };
            RenderOptions.SetBitmapScalingMode(listbox,BitmapScalingMode.NearestNeighbor);

            VirtualizingPanel.SetIsVirtualizing(listbox, true);
            VirtualizingPanel.SetVirtualizationMode(listbox, VirtualizationMode.Recycling);
            ScrollViewer.SetVerticalScrollBarVisibility(listbox, ScrollBarVisibility.Disabled);
            ScrollViewer.SetHorizontalScrollBarVisibility(listbox, ScrollBarVisibility.Disabled);

            viewer.Content = listbox;
            listbox.MouseDoubleClick += Listbox_MouseDoubleClick;
            border.Child = viewer;
            pop.Child = border;
            pop.Placement = PlacementMode.Bottom;
            pop.PlacementTarget = element;

            return pop;
        }

        /// <summary>
        /// 更新已选中成员并更新显示文本
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Listbox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListBox box = sender as ListBox;
            if (box.SelectedItem is null) return;
            IconComboBoxItem selected_item = box.SelectedItem as IconComboBoxItem;
            current_box.SelectedItem = selected_item;
            pop.IsOpen = false;
        }
    }
}