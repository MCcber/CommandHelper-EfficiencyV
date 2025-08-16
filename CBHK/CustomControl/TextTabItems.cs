using CBHK.GeneralTool;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CBHK.CustomControl
{
    public class TextTabItems:TabItem
    {
        public Brush LeftBorderTexture
        {
            get { return (Brush)GetValue(LeftBorderTextureProperty); }
            set { SetValue(LeftBorderTextureProperty, value); }
        }

        public static readonly DependencyProperty LeftBorderTextureProperty =
            DependencyProperty.Register("LeftBorderTexture", typeof(Brush), typeof(TextTabItems), new PropertyMetadata(null));

        public Brush RightBorderTexture
        {
            get { return (Brush)GetValue(RightBorderTextureProperty); }
            set { SetValue(RightBorderTextureProperty, value); }
        }

        public static readonly DependencyProperty RightBorderTextureProperty =
            DependencyProperty.Register("RightBorderTexture", typeof(Brush), typeof(TextTabItems), new PropertyMetadata(null));

        public Brush TopBorderTexture
        {
            get { return (Brush)GetValue(TopBorderTextureProperty); }
            set { SetValue(TopBorderTextureProperty, value); }
        }

        public static readonly DependencyProperty TopBorderTextureProperty =
            DependencyProperty.Register("TopBorderTexture", typeof(Brush), typeof(TextTabItems), new PropertyMetadata(null));

        public Brush BottomBorderTexture
        {
            get { return (Brush)GetValue(BottomBorderTextureProperty); }
            set { SetValue(BottomBorderTextureProperty, value); }
        }

        public static readonly DependencyProperty BottomBorderTextureProperty =
            DependencyProperty.Register("BottomBorderTexture", typeof(Brush), typeof(TextTabItems), new PropertyMetadata(null));

        public Brush SelectedLeftBorderTexture
        {
            get { return (Brush)GetValue(SelectedLeftBorderTextureProperty); }
            set { SetValue(SelectedLeftBorderTextureProperty, value); }
        }

        public static readonly DependencyProperty SelectedLeftBorderTextureProperty =
            DependencyProperty.Register("SelectedLeftBorderTexture", typeof(Brush), typeof(TextTabItems), new PropertyMetadata(null));

        public Brush SelectedRightBorderTexture
        {
            get { return (Brush)GetValue(SelectedRightBorderTextureProperty); }
            set { SetValue(SelectedRightBorderTextureProperty, value); }
        }

        public static readonly DependencyProperty SelectedRightBorderTextureProperty =
            DependencyProperty.Register("SelectedRightBorderTexture", typeof(Brush), typeof(TextTabItems), new PropertyMetadata(null));

        public Brush SelectedTopBorderTexture
        {
            get { return (Brush)GetValue(SelectedTopBorderTextureProperty); }
            set { SetValue(SelectedTopBorderTextureProperty, value); }
        }

        public static readonly DependencyProperty SelectedTopBorderTextureProperty =
            DependencyProperty.Register("SelectedTopBorderTexture", typeof(Brush), typeof(TextTabItems), new PropertyMetadata(null));

        public Brush SelectedBottomBorderTexture
        {
            get { return (Brush)GetValue(SelectedBottomBorderTextureProperty); }
            set { SetValue(SelectedBottomBorderTextureProperty, value); }
        }

        public static readonly DependencyProperty SelectedBottomBorderTextureProperty =
            DependencyProperty.Register("SelectedBottomBorderTexture", typeof(Brush), typeof(TextTabItems), new PropertyMetadata(null));

        public Brush SelectedBackground
        {
            get { return (Brush)GetValue(SelectedBackgroundProperty); }
            set { SetValue(SelectedBackgroundProperty, value); }
        }

        public static readonly DependencyProperty SelectedBackgroundProperty =
            DependencyProperty.Register("SelectedBackground", typeof(Brush), typeof(TextTabItems), new PropertyMetadata(null));

        static int current_index = -1;
        static int select_index = -1;
        static TextTabItems current_item = null;
        static TextTabItems select_item = null;
        static bool Draging = false;
        /// <summary>
        /// 对应某个树视图的节点，用于快速得知当前标签页对应的项目文件是否被包括
        /// </summary>
        public RichTreeViewItems mappingItem = null;
        /// <summary>
        /// 对应某个树视图的节点的父级，用于快速得知当前标签页对应的项目文件是否被包括
        /// </summary>
        public RichTreeViewItems mappingParentItem = null;

        public TextTabItems()
        {
            PreviewMouseLeftButtonDown += TabItem_PreviewMouseLeftButtonDown;
            MouseLeftButtonUp += TabItem_MouseLeftButtonUp;
            MouseEnter += TabItem_MouseEnter;
        }

        public void CloseRichTabItemsClick(object sender, RoutedEventArgs e)
        {
            TextTabItems item = (sender as Button).TemplatedParent as TextTabItems;
            TabControl parent = item.Parent as TabControl;
            parent.Items.Remove(item);
        }

        #region 处理拖拽互换位置
        private void TabItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Draging = true;
            current_item = sender as TextTabItems;
            TabControl current_parent = current_item.Parent as TabControl;
            current_index = current_parent.Items.IndexOf(current_item);
        }

        private void TabItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (select_item != null && current_item != null)
            {
                SolidColorBrush selectedItemBackground = select_item.Background as SolidColorBrush;
                SolidColorBrush currentItemBackground = current_item.Background as SolidColorBrush;
                SolidColorBrush selectedItemForeground = select_item.Foreground as SolidColorBrush;
                SolidColorBrush currentItemForeground = current_item.Foreground as SolidColorBrush;
                Style currentItemStyle = current_item.Style;
                Style selectedItemStyle = select_item.Style;
                string selectedItemHeaderText = select_item.Header.ToString();
                string currentItemHeaderText = current_item.Header.ToString();
                TabControl selectItemParent = select_item.FindParent<TabControl>();
                TabControl currentItemParent = current_item.FindParent<TabControl>();
                if (!Equals(selectItemParent, currentItemParent)) return;
                if (select_index != current_index && select_index != -1 && current_index != -1 && current_index >=0)
                {
                    TabControl current_parent = (sender as TextTabItems).Parent as TabControl;
                    if (select_item != null && current_item != null && current_index != -1 && select_index != -1)
                    {
                        TextTabItems new_select_item = new()
                        {
                            BorderThickness = select_item.BorderThickness,
                            Header = selectedItemHeaderText,
                            Content = select_item.Content,
                            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
                            SelectedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC6B23")),
                            LeftBorderTexture = Application.Current.Resources["TabItemLeft"] as ImageBrush,
                            RightBorderTexture = Application.Current.Resources["TabItemRight"] as ImageBrush,
                            TopBorderTexture = Application.Current.Resources["TabItemTop"] as ImageBrush,
                            SelectedLeftBorderTexture = Application.Current.Resources["SelectedTabItemLeft"] as ImageBrush,
                            SelectedRightBorderTexture = Application.Current.Resources["SelectedTabItemRight"] as ImageBrush,
                            SelectedTopBorderTexture = Application.Current.Resources["SelectedTabItemTop"] as ImageBrush,
                            Foreground = selectedItemForeground,
                            Style = selectedItemStyle
                        };
                        TextTabItems new_current_item = new()
                        {
                            BorderThickness = current_item.BorderThickness,
                            Header = currentItemHeaderText,
                            Content = current_item.Content,
                            Foreground = currentItemForeground,
                            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48382C")),
                            SelectedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CC6B23")),
                            LeftBorderTexture = Application.Current.Resources["TabItemLeft"] as ImageBrush,
                            RightBorderTexture = Application.Current.Resources["TabItemRight"] as ImageBrush,
                            TopBorderTexture = Application.Current.Resources["TabItemTop"] as ImageBrush,
                            SelectedLeftBorderTexture = Application.Current.Resources["SelectedTabItemLeft"] as ImageBrush,
                            SelectedRightBorderTexture = Application.Current.Resources["SelectedTabItemRight"] as ImageBrush,
                            SelectedTopBorderTexture = Application.Current.Resources["SelectedTabItemTop"] as ImageBrush,
                            Style = currentItemStyle
                        };
                        new_select_item.MouseLeftButtonUp += TabItem_MouseLeftButtonUp;
                        new_current_item.MouseLeftButtonUp += TabItem_MouseLeftButtonUp;
                        new_select_item.PreviewMouseLeftButtonDown += TabItem_PreviewMouseLeftButtonDown;
                        new_current_item.PreviewMouseLeftButtonDown += TabItem_PreviewMouseLeftButtonDown;
                        new_select_item.MouseEnter += TabItem_MouseEnter;
                        new_current_item.MouseEnter += TabItem_MouseEnter;

                        current_parent.Items.RemoveAt(select_index);
                        current_parent.Items.Insert(select_index, new_current_item);
                        current_parent.Items.RemoveAt(current_index);
                        current_parent.Items.Insert(current_index, new_select_item);
                        current_parent.SelectedIndex = select_index;
                    }
                }
                Draging = false;
                current_index = -1;
                select_index = -1;
                current_item = null;
                select_item = null;
            }
        }

        private void TabItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (Draging)
            {
                TextTabItems current_item = sender as TextTabItems;
                select_item = current_item;
                TabControl current_parent = current_item.Parent as TabControl;
                select_index = current_parent.Items.IndexOf(current_item);
            }

            Grid grid = Template.FindName("templateRoot", this) as Grid;
            if (grid.ToolTip is null && (File.Exists(Uid) || Directory.Exists(Uid)))
            {
                grid.ToolTip = Uid;
                ToolTipService.SetInitialShowDelay(grid, 0);
                ToolTipService.SetShowDuration(grid, 1500);
            }
        }
        #endregion
    }
}
