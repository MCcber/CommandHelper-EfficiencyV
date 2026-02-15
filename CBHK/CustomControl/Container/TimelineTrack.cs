using CBHK.CustomControl.Input;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace CBHK.CustomControl.Container
{
    public class TimelineTrack
    {
        #region Property
        public string TrackName { get; set; }
        public Brush Foreground { get; set; }
        public Brush Background { get; set; }
        public Thickness Margin { get; set; }

        public ObservableCollection<TimelineClip> TimelineClipList { get; set; }
        #endregion
    }
}