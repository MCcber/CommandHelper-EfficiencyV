using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace cbhk_environment.CustomControls
{
    public class RichToolTip:ToolTip
    {
        public ImageSource ContentIcon
        {
            get { return (ImageSource)GetValue(ContentIconProperty); }
            set { SetValue(ContentIconProperty, value); }
        }

        public static readonly DependencyProperty ContentIconProperty =
            DependencyProperty.Register("ContentIcon", typeof(ImageSource), typeof(RichToolTip), new PropertyMetadata(default(ImageSource)));

        public string DisplayID
        {
            get { return (string)GetValue(DisplayIDProperty); }
            set { SetValue(DisplayIDProperty, value); }
        }

        public static readonly DependencyProperty DisplayIDProperty =
            DependencyProperty.Register("DisplayID", typeof(string), typeof(RichToolTip), new PropertyMetadata(default(string)));

        public string CustomName
        {
            get { return (string)GetValue(CustomNameProperty); }
            set { SetValue(CustomNameProperty, value); }
        }

        public static readonly DependencyProperty CustomNameProperty =
            DependencyProperty.Register("CustomName", typeof(string), typeof(RichToolTip), new PropertyMetadata(default(string)));

        public FlowDocument Description
        {
            get { return (FlowDocument)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(FlowDocument), typeof(RichToolTip), new PropertyMetadata(default(FlowDocument)));
    }
}
