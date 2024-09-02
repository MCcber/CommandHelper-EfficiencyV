using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace cbhk.ControlsDataContexts
{
    public class TextComboBoxSearchDataContext
    {
        public Popup pop = new();

        ComboBox current_box;
        public void ItemSearcher(object sender, KeyEventArgs e)
        {
            TextBox box = sender as TextBox;
            if (box is null) return;
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
                ObservableCollection<string> dataGroup = current_box.ItemsSource as ObservableCollection<string>;
                if(dataGroup is null || box is null)
                {
                    return;
                }
                var target_data_groups = dataGroup.Where(item => item.StartsWith(box.Text.Trim()));
                if (target_data_groups.Count() > 1 && box.Text.Trim().Length > 0)
                {
                    pop = CreatePop(pop, target_data_groups, current_box, current_box.ItemTemplate);
                    pop.IsOpen = true;
                }
                #endregion

                #region 搜索目标成员
                IEnumerable<string> item_source = current_box.ItemsSource as IEnumerable<string>;
                IEnumerable<string> select_item = item_source.Where(item => item.StartsWith(box.Text.Trim()));
                #endregion
            }
        }

        public Popup CreatePop(Popup pop, IEnumerable<string> listSource, FrameworkElement element, DataTemplate display_template)
        {
            Border border = new()
            {
                BorderBrush = new SolidColorBrush(Colors.Black),
                BorderThickness = new Thickness(0)
            };

            ScrollViewer viewer = new()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            ListBox listbox = new()
            {
                Background = null,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                MinWidth = 200,
                MaxHeight = 250,
                ItemsSource = listSource,
                ItemTemplate = display_template
            };

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
            string selected_item = box.SelectedItem.ToString();
            current_box.SelectedItem = selected_item;
            pop.IsOpen = false;
        }
    }
}
