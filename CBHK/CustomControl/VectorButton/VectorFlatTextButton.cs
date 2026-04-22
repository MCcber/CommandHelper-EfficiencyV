using System.Windows;
using System.Windows.Controls;

namespace CBHK.CustomControl.VectorButton
{
    public class VectorFlatTextButton : BaseVectorFlatButton
    {
        #region Field
        #endregion

        #region Property
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(VectorFlatTextButton), new PropertyMetadata(default(string)));
        #endregion

        #region Method
        public VectorFlatTextButton()
        {
            ClickMode = ClickMode.Press;
        }
        #endregion

        #region Event
        #endregion
    }
}
