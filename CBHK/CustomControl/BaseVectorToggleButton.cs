using CBHK.Model.Common;
using CBHK.Model.Constant;
using CBHK.Utility.Visual;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace CBHK.CustomControl
{
    public class BaseVectorToggleButton : ToggleButton
    {
        #region Property
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
            DependencyProperty.Register("ThemeBackground", typeof(Brush), typeof(BaseVectorToggleButton), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public BaseVectorToggleButton()
        {
            SetResourceReference(ThemeBackgroundProperty, Theme.CommonBackground);
        }

        public virtual void UpdateBorderColorByBackgroundColor()
        {
            if (ThemeBackground is SolidColorBrush solidColorBrush)
            {
                Background = new SolidColorBrush(ColorTool.ModifyColorBrightness(solidColorBrush.Color, 0f, ModifyMode));
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
        #endregion
    }
}
