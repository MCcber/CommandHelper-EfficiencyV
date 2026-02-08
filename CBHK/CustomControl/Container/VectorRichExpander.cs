using System.Windows;
using System.Windows.Controls;

namespace CBHK.CustomControl.Container
{
    public class VectorRichExpander : Expander
    {
        public object HeaderContent
        {
            get { return GetValue(HeaderContentProperty); }
            set { SetValue(HeaderContentProperty, value); }
        }

        public static readonly DependencyProperty HeaderContentProperty =
            DependencyProperty.Register("HeaderContent", typeof(object), typeof(VectorRichExpander), new PropertyMetadata(default(object)));

        public DataTemplate HeaderContentTemplate
        {
            get { return (DataTemplate)GetValue(HeaderContentTemplateProperty); }
            set { SetValue(HeaderContentTemplateProperty, value); }
        }

        public static readonly DependencyProperty HeaderContentTemplateProperty =
            DependencyProperty.Register("HeaderContentTemplate", typeof(DataTemplate), typeof(VectorRichExpander), new PropertyMetadata(default(DataTemplate)));
    }
}