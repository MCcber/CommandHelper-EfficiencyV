using MinecraftLanguageModelLibrary.Model.MCDocument;
using MinecraftLanguageModelLibrary.Model.MCDocument.EnumContent;
using MinecraftLanguageModelLibrary.Model.MCDocument.InjectionContent;
using System.Windows;

namespace CBHK.Domain.Interface
{
    public interface INBTComponentBuilder
    {
        #region Property
        public List<IComponent> ComponentList { get; set; }
        public MCDocumentFileModel? DocumentFileModel { get; set; }
        #endregion

        #region Method

        /// <summary>
        /// 整合引用，用于在全局字典中检索控件资源
        /// </summary>
        /// <returns>一个引用列表</returns>
        public List<IComponent> GetUsestatementList();

        /// <summary>
        /// 整合一个文件实例的所有结构体内的属性块
        /// </summary>
        /// <returns>一个控件列表</returns>
        public List<IComponent> GetStructList();

        /// <summary>
        /// 整合一个文件实例的所有枚举元数据
        /// </summary>
        /// <returns></returns>
        public List<IComponent> GetEnumMetaDataList();

        #endregion
    }
}