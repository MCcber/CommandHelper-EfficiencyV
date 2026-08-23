using CBHK.Model.Constant;
using CBHK.Model.Data;
using CBHK.Utility.Data.DTOBuilder;
using MinecraftLanguageModelLibrary.Data;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;

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
        /// <param name="instanceContext">实例 DTO 根列表</param>
        /// <param name="templateList">模板 DTO 根列表</param>
        /// <returns></returns>
        public void Verify(
            DTOInstanceContext instanceContext,
            List<MetaTypeEditorFieldDTO> templateList,
            string version,
            DocumentPath documentItemPath = null,bool isLoaded = false)
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
            if (!versionFilteredTemplateList.Contains(instanceContext.dtoInstanceList[0]))
            {
                MCDocumentMetaTypeDTOHelper.VerifyVersion(instanceContext.dtoInstanceList, version);
            }
            versionFilteredTemplateList.RemoveAll(item => item.IsVisible == false);
            instanceContext.dtoInstanceList.RemoveAll(item => item.IsVisible == false);
            #endregion

            for (int i = 0; i < versionFilteredTemplateList.Count; i++)
            {
                #region Field
                string maxVersion = version;
                if (maxVersion.Contains('-'))
                {
                    maxVersion = maxVersion.Split('-')[1];
                }
                DocumentPath currentDocumentItemPath = null;
                if(documentItemPath is not null && documentItemPath.TargetPath.Length > 0)
                {
                    currentDocumentItemPath = new(documentItemPath.TargetPath);
                }
                //提前保存实例字段名
                string currentTemplateFieldName = versionFilteredTemplateList[i].FieldName;
                ResolvedTypeReference targetRealTemplate = new("", default);
                if (versionFilteredTemplateList[i].Value is not null && !string.IsNullOrEmpty(versionFilteredTemplateList[i].Value.ToString()))
                {
                    targetRealTemplate = UsePathParser.Parse(resource, currentDocumentItemPath, versionFilteredTemplateList[i].Value.ToString());
                }
                //设置为null遍无法执行浅表复制方法
                MetaTypeEditorFieldDTO currentTargetDTO = null;
                #endregion

                #region 查找相同的实例与模板
                //优先根据ID/模板搜索实例与模板组合
                if (instanceContext.dtoInstanceList.Count > 0)
                {
                    currentTargetDTO = instanceContext.dtoInstanceList.FirstOrDefault(item => item.FieldName == currentTemplateFieldName && !string.IsNullOrWhiteSpace(currentTemplateFieldName));
                    //检查是否有重合的元素
                    var existDTO = currentTargetDTO is null ? instanceContext.dtoInstanceList.FirstOrDefault(item => item.TemplateReference == versionFilteredTemplateList[i] || item.ID == versionFilteredTemplateList[i].ID) : null;
                    if (existDTO is not null)
                    {
                        currentTargetDTO ??= existDTO;
                    }
                }

                //非载入时用实际文档对象来尝试拼凑
                if(!string.IsNullOrEmpty(targetRealTemplate.Path) && targetRealTemplate.DTO is not null && !isLoaded)
                {
                    if (targetRealTemplate.DTO is not null)
                    {
                        bool isRequired = versionFilteredTemplateList[i].IsRequired;
                        versionFilteredTemplateList[i] = targetRealTemplate.DTO;
                        versionFilteredTemplateList[i].SetRequired(isRequired);
                    }
                    if (targetRealTemplate.Path is not null)
                    {
                        currentDocumentItemPath = new(targetRealTemplate.Path);
                    }
                }

                int currentIndex = 0;
                if (currentTargetDTO is not null)
                {
                    currentIndex = instanceContext.dtoInstanceList.IndexOf(currentTargetDTO);
                    currentTargetDTO = instanceContext.dtoInstanceList[currentIndex] = new(versionFilteredTemplateList[i]);
                }
                #endregion

                #region 处理资源、分配策略并执行构造
                if (currentTargetDTO is not null)
                {
                    currentTargetDTO.FieldName = currentTemplateFieldName;
                    MCDocumentResourceBuilder.BaseDataHandler(currentTargetDTO);
                    MCDocumentResourceBuilder.BuildResource(currentTargetDTO, versionFilteredTemplateList[i], maxVersion, currentDocumentItemPath, resource, DTOHelper);
                    //由注册器使用策略模式动态分配不同类型的构造器执行DTO实例构造任务
                    var childRegistry = Registry.Get(currentTargetDTO.TypeKind);
                    childRegistry.Build(currentTargetDTO, versionFilteredTemplateList[i], maxVersion, currentDocumentItemPath, instanceContext.anchorMap, !currentTargetDTO.IsRequired);
                }
                #endregion

                #region 没有名称相同的DTO则克隆模板为实例DTO
                else
                {
                    #region Field And Init
                    MetaTypeEditorFieldDTO instance = DTOHelper.InstantiateDTO(versionFilteredTemplateList[i], version);
                    MetaValue targetIDKey = new();
                    string templateDocumentItemParentPathString = string.Empty;
                    MCDocumentMetaTypeDTOHelper.VerifyVersion([instance], version);
                    //使用当前文档路径和模板的Value来拼凑实例DTO的Path，若当前文档路径为空则需要使用当前模板节点的路径
                    if (currentDocumentItemPath is not null && currentDocumentItemPath.TargetPath.Length > 0 && versionFilteredTemplateList[i].Value is not null)
                    {
                        templateDocumentItemParentPathString = currentDocumentItemPath.GetParentPath();
                        if(!string.IsNullOrEmpty(templateDocumentItemParentPathString))
                        {
                            instance.Path = new(templateDocumentItemParentPathString + "::" + versionFilteredTemplateList[i].Value.ToString());
                        }
                    }
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
                    if (currentDocumentItemPath is not null && instance.TypeKind is MetaTypeKind.Literal && !string.IsNullOrEmpty(targetIDKeyString))
                    {
                        MetaTypeEditorFieldDTO targetStruct = new() { ID = "placeHolder", TypeKind = MetaTypeKind.Any };

                        if (!string.IsNullOrEmpty(templateDocumentItemParentPathString) && !resource.DocumentItemMap.TryGetValue(templateDocumentItemParentPathString + "::" + targetIDKeyString, out targetStruct))
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
                        instance.SelectedEnumItemUpdated = () => DTOHelper.SelectedEnumItemUpdated(instance, maxVersion);
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
                    if (i < instanceContext.dtoInstanceList.Count)
                    {
                        instanceContext.dtoInstanceList.Insert(i, instance);
                    }
                    else
                    {
                        instanceContext.dtoInstanceList.Add(instance);
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