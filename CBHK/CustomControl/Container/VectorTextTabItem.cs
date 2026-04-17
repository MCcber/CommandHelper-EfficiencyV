using CBHK.Model.Constant;
using CBHK.Utility.Visual;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.Container
{
    public class VectorTextTabItem:TabItem
    {
        #region Property
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(VectorTextTabItem), new PropertyMetadata(default(string)));

        public Brush ThemeBackground
        {
            get { return (Brush)GetValue(ThemeBackgroundProperty); }
            set { SetValue(ThemeBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeBackgroundProperty =
            DependencyProperty.Register("ThemeBackground", typeof(Brush), typeof(VectorTextTabItem), new PropertyMetadata(default(Brush)));

        public Brush SideBorderBrush
        {
            get { return (Brush)GetValue(SideBorderBrushProperty); }
            set { SetValue(SideBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty SideBorderBrushProperty =
            DependencyProperty.Register("SideBorderBrush", typeof(Brush), typeof(VectorTextTabItem), new PropertyMetadata(default(Brush)));

        public Brush TopBorderBrush
        {
            get { return (Brush)GetValue(TopBorderBrushProperty); }
            set { SetValue(TopBorderBrushProperty, value); }
        }

        public static readonly DependencyProperty TopBorderBrushProperty =
            DependencyProperty.Register("TopBorderBrush", typeof(Brush), typeof(VectorTextTabItem), new PropertyMetadata(default(Brush)));

        public Brush UnSelectedBackground
        {
            get { return (Brush)GetValue(UnSelectedBackgroundProperty); }
            set { SetValue(UnSelectedBackgroundProperty, value); }
        }

        public static readonly DependencyProperty UnSelectedBackgroundProperty =
            DependencyProperty.Register("UnSelectedBackground", typeof(Brush), typeof(VectorTextTabItem), new PropertyMetadata(default(Brush)));

        public Brush SelectedBackground
        {
            get { return (Brush)GetValue(SelectedBackgroundProperty); }
            set { SetValue(SelectedBackgroundProperty, value); }
        }

        public static readonly DependencyProperty SelectedBackgroundProperty =
            DependencyProperty.Register("SelectedBackground", typeof(Brush), typeof(VectorTextTabItem), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public VectorTextTabItem()
        {
            SetResourceReference(ForegroundProperty, Theme.CommonForeground);
            SetResourceReference(ThemeBackgroundProperty, Theme.CommonBackground);
            Loaded += VectorTextTabItem_Loaded;
        }

        public void UpdateBorderColorByBackgroundColor()
        {
            if (ThemeBackground is SolidColorBrush themeBrush)
            {
                UnSelectedBackground = new SolidColorBrush(ColorTool.Darken(themeBrush.Color, 0.2f));
                SelectedBackground = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.2f));
                UpdateSelectedColor();
            }
        }

        private void UpdateSelectedColor()
        {
            SolidColorBrush targetBrush;
            if (IsSelected)
            {
                targetBrush = SelectedBackground as SolidColorBrush;
            }
            else
            {
                targetBrush = UnSelectedBackground as SolidColorBrush;
            }
            Color sideAndTopColor = ColorTool.Lighten(targetBrush.Color, 0.2f);
            Background = new SolidColorBrush(targetBrush.Color);
            SideBorderBrush = new SolidColorBrush(sideAndTopColor);
            TopBorderBrush = new SolidColorBrush(sideAndTopColor);
        }
        #endregion

        #region Event
        private void VectorTextTabItem_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Title))
            {
                Title = "Title";
            }

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

        protected override void OnSelected(RoutedEventArgs e)
        {
            base.OnSelected(e);
            if (IsLoaded)
            {
                UpdateSelectedColor();
            }
        }

        protected override void OnUnselected(RoutedEventArgs e)
        {
            base.OnUnselected(e);
            if (IsLoaded)
            {
                UpdateSelectedColor();
            }
        }
        #endregion
    }
}