using static CBHK.Model.Common.Enums;

namespace CBHK.ViewModel.Component.Datapack.DatapackGenerateSetupPage
{
    public class DescriptionData
    {
        #region 简介类型枚举属性
        private PackDescriptionType packDescriptionType = PackDescriptionType.IntType;
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
