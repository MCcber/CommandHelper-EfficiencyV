using CBHK.Model.Common;
using CBHK.Model.Constant;
using CBHK.Utility.Visual;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl
{
    /// <summary>
    /// 基础矢量控件，能够自动适应主题背景颜色的变化，提供渐变效果
    /// </summary>
    public class BaseVectorControl : Control
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
            DependencyProperty.Register("ThemeBackground", typeof(Brush), typeof(BaseVectorControl), new PropertyMetadata(default(Brush)));
        #endregion

        #region Method
        public BaseVectorControl()
        {
            SetResourceReference(ThemeBackgroundProperty, Theme.CommonBackground);
        }

        public virtual void UpdateBorderColorByBackgroundColor()
        {
            if (ThemeBackground is SolidColorBrush solidColorBrush)
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
        #endregion
    }
}