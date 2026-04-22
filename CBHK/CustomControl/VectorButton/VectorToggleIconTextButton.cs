using CBHK.Utility.Visual;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorToggleIconTextButton : BaseVectorToggleButton
    {
        #region Field
        private int OriginBottomHeight;
        private Thickness OriginThumbMargin;
        private Brush OriginLeftTopBorderBrush;
        private Brush OriginRightBottomBorderBrush;
        #endregion

        #region Property

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(string)));

        #region Thumb
        public Thickness ThumbMargin
        {
            get { return (Thickness)GetValue(ThumbMarginProperty); }
            set { SetValue(ThumbMarginProperty, value); }
        }

        public static readonly DependencyProperty ThumbMarginProperty =
            DependencyProperty.Register("ThumbMargin", typeof(Thickness), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Thickness)));

        public Brush ThumbBackground
        {
            get { return (Brush)GetValue(ThumbBackgroundProperty); }
            set { SetValue(ThumbBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThumbBackgroundProperty =
            DependencyProperty.Register("ThumbBackground", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));

        public double ThumbWidth
        {
            get { return (double)GetValue(ThumbWidthProperty); }
            set { SetValue(ThumbWidthProperty, value); }
        }

        public static readonly DependencyProperty ThumbWidthProperty =
            DependencyProperty.Register("ThumbWidth", typeof(double), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(double)));

        public double ThumbHeight
        {
            get { return (double)GetValue(ThumbHeightProperty); }
            set { SetValue(ThumbHeightProperty, value); }
        }

        public static readonly DependencyProperty ThumbHeightProperty =
            DependencyProperty.Register("ThumbHeight", typeof(double), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(double)));

        public Brush BottomBorderBrush
        {
            get { return (Brush)GetValue(BottomBorderBrushProperty); }
            set { SetValue(BottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty BottomBorderBrushProperty =
            DependencyProperty.Register("BottomBorderBrush", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));

        public Brush LeftTopBorderBrush
        {
            get { return (Brush)GetValue(LeftTopBorderBrushProperty); }
            set { SetValue(LeftTopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty LeftTopBorderBrushProperty =
            DependencyProperty.Register("LeftTopBorderBrush", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));
        public Brush RightBottomBorderBrush
        {
            get { return (Brush)GetValue(RightBottomBorderBrushProperty); }
            set { SetValue(RightBottomBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RightBottomBorderBrushProperty =
            DependencyProperty.Register("RightBottomBorderBrush", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));
        public Brush BorderCornerBrush
        {
            get { return (Brush)GetValue(BorderCornerBrushProperty); }
            set { SetValue(BorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty BorderCornerBrushProperty =
            DependencyProperty.Register("BorderCornerBrush", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));
        #endregion

        #region Background
        public Brush UnSelectedBackground
        {
            get { return (Brush)GetValue(UnSelectedBackgroundProperty); }
            set { SetValue(UnSelectedBackgroundProperty, value); }
        }

        public static readonly DependencyProperty UnSelectedBackgroundProperty =
            DependencyProperty.Register("UnSelectedBackground", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));

        public Brush SelectedBackground
        {
            get { return (Brush)GetValue(SelectedBackgroundProperty); }
            set { SetValue(SelectedBackgroundProperty, value); }
        }

        public static readonly DependencyProperty SelectedBackgroundProperty =
            DependencyProperty.Register("SelectedBackground", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));

        public double BackgroundWidth
        {
            get { return (double)GetValue(BackgroundWidthProperty); }
            set { SetValue(BackgroundWidthProperty, value); }
        }

        public static readonly DependencyProperty BackgroundWidthProperty =
            DependencyProperty.Register("BackgroundWidth", typeof(double), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(double)));

        public double BackgroundHeight
        {
            get { return (double)GetValue(BackgroundHeightProperty); }
            set { SetValue(BackgroundHeightProperty, value); }
        }

        public static readonly DependencyProperty BackgroundHeightProperty =
            DependencyProperty.Register("BackgroundHeight", typeof(double), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(double)));

        public Brush LeftTopSelectedBorderBrush
        {
            get { return (Brush)GetValue(LeftTopSelectedBorderBrushProperty); }
            set { SetValue(LeftTopSelectedBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty LeftTopSelectedBorderBrushProperty =
            DependencyProperty.Register("LeftTopSelectedBorderBrush", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));

        public Brush RightTopUnSelectedBorderBrush
        {
            get { return (Brush)GetValue(RightTopUnSelectedBorderBrushProperty); }
            set { SetValue(RightTopUnSelectedBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RightTopUnSelectedBorderBrushProperty =
            DependencyProperty.Register("RightTopUnSelectedBorderBrush", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));

        public Brush LeftSideBottomSelectedBorderBrush
        {
            get { return (Brush)GetValue(LeftSideBottomSelectedBorderBrushProperty); }
            set { SetValue(LeftSideBottomSelectedBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty LeftSideBottomSelectedBorderBrushProperty =
            DependencyProperty.Register("LeftSideBottomSelectedBorderBrush", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));

        public Brush RightSideBottomUnSelectedBorderBrush
        {
            get { return (Brush)GetValue(RightSideBottomUnSelectedBorderBrushProperty); }
            set { SetValue(RightSideBottomUnSelectedBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty RightSideBottomUnSelectedBorderBrushProperty =
            DependencyProperty.Register("RightSideBottomUnSelectedBorderBrush", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));

        public Brush RightSideUnSelectedBorderCornerBrush
        {
            get { return (Brush)GetValue(RightSideUnSelectedBorderCornerBrushProperty); }
            set { SetValue(RightSideUnSelectedBorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty RightSideUnSelectedBorderCornerBrushProperty =
            DependencyProperty.Register("RightSideUnSelectedBorderCornerBrush", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));

        public Brush LeftSideSelectedBorderCornerBrush
        {
            get { return (Brush)GetValue(LeftSideSelectedBorderCornerBrushProperty); }
            set { SetValue(LeftSideSelectedBorderCornerBrushProperty, value); }
        }

        public static readonly DependencyProperty LeftSideSelectedBorderCornerBrushProperty =
            DependencyProperty.Register("LeftSideSelectedBorderCornerBrush", typeof(Brush), typeof(VectorToggleIconTextButton), new PropertyMetadata(default(Brush)));
        #endregion

        #endregion

        #region Method
        public VectorToggleIconTextButton()
        {
            UnSelectedBackground = new BrushConverter().ConvertFromString("#8C8D90") as SolidColorBrush;
            Background = ThumbBackground = new BrushConverter().ConvertFromString("#F4F6F9") as SolidColorBrush;
            Loaded += VectorToggleIconTextButton_Loaded;
            PreviewMouseLeftButtonUp += VectorToggleIconTextButton_PreviewMouseLeftButtonUp;
            PreviewMouseLeftButtonDown += VectorToggleIconTextButton_PreviewMouseLeftButtonDown;
            MouseLeave += VectorToggleIconTextButton_MouseLeave;
            MouseEnter += VectorToggleIconTextButton_MouseEnter;
        }

        public override void UpdateBorderColorByBackgroundColor()
        {
            //base.UpdateBorderColorByBackgroundColor();

            if(ThemeBackground is SolidColorBrush themeBrush)
            {
                SelectedBackground = new SolidColorBrush(themeBrush.Color);
                LeftSideSelectedBorderCornerBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.3f));
                LeftSideBottomSelectedBorderBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.2f));
                LeftTopSelectedBorderBrush = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.2f));
            }

            if (ThumbBackground is SolidColorBrush thumbBrush)
            {
                BorderCornerBrush = new SolidColorBrush(ColorTool.Lighten(thumbBrush.Color, 0.4f));
                LeftTopBorderBrush = OriginLeftTopBorderBrush = new SolidColorBrush(ColorTool.Lighten(thumbBrush.Color, 0.2f));
                RightBottomBorderBrush = OriginRightBottomBorderBrush = new SolidColorBrush(ColorTool.Lighten(thumbBrush.Color, 0.3f));
                BottomBorderBrush = new SolidColorBrush(ColorTool.Darken(thumbBrush.Color, 0.6f));
            }

            if(UnSelectedBackground is SolidColorBrush unSelectedBackgroundBrush)
            {
                RightTopUnSelectedBorderBrush = new SolidColorBrush(ColorTool.Lighten(unSelectedBackgroundBrush.Color, 0.2f));
                RightSideBottomUnSelectedBorderBrush = new SolidColorBrush(ColorTool.Lighten(unSelectedBackgroundBrush.Color, 0.2f));
                RightSideUnSelectedBorderCornerBrush = new SolidColorBrush(ColorTool.Lighten(unSelectedBackgroundBrush.Color, 0.3f));
            }
        }
        #endregion

        #region Event
        private void VectorToggleIconTextButton_Loaded(object sender, EventArgs e)
        {
            Text ??= "";

            if (OriginBottomHeight == 0)
            {
                OriginBottomHeight = 4;
            }

            if(BackgroundWidth == 0)
            {
                BackgroundWidth = 70;
            }

            if (BackgroundHeight == 0)
            {
                BackgroundHeight = 35;
            }

            if(ThumbWidth == 0)
            {
                ThumbWidth = BackgroundWidth / 2 + 2;
            }

            if(ThumbHeight == 0)
            {
                ThumbHeight = BackgroundHeight + 5;
            }

            OriginThumbMargin = ThumbMargin = new(2, -5, 0, 0);

            if (GetTemplateChild("extraBottomLine") is RowDefinition rowDefinition)
            {
                rowDefinition.Height = new GridLength(OriginBottomHeight);
            }

            UpdateBorderColorByBackgroundColor();
        }

        private void VectorToggleIconTextButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            VectorToggleIconTextButton_MouseEnter(sender, null);
        }

        private void VectorToggleIconTextButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (Background is SolidColorBrush solidColorBrush)
            {
                ThumbBackground = new SolidColorBrush(ColorTool.Darken(solidColorBrush.Color, 0.4f));
            }
        }

        private void VectorToggleIconTextButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ThumbBackground = Background;
            LeftTopBorderBrush = OriginLeftTopBorderBrush;
            RightBottomBorderBrush = OriginRightBottomBorderBrush;
        }

        private void VectorToggleIconTextButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (Background is SolidColorBrush backgroundBrush)
            {
                ThumbBackground = new SolidColorBrush(ColorTool.Darken(backgroundBrush.Color, 0.2f));
            }
            if (OriginLeftTopBorderBrush is SolidColorBrush leftTopBorderBrush)
            {
                LeftTopBorderBrush = new SolidColorBrush(ColorTool.Lighten(leftTopBorderBrush.Color, 0.3f));
            }
            if (OriginRightBottomBorderBrush is SolidColorBrush rightBottomBorderBrush)
            {
                RightBottomBorderBrush = new SolidColorBrush(ColorTool.Lighten(rightBottomBorderBrush.Color, 0.4f));
            }
        }

        protected override void OnChecked(RoutedEventArgs e)
        {
            base.OnChecked(e);
            ThumbMargin = new(BackgroundWidth / 2, -5, 0, 0);
        }

        protected override void OnUnchecked(RoutedEventArgs e)
        {
            base.OnUnchecked(e);
            ThumbMargin = OriginThumbMargin;
        }
        #endregion
    }
}