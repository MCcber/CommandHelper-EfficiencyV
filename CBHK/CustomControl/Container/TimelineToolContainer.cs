using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;
using System.Windows.Controls;

namespace CBHK.CustomControl.Container
{
    public class TimelineToolContainer : Control
    {
        #region Field
        private Timeline timeline;
        private TimeRulerElement timeRulerElement;
        #endregion

        #region Property
        public double ZoomFactor
        {
            get { return (double)GetValue(ZoomFactorProperty); }
            set { SetValue(ZoomFactorProperty, value); }
        }

        public static readonly DependencyProperty ZoomFactorProperty =
            DependencyProperty.Register("ZoomFactor", typeof(double), typeof(TimelineToolContainer), new PropertyMetadata(default(double), OnZoomFactor_ValueChanged));

        public IRelayCommand MouseToolCommand
        {
            get { return (IRelayCommand)GetValue(MouseToolCommandProperty); }
            set { SetValue(MouseToolCommandProperty, value); }
        }

        public static readonly DependencyProperty MouseToolCommandProperty =
            DependencyProperty.Register("MouseToolCommand", typeof(IRelayCommand), typeof(TimelineToolContainer), new PropertyMetadata(default(IRelayCommand)));

        public IRelayCommand AddKeyFramesCommand
        {
            get { return (IRelayCommand)GetValue(AddKeyFramesCommandProperty); }
            set { SetValue(AddKeyFramesCommandProperty, value); }
        }

        public static readonly DependencyProperty AddKeyFramesCommandProperty =
            DependencyProperty.Register("AddKeyFramesCommand", typeof(IRelayCommand), typeof(TimelineToolContainer), new PropertyMetadata(default(IRelayCommand)));

        public IRelayCommand DeleteKeyFramesCommand
        {
            get { return (IRelayCommand)GetValue(DeleteKeyFramesCommandProperty); }
            set { SetValue(DeleteKeyFramesCommandProperty, value); }
        }

        public static readonly DependencyProperty DeleteKeyFramesCommandProperty =
            DependencyProperty.Register("DeleteKeyFramesCommand", typeof(IRelayCommand), typeof(TimelineToolContainer), new PropertyMetadata(default(IRelayCommand)));

        public IRelayCommand CopyCommand
        {
            get { return (IRelayCommand)GetValue(CopyCommandProperty); }
            set { SetValue(CopyCommandProperty, value); }
        }

        public static readonly DependencyProperty CopyCommandProperty =
            DependencyProperty.Register("CopyCommand", typeof(IRelayCommand), typeof(TimelineToolContainer), new PropertyMetadata(default(IRelayCommand)));

        public IRelayCommand SplitCommand
        {
            get { return (IRelayCommand)GetValue(SplitCommandProperty); }
            set { SetValue(SplitCommandProperty, value); }
        }

        public static readonly DependencyProperty SplitCommandProperty =
            DependencyProperty.Register("SplitCommand", typeof(IRelayCommand), typeof(TimelineToolContainer), new PropertyMetadata(default(IRelayCommand)));

        public IRelayCommand CuttingCommand
        {
            get { return (IRelayCommand)GetValue(CuttingCommandProperty); }
            set { SetValue(CuttingCommandProperty, value); }
        }

        public static readonly DependencyProperty CuttingCommandProperty =
            DependencyProperty.Register("CuttingCommand", typeof(IRelayCommand), typeof(TimelineToolContainer), new PropertyMetadata(default(IRelayCommand)));

        public IRelayCommand HorizontalFlipCommand
        {
            get { return (IRelayCommand)GetValue(HorizontalFlipCommandProperty); }
            set { SetValue(HorizontalFlipCommandProperty, value); }
        }

        public static readonly DependencyProperty HorizontalFlipCommandProperty =
            DependencyProperty.Register("HorizontalFlipCommand", typeof(IRelayCommand), typeof(TimelineToolContainer), new PropertyMetadata(default(IRelayCommand)));

        public IRelayCommand ReverseCommand
        {
            get { return (IRelayCommand)GetValue(ReverseCommandProperty); }
            set { SetValue(ReverseCommandProperty, value); }
        }

        public static readonly DependencyProperty ReverseCommandProperty =
            DependencyProperty.Register("ReverseCommand", typeof(IRelayCommand), typeof(TimelineToolContainer), new PropertyMetadata(default(IRelayCommand)));

        public IRelayCommand AddTrackCommand
        {
            get { return (IRelayCommand)GetValue(AddTrackCommandProperty); }
            set { SetValue(AddTrackCommandProperty, value); }
        }

        public static readonly DependencyProperty AddTrackCommandProperty =
            DependencyProperty.Register("AddTrackCommand", typeof(IRelayCommand), typeof(TimelineToolContainer), new PropertyMetadata(default(IRelayCommand)));

        public IRelayCommand DeleteTrackCommand
        {
            get { return (IRelayCommand)GetValue(DeleteTrackCommandProperty); }
            set { SetValue(DeleteTrackCommandProperty, value); }
        }

        public static readonly DependencyProperty DeleteTrackCommandProperty =
            DependencyProperty.Register("DeleteTrackCommand", typeof(IRelayCommand), typeof(TimelineToolContainer), new PropertyMetadata(default(IRelayCommand)));

        public IRelayCommand PlayCommand
        {
            get { return (IRelayCommand)GetValue(PlayCommandProperty); }
            set { SetValue(PlayCommandProperty, value); }
        }

        public static readonly DependencyProperty PlayCommandProperty =
            DependencyProperty.Register("PlayCommand", typeof(IRelayCommand), typeof(TimelineToolContainer), new PropertyMetadata(default(IRelayCommand)));

        public string CurrentTime
        {
            get { return (string)GetValue(CurrentTimeProperty); }
            set { SetValue(CurrentTimeProperty, value); }
        }

        public static readonly DependencyProperty CurrentTimeProperty =
            DependencyProperty.Register("CurrentTime", typeof(string), typeof(TimelineToolContainer), new PropertyMetadata(default(string)));

        public string TotalTime
        {
            get { return (string)GetValue(TotalTimeProperty); }
            set { SetValue(TotalTimeProperty, value); }
        }

        public static readonly DependencyProperty TotalTimeProperty =
            DependencyProperty.Register("TotalTime", typeof(string), typeof(TimelineToolContainer), new PropertyMetadata(default(string)));

        public bool IsShowPreviewLine
        {
            get { return (bool)GetValue(IsShowPreviewLineProperty); }
            set { SetValue(IsShowPreviewLineProperty, value); }
        }

        public static readonly DependencyProperty IsShowPreviewLineProperty =
            DependencyProperty.Register("IsShowPreviewLine", typeof(bool), typeof(TimelineToolContainer), new PropertyMetadata(default(bool), OnIsShowPreviewLine_ValueChanged));

        public IRelayCommand ShrinkCommand
        {
            get { return (IRelayCommand)GetValue(ShrinkCommandProperty); }
            set { SetValue(ShrinkCommandProperty, value); }
        }

        public static readonly DependencyProperty ShrinkCommandProperty =
            DependencyProperty.Register("ShrinkCommand", typeof(IRelayCommand), typeof(TimelineToolContainer), new PropertyMetadata(default(IRelayCommand)));

        public IRelayCommand EnlargeCommand
        {
            get { return (IRelayCommand)GetValue(EnlargeCommandProperty); }
            set { SetValue(EnlargeCommandProperty, value); }
        }

        public static readonly DependencyProperty EnlargeCommandProperty =
            DependencyProperty.Register("EnlargeCommand", typeof(IRelayCommand), typeof(TimelineToolContainer), new PropertyMetadata(default(IRelayCommand)));

        public IRelayCommand HideTimelineCommand
        {
            get { return (IRelayCommand)GetValue(HideTimelineCommandProperty); }
            set { SetValue(HideTimelineCommandProperty, value); }
        }

        public static readonly DependencyProperty HideTimelineCommandProperty =
            DependencyProperty.Register("HideTimelineCommand", typeof(IRelayCommand), typeof(TimelineToolContainer), new PropertyMetadata(default(IRelayCommand)));
        #endregion

        #region Method
        public TimelineToolContainer()
        {
            ZoomFactor = 1;
            MouseToolCommand = new RelayCommand(MouseTool_Click);
            AddKeyFramesCommand = new RelayCommand(AddKeyFrames_Click);
            DeleteKeyFramesCommand = new RelayCommand(DeleteFrames_Click);
            CopyCommand = new RelayCommand(Copy_Click);
            SplitCommand = new RelayCommand(Split_Click);
            CuttingCommand = new RelayCommand(Cutting_Click);
            HorizontalFlipCommand = new RelayCommand(HorizontalFlip_Click);
            ReverseCommand = new RelayCommand(Reverse_Click);
            AddTrackCommand = new RelayCommand(AddTrack_Click);
            DeleteTrackCommand = new RelayCommand(DeleteTrack_Click);
            PlayCommand = new RelayCommand(Play_Click);

            ShrinkCommand = new RelayCommand(Shrink_Click);
            EnlargeCommand = new RelayCommand(Enlarge_Click);
            HideTimelineCommand = new RelayCommand(HideTimeline_Click);
        }

        private void UpdateTotalTimeDisplay()
        {
            double maxSeconds = timeRulerElement.Maximum;
            TimeSpan t = TimeSpan.FromSeconds(maxSeconds);

            int seconds = t.Seconds;
            if(seconds == 0)
            {
                seconds = t.Minutes * 60;
            }

            int milliseconds = t.Milliseconds;

            int frames = (int)Math.Round(maxSeconds * 20) % 20;

            TotalTime = string.Format(" | {0:D2}s{1:D2}ms({2:D2}f)",seconds,milliseconds,frames);
        }

        private void UpdateCurrentTimeDisplay()
        {
            if (timeline is not null)
            {
                int seconds = timeline.CurrentTime.Seconds;
                int milliseconds = timeline.CurrentTime.Milliseconds;
                int frames = (int)(timeline.CurrentTime.TotalSeconds * 20) % 20;
                CurrentTime = string.Format("{0:D2}s{1:D2}ms{2:D2}f", seconds, milliseconds, frames);
            }
        }
        #endregion

        #region Event
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            timeline = GetTemplateChild("timeline") as Timeline;
            timeline.Loaded += (sender,e)=> 
            { 
                timeRulerElement = timeline.Ruler;
                timeline.TimeUpdateAction = UpdateCurrentTimeDisplay;

                if(timeline is not null)
                {
                    UpdateCurrentTimeDisplay();
                }
                if (timeRulerElement is not null)
                {
                    UpdateTotalTimeDisplay();
                }
            };
        }

        public static void OnIsShowPreviewLine_ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is TimelineToolContainer container && e.NewValue is bool newValue)
            {
                container.OnCurrentIsShowPreviewLine_ValueChanged(newValue);
            }
        }

        public static void OnZoomFactor_ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is TimelineToolContainer container && e.NewValue is double newValue)
            {
                container.OnCurrentZoomFactor_ValueChanged(newValue);
            }
        }

        private void OnCurrentZoomFactor_ValueChanged(double newValue)
        {
            if (timeRulerElement is not null)
            {
                timeRulerElement.ZoomFactor = newValue;
            }
            timeline?.UpdatePlayHeadPositionByCurrentTime();
        }

        private void OnCurrentIsShowPreviewLine_ValueChanged(bool newValue)
        {
            if (timeline is not null)
            {
                timeline.IsShowPreviewLine = newValue;
            }
        }

        private void VectorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ZoomFactor = e.NewValue;
        }

        /// <summary>
        /// 鼠标工具
        /// </summary>
        private void MouseTool_Click()
        {

        }

        private void Play_Click()
        {

        }

        private void DeleteTrack_Click()
        {

        }

        private void AddTrack_Click()
        {

        }

        private void Reverse_Click()
        {

        }

        private void HorizontalFlip_Click()
        {

        }

        private void Cutting_Click()
        {

        }

        private void Split_Click()
        {

        }

        private void Copy_Click()
        {

        }

        private void DeleteFrames_Click()
        {

        }

        private void AddKeyFrames_Click()
        {
            if(timeline is not null)
            {
                timeline.TrackList.Add();
            }
        }

        /// <summary>
        /// 缩小按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Shrink_Click()
        {
            ZoomFactor--;
            if(ZoomFactor < 1)
            {
                ZoomFactor = 1;
            }
        }

        /// <summary>
        /// 放大按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Enlarge_Click()
        {
            ZoomFactor++;
            if(ZoomFactor > 10)
            {
                ZoomFactor = 10;
            }
        }

        /// <summary>
        /// 隐藏时间线
        /// </summary>
        private void HideTimeline_Click()
        {

        }
        #endregion
    }
}