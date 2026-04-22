using CBHK.Model.Constant;
using CBHK.Utility.Visual;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace CBHK.CustomControl.VectorButton
{
    public partial class TagBlock:Button
    {
        #region Field
        private Brush OriginBackground;
        #endregion

        #region Property
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(TagBlock), new PropertyMetadata(default(string)));

        public IRelayCommand Delete
        {
            get { return (IRelayCommand)GetValue(DeleteProperty); }
            set { SetValue(DeleteProperty, value); }
        }

        public static readonly DependencyProperty DeleteProperty =
            DependencyProperty.Register("Delete", typeof(IRelayCommand), typeof(TagBlock), new PropertyMetadata(default(IRelayCommand)));

        public Brush ThemeBackground
        {
            get { return (Brush)GetValue(ThemeBackgroundProperty); }
            set { SetValue(ThemeBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeBackgroundProperty =
            DependencyProperty.Register("ThemeBackground", typeof(Brush), typeof(TagBlock), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public TagBlock()
        {
            SetResourceReference(ThemeBackgroundProperty, Theme.CommonBackground);
            Loaded += TagBlock_Loaded;
            Delete = new RelayCommand(DeleteButton);
        }

        public void UpdateBorderColorByBackgroundColor()
        {
            if (ThemeBackground is SolidColorBrush themeBrush)
            {
                Background = OriginBackground = new SolidColorBrush(ColorTool.Lighten(themeBrush.Color, 0.2f));
            }
        }
        #endregion

        #region Event
        private void TagBlock_Loaded(object sender, RoutedEventArgs e)
        {
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

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);
            if(OriginBackground is SolidColorBrush solidColorBrush)
            {
                Background = new SolidColorBrush(ColorTool.Darken(solidColorBrush.Color, 0.2f));
            }
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            if (OriginBackground is SolidColorBrush solidColorBrush)
            {
                Background = new SolidColorBrush(ColorTool.Lighten(solidColorBrush.Color, 0.2f));
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if(OriginBackground is SolidColorBrush solidColorBrush)
            {
                Background = new SolidColorBrush(solidColorBrush.Color);
            }
        }

        public void DeleteButton()
        {
            InlineUIContainer parentContainer = Parent as InlineUIContainer;
            Paragraph paragraph = parentContainer.Parent as Paragraph;
            paragraph.Inlines.Remove(parentContainer);
        }
        #endregion
    }
}