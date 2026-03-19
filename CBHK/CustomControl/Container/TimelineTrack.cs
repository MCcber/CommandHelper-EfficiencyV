using CBHK.Interface;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace CBHK.CustomControl.Container
{
    public class TimelineTrack : INotifyPropertyChanged
    {
        #region Property
        public bool IsStable { get; set; }

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


        private double headHeight = 0;
        public double HeadHeight
        {
            get => headHeight;
            set
            {
                headHeight = value;
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

        #region Event
        public TimelineTrack Clone()
        {
            TimelineTrack result = new();
            return result;
        }
        #endregion
    }
}