using CBHK.Model.Constant;
using CBHK.Model.Data;
using CBHK.Utility.Data.DTOBuilder;
using MinecraftLanguageModelLibrary.Data;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;

namespace CBHK.Utility.Data
{
    public partial class MetaTypeDTOValidator(Resource resource)
    {
        #region Field
        private Resource resource = resource;
        #endregion

        #region Property
        public DocumentDTOBuildStrategyRegistry Registry { get; set; }
        public MCDocumentMetaTypeDTOHelper DTOHelper { get; set; }

        #endregion

        #region Method
        /// <summary>
        /// 验证单层DTO实例
        /// </summary>
        /// <param name="targetValueTuple">实例 DTO 根列表</param>
        /// <param name="templateList">模板 DTO 根列表</param>
        /// <returns></returns>
        public void Verify(
            (List<MetaTypeEditorFieldDTO>, Dictionary<string, KeyValueAnchors>) targetValueTuple,
            List<MetaTypeEditorFieldDTO> templateList,
            string version,
            StringBuilder documentItemPath = null)
        {
            #region 遍历前就执行版本过滤，将计算开销降到最低
            if (templateList.Count == 0)
            {
                return;
            }

            List<MetaTypeEditorFieldDTO> versionFilteredTemplateList = [.. templateList];
            if (versionFilteredTemplateList is null || (versionFilteredTemplateList is not null && versionFilteredTemplateList.Count == 0))
            {
                return;
            }
            MCDocumentMetaTypeDTOHelper.VerifyVersion(versionFilteredTemplateList, version);
            if (!versionFilteredTemplateList.Contains(targetValueTuple.Item1[0]))
            {
                MCDocumentMetaTypeDTOHelper.VerifyVersion(targetValueTuple.Item1, version);
            }
            versionFilteredTemplateList.RemoveAll(item => item.IsVisible == false);
            targetValueTuple.Item1.RemoveAll(item => item.IsVisible == false);
            #endregion

            for (int i = 0; i < versionFilteredTemplateList.Count; i++)
            {
                #region Field
                string currentTemplateFieldName = versionFilteredTemplateList[i].FieldName;
                MetaTypeEditorFieldDTO targetDTO = null;
                #endregion

                #region 查找相同的实例与模板
                if (targetValueTuple.Item1.Count > 0)
                {
                    targetDTO = targetValueTuple.Item1.FirstOrDefault(item => item.FieldName == currentTemplateFieldName && !string.IsNullOrWhiteSpace(currentTemplateFieldName));
                    //检查是否有重合的元素
                    var existDTO = targetDTO is null ? targetValueTuple.Item1.FirstOrDefault(item => item.TemplateReference == versionFilteredTemplateList[i] || item.ID == versionFilteredTemplateList[i].ID) : null;
                    if (existDTO is not null)
                    {
                        targetDTO ??= existDTO;
                    }
                }
                #endregion

                #region 识别版本
                string maxVersion = version;
                if (version.Contains('-'))
                {
                    maxVersion = version.Split('-')[1];
                }
                #endregion

                #region 处理资源、分配策略并执行构造
                if (targetDTO is not null)
                {
                    MCDocumentResourceBuilder.BaseDataHandler(targetDTO);
                    MCDocumentResourceBuilder.BuildResource(targetDTO, versionFilteredTemplateList[i], maxVersion, documentItemPath, resource, DTOHelper);
                    //由注册器使用策略模式动态分配不同类型的构造器执行DTO实例构造任务
                    var childRegistry = Registry.Get(targetDTO.TypeKind);
                    childRegistry.Build(targetDTO, versionFilteredTemplateList[i], maxVersion, documentItemPath, targetValueTuple.Item2);
                }
                #endregion

                #region 没有名称相同的DTO则克隆模板为实例DTO
                else
                {
                    #region Field And Init
                    MetaTypeEditorFieldDTO instance = DTOHelper.InstantiateDTO(versionFilteredTemplateList[i], maxVersion);
                    MetaValue targetIDKey = new();
                    MCDocumentMetaTypeDTOHelper.VerifyVersion([instance], maxVersion);
                    #endregion

                    #region 提取id的键值对或Value并转化值
                    _ = instance.FeatureMap?.TryGetValue("id", out targetIDKey);
                    string targetIDKeyString = "";
                    if (targetIDKey is not null && targetIDKey.TypeValue?.LiteralValue is not null)
                    {
                        targetIDKeyString = targetIDKey.TypeValue.LiteralValue.ToString();
                    }
                    else if (instance.Value is not null && !string.IsNullOrEmpty(instance.Value.ToString()))
                    {
                        targetIDKeyString = instance.Value.ToString();
                    }
                    #endregion

                    #region 处理结构体的挂载节点
                    if (documentItemPath is not null && instance.TypeKind is MetaTypeKind.Literal && !string.IsNullOrEmpty(targetIDKeyString))
                    {
                        MetaTypeEditorFieldDTO targetStruct = new() { ID = "placeHolder", TypeKind = MetaTypeKind.Any };
                        string documentItemPathString = documentItemPath.ToString();
                        int lastDoubleColonIndex = documentItemPathString.LastIndexOf("::");

                        if (lastDoubleColonIndex > -1 && !resource.DocumentItemMap.TryGetValue(documentItemPathString[..lastDoubleColonIndex] + "::" + targetIDKeyString, out targetStruct))
                        {
                            targetStruct = resource.DocumentItemMap.FirstOrDefault(item => item.Key.EndsWith(targetIDKeyString)).Value;
                        }

                        if (targetStruct is not null)
                        {
                            instance.TypeKind = targetStruct.TypeKind;
                            if (string.IsNullOrEmpty(instance.FieldName) || string.IsNullOrWhiteSpace(instance.FieldName))
                            {
                                instance.FieldName = targetStruct.FieldName;
                            }

                            // 统一判断所有容器类型
                            if (MCDocumentMetaTypeDTOHelper.IsContainerType(targetStruct.TypeKind))
                            {
                                //统一使用placeHolder
                                instance.Children = [new MetaTypeEditorFieldDTO() { ID = "placeHolder", TypeKind = MetaTypeKind.Any }];
                            }
                        }
                    }
                    #endregion

                    #region 处理枚举类节点
                    if (instance.TypeKind is MetaTypeKind.String && resource.RunningDataObject[maxVersion][targetIDKeyString] is JArray targetJArray)
                    {
                        instance.TypeKind = MetaTypeKind.Enum;
                        instance.EnumOptionList ??= [];
                        if (!instance.IsRequired)
                        {
                            instance.EnumOptionList.Add(new EnumMember() { Name = "- unset -", Value = new MetaValue() { LiteralValue = "unset" } });
                        }
                        instance.EnumOptionList.AddRange(targetJArray.Values<string>().Select(item => new EnumMember() { Name = item, Value = new MetaValue() { LiteralValue = item } }));
                        instance.SelectedEnumOption = instance.EnumOptionList[0];
                        instance.SelectedEnumItemUpdated = () => MCDocumentMetaTypeDTOHelper.SelectedEnumItemUpdated(instance);
                    }
                    #endregion

                    #region 处理可选/必选
                    if (!instance.IsRequired && instance.TypeKind is not MetaTypeKind.Struct or MetaTypeKind.Dispatch or MetaTypeKind.ByteArray or MetaTypeKind.LongArray or MetaTypeKind.IntArray or MetaTypeKind.List)
                    {
                        instance.Value = null;
                        instance.IsTrue = instance.IsFalse = false;
                    }
                    #endregion

                    #region 直接在对应位置插入或追加
                    targetValueTuple.Item1 ??= [];
                    if (i < targetValueTuple.Item1.Count)
                    {
                        targetValueTuple.Item1.Insert(i, instance);
                    }
                    else
                    {
                        targetValueTuple.Item1.Add(instance);
                    }
                    // 确保新克隆的枚举节点有默认选中项（InstantiateDTO 已设置，此处兜底）
                    if (instance.TypeKind is MetaTypeKind.Enum && instance.EnumOptionList is not null && instance.EnumOptionList.Count > 0 && instance.SelectedEnumOption is null)
                    {
                        instance.SelectedEnumOption = instance.EnumOptionList[0];
                    }
                    #endregion
                }
                #endregion
            }
        } 
        #endregion
    }
}