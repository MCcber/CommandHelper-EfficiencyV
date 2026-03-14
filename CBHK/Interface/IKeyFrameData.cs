namespace CBHK.Interface
{
    public interface IKeyFrameData
    {
        Model.Common.KeyFrameValueType ValueType { get; set; }
        object Value { get; set; }
        IKeyFrameData Clone();
    }
}
