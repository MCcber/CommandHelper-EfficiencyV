using CBHK.Model.Constant;
using CBHK.Utility.Visual;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CBHK.CustomControl.Container
{
    public class VectorListViewItemContainer : ListViewItem
    {
        #region Field
        private SolidColorBrush OriginalContainerBackground;
        #endregion

        #region Property
        public Color ItemContainerMouseOverColor { get; set; }

        public Color ItemContainerMouseDownColor { get; set; }

        public Brush ItemSelectedMarkerBrush
        {
            get { return (Brush)GetValue(ItemSelectedMarkerBrushProperty); }
            set { SetValue(ItemSelectedMarkerBrushProperty, value); }
        }

        public static readonly DependencyProperty ItemSelectedMarkerBrushProperty =
            DependencyProperty.Register("ItemSelectedMarkerBrush", typeof(Brush), typeof(VectorListViewItemContainer), new PropertyMetadata(default(Brush)));

        public Brush ItemContainerBackground
        {
            get { return (Brush)GetValue(ItemContainerBackgroundProperty); }
            set { SetValue(ItemContainerBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ItemContainerBackgroundProperty =
            DependencyProperty.Register("ItemContainerBackground", typeof(Brush), typeof(VectorListViewItemContainer), new PropertyMetadata(default(Brush)));

        public Brush ThemeItemContainerBackground
        {
            get { return (Brush)GetValue(ThemeItemContainerBackgroundProperty); }
            set { SetValue(ThemeItemContainerBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeItemContainerBackgroundProperty =
            DependencyProperty.Register("ThemeItemContainerBackground", typeof(Brush), typeof(VectorListViewItemContainer), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public VectorListViewItemContainer()
        {
            Loaded += VectorListViewItemContainer_Loaded;
        }

        public void UpdateBorderColorByBackgroundColor()
        {
            if (ThemeItemContainerBackground is SolidColorBrush ThemeItemContainerBackgroundBrush)
            {
                ItemContainerBackground = new SolidColorBrush(ThemeItemContainerBackgroundBrush.Color);
                ItemContainerMouseOverColor = ColorTool.Darken(ThemeItemContainerBackgroundBrush.Color, 0.2f);
                ItemContainerMouseDownColor = ColorTool.Darken(ThemeItemContainerBackgroundBrush.Color, 0.4f);
            }
        }

        private void AnimateBackground(Color sourceColor, Color targetColor)
        {
            var animation = new ColorAnimation
            {
                From = sourceColor,
                To = targetColor,
                Duration = TimeSpan.FromSeconds(0.2),
                FillBehavior = FillBehavior.HoldEnd
            };
            ItemContainerBackground.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }
        #endregion

        #region Event
        private void VectorListViewItemContainer_Loaded(object sender, RoutedEventArgs e)
        {
            SetResourceReference(ThemeItemContainerBackgroundProperty, Theme.CommonBackground);
            SetResourceReference(ItemSelectedMarkerBrushProperty, Theme.CommonForeground);

            if (ThemeItemContainerBackground is SolidColorBrush solidColorBrush)
            {
                ItemContainerBackground = new SolidColorBrush(solidColorBrush.Color);
                OriginalContainerBackground = new SolidColorBrush(solidColorBrush.Color);
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == ThemeItemContainerBackgroundProperty)
            {
                UpdateBorderColorByBackgroundColor();
            }
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);

            AnimateBackground(ItemContainerMouseOverColor, ItemContainerMouseDownColor);
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);

            if (ItemContainerBackground is SolidColorBrush solidColorBrush)
            {
                AnimateBackground(solidColorBrush.Color, ItemContainerMouseOverColor);
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            if (OriginalContainerBackground is SolidColorBrush solidColorBrush)
            {
                AnimateBackground(ItemContainerMouseOverColor, solidColorBrush.Color);
            }
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);

            if (ItemContainerBackground is SolidColorBrush solidColorBrush)
            {
                AnimateBackground(ItemContainerMouseDownColor, solidColorBrush.Color);
            }
        }
        #endregion
    }
}
