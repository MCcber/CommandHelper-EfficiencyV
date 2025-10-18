using MinecraftLanguageModelLibrary.Model.MCDocument;
using MinecraftLanguageModelLibrary.Model.MCDocument.EnumContent;
using System.Windows;

namespace CBHK.Domain.Service
{
    public interface INBTComponentBuilder
    {
        #region 获取顶层数据
        public List<Structure> GetStructureList();
        public List<Enumeration> GetEnumList();
        public List<TypeAlia> GetTypeAliaList();
        public List<string> GetUseStatementList();
        public List<Injection> GetInjectionList();
        public List<DispatchStatement> GetDispatchStatementList();
        #endregion

        #region 获取中层数据
        public List<StructField> GetStructFieldList();

        public List<EnumField> GetEnumField();
        #endregion

        #region 处理版本差异
        public bool IsVersionSupported(string version);
        #endregion

        #region 生成有意义的NBT组件
        public List<FrameworkElement> BuildNBTComponent();
        #endregion
    }
}