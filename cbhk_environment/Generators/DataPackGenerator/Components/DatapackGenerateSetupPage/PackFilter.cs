namespace cbhk_environment.Generators.DataPackGenerator.Components.DatapackGenerateSetupPage
{
    /// <summary>
    /// 包过滤器数据结构
    /// </summary>
    public class PackFilter
    {
        private string nameSpace = "";
        public string NameSpace
        {
            get => nameSpace;
            set => nameSpace = value;
        }

        private string path = "";
        public string Path
        {
            get => path;
            set => path = value;
        }
    }
}
