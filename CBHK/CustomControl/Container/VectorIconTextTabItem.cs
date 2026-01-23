using CBHK.Utility.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.Container
{
    public class VectorIconTextTabItem : TabItem
    {
        #region Field
        private Brush OriginBackground;
        private Brush OriginForeground;
        private Brush OriginLeftTopBrush;
        private Brush OriginRightBottomBrush;
        #endregion

        #region Property
        public ImageSource HeaderImage
        {
            get { return (ImageSource)GetValue(HeaderImageProperty); }
            set { SetValue(HeaderImageProperty, value); }
        }

        public static readonly DependencyProperty HeaderImageProperty =
            DependencyProperty.Register("HeaderImage", typeof(ImageSource), typeof(VectorIconTextTabItem), new PropertyMetadata(default(ImageSource)));

        public string HeaderText
        {
            get { return (string)GetValue(HeaderTextProperty); }
            set { SetValue(HeaderTextProperty, value); }
        }

        public static readonly DependencyProperty HeaderTextProperty =
            DependencyProperty.Register("HeaderText", typeof(string), typeof(VectorIconTextTabItem), new PropertyMetadata(default(string)));

        public Visibility SelectedMarkerVisibility
        {
            get { return (Visibility)GetValue(SelectedMarkerVisibilityProperty); }
            set { SetValue(SelectedMarkerVisibilityProperty, value); }
        }

        public static readonly DependencyProperty SelectedMarkerVisibilityProperty =
            DependencyProperty.Register("SelectedMarkerVisibility", typeof(Visibility), typeof(VectorIconTextTabItem), new PropertyMetadata(default(Visibility)));

        public Brush SelectedMarkerBrush
        {
            get { return (Brush)GetValue(SelectedMarkerBrushProperty); }
            set { SetValue(SelectedMarkerBrushProperty, value); }
        }

        public static readonly DependencyProperty SelectedMarkerBrushProperty =
            DependencyProperty.Register("SelectedMarkerBrush", typeof(Brush), typeof(VectorIconTextTabItem), new PropertyMetadata(default(Brush)));

        public Brush LeftTopBorderBrush
        {
            get { return (Brush)GetValue(LeftTopBorderBrushProperty); }
            set { SetValue(LeftTopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty LeftTopBorderBrushProperty =
            DependencyProperty.Register("LeftTopBorderBrush", typeof(Brush), typeof(VectorIconTextTabItem), new PropertyMetadata(default(Brush)));

        public Brush RightBottomBorderBrush
        {
            get { return (Brush)GetValue(RightBottomBorderBrushProperty); }
            set { SetValue(RightBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RightBottomBorderBrushProperty =
            DependencyProperty.Register("RightBottomBorderBrush", typeof(Brush), typeof(VectorIconTextTabItem), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public VectorIconTextTabItem()
        {
            Loaded += VectorIconTextTabItem_Loaded;
            Unloaded += VectorIconTextTabItem_Unloaded;
            MouseEnter += VectorIconTextTabItem_MouseEnter;
            MouseLeave += VectorIconTextTabItem_MouseLeave;
        }

        private bool IsElementInTabItem(DependencyObject element, TabItem tab)
        {
            if (element == null || tab == null) return false;
            var tabControl = ItemsControl.ItemsControlFromItemContainer(tab) as TabControl;
            if (tabControl != null)
            {
                var container = ItemsControl.ContainerFromElement(tabControl, element) as TabItem;
                if (container == tab) return true;
            }
            return false;
        }
        #endregion

        #region Event
        private void VectorIconTextTabItem_Unloaded(object sender, RoutedEventArgs e)
        {
            var parent = ItemsControl.ItemsControlFromItemContainer(this) as TabControl;
            if (parent is not null)
            {
                parent.SelectionChanged -= Parent_SelectionChanged;
            }
        }

        private void VectorIconTextTabItem_Loaded(object sender, RoutedEventArgs e)
        {
            var parent = ItemsControl.ItemsControlFromItemContainer(this) as TabControl;
            if (parent is not null)
            {
                parent.SelectionChanged += Parent_SelectionChanged;
            }

            if (HeaderText == "" || HeaderText is null)
            {
                HeaderText = "Button";
            }

            var foregroundSource = DependencyPropertyHelper.GetValueSource(this, ForegroundProperty);
            if (foregroundSource.BaseValueSource is BaseValueSource.DefaultStyle || foregroundSource.BaseValueSource is BaseValueSource.Style)
            {
                Foreground = Brushes.White;
            }
            var backgroundSource = DependencyPropertyHelper.GetValueSource(this, BackgroundProperty);
            if (backgroundSource.BaseValueSource is BaseValueSource.DefaultStyle || foregroundSource.BaseValueSource is BaseValueSource.Style)
            {
                Background = new BrushConverter().ConvertFromString("#3c8527") as Brush;
            }
            var selectedMarkerSource = DependencyPropertyHelper.GetValueSource(this, BackgroundProperty);
            if (selectedMarkerSource.BaseValueSource is BaseValueSource.DefaultStyle || selectedMarkerSource.BaseValueSource is BaseValueSource.Style)
            {
                SelectedMarkerBrush = Brushes.White;
            }
            var borderBrushSource = DependencyPropertyHelper.GetValueSource(this, BorderBrushProperty);
            if (borderBrushSource.BaseValueSource is BaseValueSource.DefaultStyle || borderBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                BorderBrush = Brushes.Black;
            }
            var leftTopBorderBrushSource = DependencyPropertyHelper.GetValueSource(this, LeftTopBorderBrushProperty);
            if (leftTopBorderBrushSource.BaseValueSource is BaseValueSource.Default || leftTopBorderBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                SolidColorBrush solidBorderBrush = Background as SolidColorBrush;
                Color color = ColorTool.Lighten(solidBorderBrush.Color, 0.4f);
                LeftTopBorderBrush = new SolidColorBrush(color);
            }
            var rightBottomBrushSource = DependencyPropertyHelper.GetValueSource(this, RightBottomBorderBrushProperty);
            if (rightBottomBrushSource.BaseValueSource is BaseValueSource.Default || rightBottomBrushSource.BaseValueSource is BaseValueSource.Style)
            {
                Color color = ColorTool.Darken((Background as SolidColorBrush).Color, 0.4f);
                RightBottomBorderBrush ??= new SolidColorBrush(color);
            }

            OriginLeftTopBrush = LeftTopBorderBrush;
            OriginRightBottomBrush = RightBottomBorderBrush;
            OriginBackground = Background;
            OriginForeground = Foreground;

            Parent_SelectionChanged(sender, null);
        }

        private void Parent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsSelected)
            {
                SolidColorBrush leftTopBrush = new((OriginLeftTopBrush as SolidColorBrush).Color);
                SolidColorBrush rightBottomBrush = new((OriginRightBottomBrush as SolidColorBrush).Color);
                Background = rightBottomBrush;
                LeftTopBorderBrush = rightBottomBrush;
                RightBottomBorderBrush = leftTopBrush;
                SelectedMarkerVisibility = Visibility.Visible;
            }
            else
            {
                SelectedMarkerVisibility = Visibility.Hidden;
                LeftTopBorderBrush = OriginLeftTopBrush;
                RightBottomBorderBrush = OriginRightBottomBrush;
                Background = OriginBackground;
            }
        }

        private void VectorIconTextTabItem_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var tab = (TabItem)sender;
            var directly = System.Windows.Input.Mouse.DirectlyOver as DependencyObject;
            bool isElementInTabItem = IsElementInTabItem(directly, tab);
            if (!isElementInTabItem)
            {
                VectorIconTextTabItem_MouseLeave(null, null);
                return;
            }
            Color color = ColorTool.Lighten((OriginBackground as SolidColorBrush).Color,0.3f);
            Background = new SolidColorBrush(color);
        }

        private void VectorIconTextTabItem_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!IsSelected)
            {
                Background = OriginBackground;
            }
            else
            {
                SolidColorBrush rightBottomBrush = new((OriginRightBottomBrush as SolidColorBrush).Color);
                Background = rightBottomBrush;
            }
        }
        #endregion
    }
}