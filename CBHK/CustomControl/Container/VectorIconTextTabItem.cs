using CBHK.Model.Constant;
using CBHK.Utility.Visual;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.Container
{
    public class VectorIconTextTabItem : TabItem
    {
        #region Field
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

        public Brush ThemeBackground
        {
            get { return (Brush)GetValue(ThemeBackgroundProperty); }
            set { SetValue(ThemeBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeBackgroundProperty =
            DependencyProperty.Register("ThemeBackground", typeof(Brush), typeof(VectorIconTextTabItem), new PropertyMetadata(default(Brush)));

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
            SetResourceReference(ThemeBackgroundProperty, Theme.CommonBackground);
            SetResourceReference(ForegroundProperty, Theme.CommonForeground);
            Loaded += VectorIconTextTabItem_Loaded;
            Unloaded += VectorIconTextTabItem_Unloaded;
            MouseEnter += VectorIconTextTabItem_MouseEnter;
            MouseLeave += VectorIconTextTabItem_MouseLeave;
        }

        public void UpdateBorderColorByBackgroundColor()
        {
            if(ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(themeBrush.Color);
                SelectedMarkerBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.6f));
                LeftTopBorderBrush = OriginLeftTopBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.4f));
                RightBottomBorderBrush = OriginRightBottomBrush = new SolidColorBrush(ColorTool.Darken(themeBrush.Color, 0.4f));
            }
        }

        private bool IsElementInTabItem(DependencyObject element, TabItem tab)
        {
            if (element == null || tab == null) return false;
            if (ItemsControl.ItemsControlFromItemContainer(tab) is TabControl tabControl)
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

            if (string.IsNullOrEmpty(HeaderText))
            {
                HeaderText = "Button";
            }

            Parent_SelectionChanged(sender, null);

            UpdateBorderColorByBackgroundColor();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if(e.Property == ThemeBackgroundProperty)
            {
                UpdateBorderColorByBackgroundColor();
            }
        }

        private void Parent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsSelected)
            {
                if(OriginLeftTopBrush is SolidColorBrush originLeftTopBrush)
                {
                    RightBottomBorderBrush = new SolidColorBrush(originLeftTopBrush.Color);
                }
                if(OriginRightBottomBrush is SolidColorBrush originRightBottomBrush)
                {
                    Background = new SolidColorBrush(originRightBottomBrush.Color);
                    LeftTopBorderBrush = new SolidColorBrush(originRightBottomBrush.Color);
                }
                SelectedMarkerVisibility = Visibility.Visible;
            }
            else
            {
                SelectedMarkerVisibility = Visibility.Hidden;
                LeftTopBorderBrush = OriginLeftTopBrush;
                RightBottomBorderBrush = OriginRightBottomBrush;
                Background = ThemeBackground;
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

            if (ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.3f));
            }
        }

        private void VectorIconTextTabItem_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!IsSelected && ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = new SolidColorBrush(themeBrush.Color);
            }
            else
            if(OriginRightBottomBrush is SolidColorBrush originRightBottomBrush)
            {
                SolidColorBrush rightBottomBrush = new(originRightBottomBrush.Color);
                Background = rightBottomBrush;
            }
        }
        #endregion
    }
}