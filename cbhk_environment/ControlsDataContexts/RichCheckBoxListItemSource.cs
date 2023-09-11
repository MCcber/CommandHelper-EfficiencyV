using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace cbhk_environment.ControlsDataContexts
{
    public class RichCheckBoxListItemSource: ObservableObject
    {
        public ObservableCollection<CheckBoxListItemDataGroup> ItemDataSource { get; set; }

        public RichCheckBoxListItemSource()
        {

        }
    }

    public class CheckBoxListItemDataGroup : ObservableObject
    {
        public bool color_visibility;
        public bool ColorVisibility
        {
            get { return color_visibility; }
            set
            {
                color_visibility = value;
                OnPropertyChanged();
            }
        }

        public Brush content_color;
        public Brush ContentColor
        {
            get { return content_color; }
            set
            {
                content_color = value;
                OnPropertyChanged();
            }
        }

        public BitmapImage content_image;
        public BitmapImage ContentImage
        {
            get { return content_image; }
            set
            {
                content_image = value;
                OnPropertyChanged();
            }
        }

        public Brush foreground;
        public Brush Foreground
        {
            get { return foreground; }
            set
            {
                foreground = value;
                OnPropertyChanged();
            }
        }

        public double header_height;
        public double HeaderHeight
        {
            get { return header_height; }
            set
            {
                header_height = value;
                OnPropertyChanged();
            }
        }

        public string header_text;
        public string HeaderText
        {
            get { return header_text; }
            set
            {
                header_text = value;
                OnPropertyChanged();
            }
        }

        public double header_width;
        public double HeaderWidth
        {
            get { return header_width; }
            set
            {
                header_width = value;
                OnPropertyChanged();
            }
        }

        public bool image_visibility;
        public bool ImageVisibility
        {
            get { return image_visibility; }
            set
            {
                image_visibility = value;
                OnPropertyChanged();
            }
        }

        public bool text_visibility;
        public bool TextVisibility
        {
            get { return text_visibility; }
            set
            {
                text_visibility = value;
                OnPropertyChanged();
            }
        }
    }
}
