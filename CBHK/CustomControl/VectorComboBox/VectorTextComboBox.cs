using CBHK.Model.Constant;
using CBHK.Utility.Common;
using CBHK.Utility.Data;
using CBHK.Utility.Visual;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorComboBox
{
    public partial class VectorTextComboBox : ComboBox
    {
        #region Field
        private CollectionViewSource itemView = new();
        #endregion

        #region Property
        public string SearchText
        {
            get { return (string)GetValue(SearchTextProperty); }
            set { SetValue(SearchTextProperty, value); }
        }

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register("SearchText", typeof(string), typeof(VectorTextComboBox), new PropertyMetadata(default(string)));

        public Visibility SearchBoxVisibility
        {
            get { return (Visibility)GetValue(SearchBoxVisibilityProperty); }
            set { SetValue(SearchBoxVisibilityProperty, value); }
        }

        public static readonly DependencyProperty SearchBoxVisibilityProperty =
            DependencyProperty.Register("SearchBoxVisibility", typeof(Visibility), typeof(VectorTextComboBox), new PropertyMetadata(default(Visibility)));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(VectorTextComboBox), new PropertyMetadata(default(string)));

        public Thickness ArrowMargin
        {
            get { return (Thickness)GetValue(ArrowMarginProperty); }
            set { SetValue(ArrowMarginProperty, value); }
        }

        public static readonly DependencyProperty ArrowMarginProperty =
            DependencyProperty.Register("ArrowMargin", typeof(Thickness), typeof(VectorTextComboBox), new PropertyMetadata(default(Thickness)));

        public Brush ThemeTitleBorderBackground
        {
            get { return (Brush)GetValue(ThemeTitleBorderBackgroundProperty); }
            set { SetValue(ThemeTitleBorderBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeTitleBorderBackgroundProperty =
            DependencyProperty.Register("ThemeTitleBorderBackground", typeof(Brush), typeof(VectorTextComboBox), new PropertyMetadata(default(Brush)));

        public Brush TitleBorderBackground
        {
            get { return (Brush)GetValue(TitleBorderBackgroundProperty); }
            set { SetValue(TitleBorderBackgroundProperty, value); }
        }

        public static readonly DependencyProperty TitleBorderBackgroundProperty =
            DependencyProperty.Register("TitleBorderBackground", typeof(Brush), typeof(VectorTextComboBox), new PropertyMetadata(default(Brush)));

        public Brush TitleLeftTopBorderBrush
        {
            get { return (Brush)GetValue(TitleLeftTopBorderBrushProperty); }
            set { SetValue(TitleLeftTopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty TitleLeftTopBorderBrushProperty =
            DependencyProperty.Register("TitleLeftTopBorderBrush", typeof(Brush), typeof(VectorTextComboBox), new PropertyMetadata(default(Brush)));

        public Brush TitleRightBottomBorderBrush
        {
            get { return (Brush)GetValue(TitleRightBottomBorderBrushProperty); }
            set { SetValue(TitleRightBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty TitleRightBottomBorderBrushProperty =
            DependencyProperty.Register("TitleRightBottomBorderBrush", typeof(Brush), typeof(VectorTextComboBox), new PropertyMetadata(default(Brush)));

        public Brush TitleBorderCornerBrush
        {
            get { return (Brush)GetValue(TitleBorderCornerBrushProperty); }
            set { SetValue(TitleBorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty TitleBorderCornerBrushProperty =
            DependencyProperty.Register("TitleBorderCornerBrush", typeof(Brush), typeof(VectorTextComboBox), new PropertyMetadata(default(Brush)));

        public Brush ArrowBrush
        {
            get { return (Brush)GetValue(ArrowBrushProperty); }
            set { SetValue(ArrowBrushProperty, value); }
        }

        public static readonly DependencyProperty ArrowBrushProperty =
            DependencyProperty.Register("ArrowBrush", typeof(Brush), typeof(VectorTextComboBox), new PropertyMetadata(default(Brush)));

        public Brush TitleBrush
        {
            get { return (Brush)GetValue(TitleBrushProperty); }
            set { SetValue(TitleBrushProperty, value); }
        }

        public static readonly DependencyProperty TitleBrushProperty =
            DependencyProperty.Register("TitleBrush", typeof(Brush), typeof(VectorTextComboBox), new PropertyMetadata(default(Brush)));

        public Brush SearchBoxForeground
        {
            get { return (Brush)GetValue(SearchBoxForegroundProperty); }
            set { SetValue(SearchBoxForegroundProperty, value); }
        }

        public static readonly DependencyProperty SearchBoxForegroundProperty =
            DependencyProperty.Register("SearchBoxForeground", typeof(Brush), typeof(VectorTextComboBox), new PropertyMetadata(default(Brush)));

        public Brush PopupItemPanelBackground
        {
            get { return (Brush)GetValue(PopupItemPanelBackgroundProperty); }
            set { SetValue(PopupItemPanelBackgroundProperty, value); }
        }

        public static readonly DependencyProperty PopupItemPanelBackgroundProperty =
            DependencyProperty.Register("PopupItemPanelBackground", typeof(Brush), typeof(VectorTextComboBox), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public VectorTextComboBox()
        {
            BorderBrush = Brushes.Black;
            Loaded += VectorTextComboBox_Loaded;
            DropDownClosed += VectorTextComboBox_DropDownClosed;
            itemView.Filter += ItemView_Filter;
        }

        private void UpdateBorderColorByBackgroundColor()
        {
            if (ThemeTitleBorderBackground is SolidColorBrush solidColorBrush)
            {
                BorderBrush = new SolidColorBrush(ColorTool.Darken(solidColorBrush.Color,0.6f));
                TitleBorderBackground = new SolidColorBrush(solidColorBrush.Color);
                TitleLeftTopBorderBrush = new SolidColorBrush(ColorTool.Lighten(solidColorBrush.Color, 0.2f));
                TitleRightBottomBorderBrush = new SolidColorBrush(ColorTool.Lighten(solidColorBrush.Color, 0.2f));
                TitleBorderCornerBrush = new SolidColorBrush(ColorTool.Lighten(solidColorBrush.Color, 0.4f));
            }
        }
        #endregion

        #region Event
        private void VectorTextComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            SetResourceReference(ThemeTitleBorderBackgroundProperty, Theme.CommonBackground);
            SetResourceReference(TitleBrushProperty,Theme.CommonForeground);
            SetResourceReference(ArrowBrushProperty, Theme.CommonForeground);
            SetResourceReference(SearchBoxForegroundProperty,Theme.CommonForeground);
            SetResourceReference(PopupItemPanelBackgroundProperty,Theme.CommonBackground);

            UpdateBorderColorByBackgroundColor();

            // Loaded 可能多次触发（控件重新挂载/换父级时），第二次 ItemsSource 已经是 itemView.View，
            // 直接跳过避免重复包装（重复包装会让 ItemsSource 变成视图再赋给 Source 而抛异常，也会破坏选择）。
            if (ReferenceEquals(ItemsSource, itemView.View))
            {
                return;
            }

            // 将 ItemsSource 包装进 itemView，以便搜索框按 SearchText 过滤
            if (ItemsSource is not null)
            {
                itemView.Source = ItemsSource;
                ItemsSource = itemView.View;
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if(e.Property == ThemeTitleBorderBackgroundProperty)
            {
                UpdateBorderColorByBackgroundColor();
            }
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new VectorComboBoxItemContainer();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is VectorComboBoxItemContainer;
        }

        private void ItemView_Filter(object sender, FilterEventArgs e)
        {
            string text = e.Item switch
            {
                VectorTextComboBoxItem v => v.Text,
                string s => s,
                _ => e.Item?.ToString() ?? ""
            };
            e.Accepted = StringTool.IsMatchSearchText(text, SearchText);
        }

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            // 当外部 XAML 绑定设置 ItemsSource 时，将过滤器挂到 WPF 自动创建的 CollectionView 上
            if (newValue is not null && newValue != itemView.View)
            {
                var defaultView = CollectionViewSource.GetDefaultView(newValue);
                if (defaultView is not null)
                {
                    defaultView.Filter = item =>
                    {
                        string text = item switch
                        {
                            VectorTextComboBoxItem v => v.Text,
                            string s => s,
                            _ => item?.ToString() ?? ""
                        };
                        return StringTool.IsMatchSearchText(text, SearchText);
                    };
                }
            }
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            int count = 0;
            if (ItemsSource is IList list)
            {
                count = list.Count;
            }
            else 
            if (ItemsSource is ICollection col)
            {
                count = col.Count;
            }
            else
            if(ItemsSource is ListCollectionView listCollectionView && listCollectionView.SourceCollection is ICollection subCollection)
            {
                count = subCollection.Count;
            }

            if (count > 10)
            {
                SearchBoxVisibility = Visibility.Visible;
            }
            else
            {
                SearchBoxVisibility = Visibility.Collapsed;
            }
        }

        private void VectorTextComboBox_DropDownClosed(object sender, System.EventArgs e)
        {
            // 查找模板中的按钮
            var toggleButton = FindSomeThingByType.FindVisualChildByName<ToggleButton>(this, "toggleButton");
            if (toggleButton != null)
            {
                // 创建并触发MouseLeave事件
                var args = new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount)
                {
                    RoutedEvent = MouseLeaveEvent
                };
                toggleButton.RaiseEvent(args);
            }
        }

        public void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key is Key.Enter)
            {
                e.Handled = true;
                IsDropDownOpen = true;
                // 刷新当前生效的视图
                if (ItemsSource == itemView.View)
                    itemView.View?.Refresh();
                else if (ItemsSource is not null)
                {
                    CollectionViewSource.GetDefaultView(ItemsSource)?.Refresh();
                }
            }
        }
        #endregion
    }
}