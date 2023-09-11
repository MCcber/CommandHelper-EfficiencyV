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

namespace cbhk_environment.ControlsDataContexts
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

            if(box.TemplatedParent != null)
            {
                current_box = box.TemplatedParent as ComboBox;
                current_box.IsDropDownOpen = false;

                #region 打开下拉框
                ObservableCollection<IconComboBoxItem> dataGroup = current_box.ItemsSource as ObservableCollection<IconComboBoxItem>;
                string[] searchList = box.Text.Split(' ');
                bool HaveIDAndName = false;
                if(searchList.Length > 1)
                if (Regex.IsMatch(searchList[0], @"[a-zA-Z0-9_]+") && Regex.IsMatch(searchList[1], @"[\u4e00-\u9fa5]+"))
                    HaveIDAndName = true;

                var target_data_groups = dataGroup.Where(item => (HaveIDAndName && item.ComboBoxItemId.StartsWith(searchList[0]) && item.ComboBoxItemText.StartsWith(searchList[1])) || item.ComboBoxItemId.StartsWith(box.Text.Trim()) || item.ComboBoxItemText.StartsWith(box.Text.Trim()) || item.ComboBoxItemId.Contains(box.Text.Trim()) || item.ComboBoxItemText.Contains(box.Text.Trim()));
                if (target_data_groups.Count() > 1 && box.Text.Trim().Length > 0)
                {
                    pop = CreatePop(pop, target_data_groups, current_box, current_box.ItemTemplate);
                    pop.IsOpen = true;
                }
                else
                    if(target_data_groups.Count() == 1 && box.Text.Trim().Length > 0)
                {
                    current_box.SelectedItem = target_data_groups.First();
                    pop.IsOpen = false;
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
                Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/cbhk_environment;component/resources/common/images/Frame.png"))),
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
            if (box.SelectedItem == null) return;
            IconComboBoxItem selected_item = box.SelectedItem as IconComboBoxItem;
            current_box.SelectedItem = selected_item;
            pop.IsOpen = false;
        }
    }

    public class IconComboBoxItem
    {
        public ImageSource ComboBoxItemIcon { get; set; } = new BitmapImage();
        public string ComboBoxItemText { get; set; } = "";
        public string ComboBoxItemId { get; set; } = "";
    }
}
