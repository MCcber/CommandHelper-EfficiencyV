using CBHK.Interface.Data;
using CBHK.Model.Constant;
using CBHK.Utility.Visual;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.Container
{
    public class ContinuousKeyframeItem : BaseVectorToggleButton, ICloneable
    {
        #region Property
        public bool IsPlayed { get; set; }
        public bool IsBorderKeyFrame { get; set; }

        public ObservableCollection<IKeyFrameData> DataList { get; set; } = [];

        public TimeSpan CurrentTime
        {
            get { return (TimeSpan)GetValue(CurrentTimeProperty); }
            set { SetValue(CurrentTimeProperty, value); }
        }

        public static readonly DependencyProperty CurrentTimeProperty =
            DependencyProperty.Register("CurrentTime", typeof(TimeSpan), typeof(ContinuousKeyframeItem), new PropertyMetadata(default(TimeSpan)));

        public double X
        {
            get { return (double)GetValue(XProperty); }
            set { SetValue(XProperty, value); }
        }

        public static readonly DependencyProperty XProperty =
            DependencyProperty.Register("X", typeof(double), typeof(ContinuousKeyframeItem), new PropertyMetadata(default(double)));
        #endregion

        #region Method
        public ContinuousKeyframeItem()
        {
            Loaded += ContinuousKeyframeItem_Loaded;
        }

        public override void UpdateBorderColorByBackgroundColor()
        {
            //base.UpdateBorderColorByBackgroundColor();
            if (ThemeBackground is SolidColorBrush solidColorBrush)
            {
                Background = new SolidColorBrush(ColorTool.ModifyColorBrightness(solidColorBrush.Color, 0.6f,ModifyMode));
            }
        }

        public object Clone()
        {
            ContinuousKeyframeItem result = new()
            {
                IsBorderKeyFrame = IsBorderKeyFrame,
                IsChecked = IsChecked,
                DataList = new(DataList.ToList().Select(item => item.Clone())),
                Width = Width,
                Height = Height,
                CurrentTime = CurrentTime,
                X = X,
                Style = Style
            };
            Canvas.SetTop(result, Canvas.GetTop(this));
            return result;
        }
        #endregion

        #region Event
        private void ContinuousKeyframeItem_Loaded(object sender, RoutedEventArgs e)
        {
            if (Background is null)
            {
                SetResourceReference(ThemeBackgroundProperty, Theme.CommonBackground);
            }
            UpdateBorderColorByBackgroundColor();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if(e.Property == ThemeBackgroundProperty)
            {
                UpdateBorderColorByBackgroundColor();
            }
        }

        protected override void OnChecked(RoutedEventArgs e)
        {
            base.OnChecked(e);
        }

        protected override void OnUnchecked(RoutedEventArgs e)
        {
            base.OnUnchecked(e);
        }
        #endregion
    }
}
