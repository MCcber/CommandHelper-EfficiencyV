namespace cbhk_environment.Generators.DataPackGenerator.Components.DatapackGenerateSetupPage
{
    public class DescriptionData
    {
        #region 简介类型枚举属性
        public enum PackDescriptionType
        {
            stringType,
            boolType,
            intType,
            objectType,
            arrayType
        };

        private PackDescriptionType packDescriptionType = PackDescriptionType.intType;
        public PackDescriptionType DescriptionType
        {
            get { return packDescriptionType; }
            set
            {
                packDescriptionType = value;
            }
        }
        #endregion
    }
}
