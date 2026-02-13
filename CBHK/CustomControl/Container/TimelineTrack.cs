using System.Windows;
using System.Windows.Media;

namespace CBHK.CustomControl.Container
{
    public class TimelineTrack
    {
        #region Field
        #endregion

        #region Property
        public string TrackName { get; set; }
        public Brush Foreground { get; set; }
        public Thickness Margin { get; set; }
        #endregion

        #region Method
        public TimelineTrack()
        {

        } 
        #endregion
    }
}
