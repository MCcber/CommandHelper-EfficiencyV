namespace CBHK.Interface.Data
{
    public interface IBoolItem
    {
        public bool IsRequired { get; set; }
        public bool IsTrue { get; set; }
        public bool IsFalse { get; set; }
    }
}