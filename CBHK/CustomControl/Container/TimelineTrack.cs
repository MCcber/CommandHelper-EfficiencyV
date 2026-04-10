using CBHK.Interface.Visual;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.Container
{
    public class TimelineTrack : INotifyPropertyChanged
    {
        #region Property
        public bool IsStable { get; set; }

        private Visibility editorBoxVisibility = Visibility.Hidden;
        public Visibility EditorBoxVisibility
        {
            get => editorBoxVisibility;
            set
            {
                editorBoxVisibility = value;
                OnPropertyChanged();
            }
        }

        private Thickness padding;
        public Thickness Padding
        {
            get => padding;
            set
            {
                padding = value;
                OnPropertyChanged();
            }
        }

        private Brush borderBrush;
        public Brush BorderBrush
        {
            get => borderBrush;
            set
            {
                borderBrush = value;
                OnPropertyChanged();
            }
        }

        public string trackName = "";
        public string TrackName
        {
            get => trackName;
            set
            {
                trackName = value;
                OnPropertyChanged();
            }
        }

        public Brush foreground = Brushes.Transparent;
        public Brush Foreground
        {
            get => foreground;
            set
            {
                foreground = value;
                OnPropertyChanged();
            }
        }

        private Brush background = Brushes.Transparent;
        public Brush Background 
        {
            get => background; 
            set
            {
                background = value;
                OnPropertyChanged();
            }
        }

        private Brush trackHeadBackground = Brushes.Transparent;
        public Brush TrackHeadBackground
        {
            get => trackHeadBackground;
            set
            {
                trackHeadBackground = value;
                OnPropertyChanged();
            }
        }

        private double height;
        public double Height
        {
            get => height;
            set
            {
                height = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<ITimelineElement> timelineElementList = [];
        public ObservableCollection<ITimelineElement> TimelineElementList 
        { 
            get => timelineElementList;
            set
            {
                timelineElementList = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion

        #region Method
        public TimelineTrack()
        {
            EditorBoxVisibility = Visibility.Hidden;
        }
        #endregion

        #region Event
        public void EditorBox_Click(object sender,RoutedEventArgs e)
        {
            EditorBoxVisibility = Visibility.Visible;
        }

        public void EditorBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if(sender is TextBox textBox)
            {
                textBox.CaretIndex = textBox.Text.Length;
            }
        }

        public void EditorBox_LostFocus(object sender, RoutedEventArgs e)
        {
            EditorBoxVisibility = Visibility.Hidden;
        }

        public void EditorBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key is System.Windows.Input.Key.Enter)
            {
                EditorBoxVisibility = Visibility.Hidden;
            }
        }

        public TimelineTrack Clone()
        {
            TimelineTrack result = new();
            return result;
        }
        #endregion
    }
}