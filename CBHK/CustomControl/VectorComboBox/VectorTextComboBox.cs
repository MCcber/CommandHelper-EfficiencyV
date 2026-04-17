using CBHK.Model.Constant;
using CBHK.Utility.Common;
using CBHK.Utility.Data;
using CBHK.Utility.Visual;
using System;
using System.Collections;
using System.Collections.ObjectModel;
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
        public ObservableCollection<VectorTextComboBoxItem> DataList
        {
            get { return (ObservableCollection<VectorTextComboBoxItem>)GetValue(DataListProperty); }
            set { SetValue(DataListProperty, value); }
        }

        public static readonly DependencyProperty DataListProperty =
            DependencyProperty.Register("DataList", typeof(ObservableCollection<VectorTextComboBoxItem>), typeof(VectorTextComboBox), new PropertyMetadata(default(ObservableCollection<VectorTextComboBoxItem>), OnDataListChanged));

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
            itemView.Source = DataList;
            ItemsSource = itemView.View;
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
            if (e.Item is VectorTextComboBoxItem vectorTextComboBoxItem)
            {
                bool result = StringTool.IsMatchSearchText(vectorTextComboBoxItem.Text, SearchText);
                e.Accepted = result;
            }
        }

        private static void OnDataListChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is VectorTextComboBox vectorTextComboBox)
            {
                vectorTextComboBox.OnDataList_Changed(e.NewValue as ObservableCollection<VectorTextComboBoxItem>);
            }
        }

        private void OnDataList_Changed(ObservableCollection<VectorTextComboBoxItem> newValue)
        {
            if (itemView.Source != newValue)
            {
                itemView.Source = newValue;
                ItemsSource = itemView.View;
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
            if(e.Key is Key.Enter && itemView is not null)
            {
                e.Handled = true;
                IsDropDownOpen = true;
                itemView.View?.Refresh();
            }
        }
        #endregion
    }
}