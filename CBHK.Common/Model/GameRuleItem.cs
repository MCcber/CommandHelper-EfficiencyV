namespace CBHK.Common.Model
{
    public class GameRuleItem
    {
        public enum DataType
        {
            Bool,
            Int
        }

        public DataType ItemType { get; set; }

        public string Value { get; set; } = "";

        public string Description { get; set; } = "";
    }
}
