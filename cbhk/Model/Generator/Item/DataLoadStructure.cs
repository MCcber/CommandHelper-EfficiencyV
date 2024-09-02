namespace cbhk.Model.Generator.Item
{
    public class DataLoadStructure(int index, object item, object version)
    {
        public int Index { get; set; } = index;
        public object Item { get; set; } = item;
        public object Version { get; set; } = version;
    }
}