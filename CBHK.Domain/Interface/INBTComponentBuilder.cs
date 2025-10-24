using MinecraftLanguageModelLibrary.Model.MCDocument;
using MinecraftLanguageModelLibrary.Model.MCDocument.EnumContent;
using MinecraftLanguageModelLibrary.Model.MCDocument.InjectionContent;
using System.Windows;

namespace CBHK.Domain.Interface
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
        public List<EnumInjection> GetEnumInjectionList();
        public List<StructInjection> GetStructInjectionList();
        #endregion

        #region 生成有意义的NBT组件
        public List<FrameworkElement> BuildNBTComponent();
        #endregion
    }
}