using CBHK.CustomControl.Container;
using System;

namespace CBHK.Interface.Visual
{
    public interface ITimelineElement
    {
        #region Property
        public bool IsPlayed { get; set; }
        public bool IsChecked { get; set; }
        public TimeRulerElement Ruler { get; set; }
        public double OriginCanvasTop { get; set; }
        public Timeline ParentTimeline { get; set; }
        public TimeSpan StartTime { get; set; }
        public object ParentPanel { get; set; }
        public string Title { get; set; }
        #endregion
    }
}
