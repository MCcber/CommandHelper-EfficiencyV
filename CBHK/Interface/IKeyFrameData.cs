using CBHK.CustomControl.Input;
using CBHK.Model.Common;
using MathNet.Numerics.Interpolation;

namespace CBHK.Interface
{
    public interface IKeyFrameData
    {
        IInterpolation RightInterpolation  { get; set; }
        KeyFrameValueType RightValueType { get; set; }
        InterpolationType RightEasing { get; set; }
        object RightDeltaValue { get; set; }
        object RightValue { get; set; }
        object LeftValue { get; set; }
        IKeyFrameData Clone();
    }
}