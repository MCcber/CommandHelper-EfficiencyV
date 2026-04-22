using CBHK.Model.Common;
using CBHK.Model.Constant;
using CBHK.Utility.Visual;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CBHK.CustomControl
{
    public class BaseVectorTextButton : Button
    {
        #region Field
        protected Brush OriginBackground;
        #endregion

        #region Property
        public bool IsManualBackground { get; set; }
        /// <summary>
        /// 背景颜色渐变率
        /// </summary>
        public virtual float ModifyRate { get; set; } = 0.0f;
        /// <summary>
        /// 背景颜色渐变模式
        /// </summary>
        public virtual ColorModifyMode ModifyMode { get; set; } = ColorModifyMode.Lighten;

        public Brush ThemeBackground
        {
            get { return (Brush)GetValue(ThemeBackgroundProperty); }
            set { SetValue(ThemeBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ThemeBackgroundProperty =
            DependencyProperty.Register("ThemeBackground", typeof(Brush), typeof(BaseVectorTextButton), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public BaseVectorTextButton()
        {
            SetResourceReference(ThemeBackgroundProperty, Theme.CommonBackground);
        }

        public virtual void UpdateBorderColorByBackgroundColor()
        {
            if (ThemeBackground is SolidColorBrush solidColorBrush && !IsManualBackground)
            {
                Background = new SolidColorBrush(ColorTool.ModifyColorBrightness(solidColorBrush.Color, ModifyRate, ModifyMode));
            }
        }
        #endregion

        #region Event

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == ThemeBackgroundProperty)
            {
                UpdateBorderColorByBackgroundColor();
            }
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
        }
        #endregion
    }
}
