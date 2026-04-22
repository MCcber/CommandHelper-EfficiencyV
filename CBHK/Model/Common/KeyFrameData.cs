using CBHK.Interface.Data;
using MathNet.Numerics.Interpolation;

namespace CBHK.Model.Common
{
    public struct KeyFrameData<T>(object targetName, T value,double deltaTime, InterpolationType easing = InterpolationType.Linear)
        : IKeyFrameData
        where T : struct
    {
        public object TargetName { get; set; } = targetName;
        public T RightValue { get; set; } = value;
        public T LeftValue { get; set; }
        public double RightDeltaValue { get; set; } = deltaTime;
        public InterpolationType RightEasing { get; set; } = easing;
        public KeyFrameValueType RightValueType { get; set; }
        public IInterpolation RightInterpolation { get; set; }

        object IKeyFrameData.RightDeltaValue
        {
            get => RightDeltaValue;
            set => RightDeltaValue = (double)value;
        }

        object IKeyFrameData.LeftValue
        {
            get => LeftValue;
            set => LeftValue = (T)value;
        }

        object IKeyFrameData.RightValue
        {
            get => RightValue;
            set => RightValue = (T)value;
        }

        public readonly IKeyFrameData Clone() => new KeyFrameData<T>(targetName,RightValue,RightDeltaValue, RightEasing);
    }
}