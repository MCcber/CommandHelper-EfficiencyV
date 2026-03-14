using CBHK.Interface;

namespace CBHK.Model.Common
{
    public struct KeyFrameData<T>(T value, InterpolationType easing = InterpolationType.Linear)
        : IKeyFrameData
        where T : struct
    {
        public T Value = value;
        public InterpolationType Easing = easing;

        object IKeyFrameData.Value
        {
            get => Value;
            set => Value = (T)value;
        }

        public readonly IKeyFrameData Clone() => new KeyFrameData<T>(Value, Easing);
    }
}