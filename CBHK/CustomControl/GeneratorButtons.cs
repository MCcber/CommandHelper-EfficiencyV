using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl
{
    public class GeneratorButtons:System.Windows.Controls.Button
    {
        public ImageSource Icon

        {
            get { return (ImageSource)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(ImageSource), typeof(GeneratorButtons), new PropertyMetadata(default(ImageSource)));

        public ImageSource HoverBorder
        {
            get { return (ImageSource)GetValue(HoverBorderProperty); }
            set { SetValue(HoverBorderProperty, value); }
        }

        public static readonly DependencyProperty HoverBorderProperty =
            DependencyProperty.Register("HoverBorder", typeof(ImageSource), typeof(GeneratorButtons), new PropertyMetadata(default(ImageSource)));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(GeneratorButtons), new PropertyMetadata(default(string)));

        public string SubTitle
        {
            get { return (string)GetValue(SubTitleProperty); }
            set { SetValue(SubTitleProperty, value); }
        }

        public static readonly DependencyProperty SubTitleProperty =
            DependencyProperty.Register("SubTitle", typeof(string), typeof(GeneratorButtons), new PropertyMetadata(default(string)));
    }
}
