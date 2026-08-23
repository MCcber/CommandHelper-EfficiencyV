using CBHK.Model.Constant;
using MinecraftLanguageModelLibrary.Data;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace CBHK.Utility.Data.DTOBuilder
{
    public static class MCDocumentResourceBuilder
    {
        /// <summary>
        /// 构建资源数据
        /// </summary>
        /// <param name="target"></param>
        /// <param name="template"></param>
        /// <param name="version"></param>
        /// <param name="documentPath"></param>
        /// <param name="resource"></param>
        /// <param name="helper"></param>
        public static void BuildResource(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template, string version, DocumentPath documentPath, Resource resource, MCDocumentMetaTypeDTOHelper helper)
        {
            #region 处理资源
            if (target.FeatureMap is not null && target.FeatureMap.Count > 0)
            {
                #region 处理ID
                List<string> EnumOptionList = [];
                //解析id键对应的文档资源
                if (target.FeatureMap.TryGetValue("id", out MetaValue idObject))
                {
                    MetaTypeEditorFieldDTO enumDTO = new()
                    {
                        FieldName = target.FieldName,
                        TypeKind = MetaTypeKind.Enum,
                        ID = "placeHolder",
                        Parent = target.Parent,
                        Path = documentPath ?? target.Path,
                        EnumOptionList = []
                    };
                    if (target.TypeKind is MetaTypeKind.Dispatch or MetaTypeKind.Struct)
                    {
                        target.TypeKind = MetaTypeKind.Composite;
                        target.Items =
                        [
                            enumDTO,
                            new MetaTypeEditorFieldDTO()
                            {
                                FieldName = "",
                                TypeKind = MetaTypeKind.Add,
                                Path = documentPath ?? target.Path,
                                AddItemCommand = helper.CreateAddItemCommand(target,version),
                                ID = "placeHolder",
                                Parent = target.Parent
                            }
                        ];
                        target.EnumOptionList = null;
                        target.FieldName = "";
                    }
                    else
                    {
                        target.TypeKind = MetaTypeKind.Enum;
                        target.EnumOptionList ??= [];
                        enumDTO = target;
                    }

                    EnumOptionList.Insert(0, "- unset -");

                    //提取简单数据
                    if (idObject.TypeValue?.LiteralValue is not null && resource.RunningDataObject[version][idObject.TypeValue.LiteralValue.ToString().Trim('"')] is JArray literalResourceArray)
                    {
                        EnumOptionList.AddRange(literalResourceArray.Values<string>());
                    }
                    //提取复合数据
                    else if (idObject.Kind is MetaValueKind.Tuple && idObject.Members is not null)
                    {
                        #region 收集数据模型
                        MetaNamedValue registryValue = idObject.Members.FirstOrDefault(item => item.Name == "registry");
                        MetaNamedValue pathValue = idObject.Members.FirstOrDefault(item => item.Name == "path");
                        MetaNamedValue excludeValue = idObject.Members.FirstOrDefault(item => item.Name == "exclude");
                        MetaNamedValue includeValue = idObject.Members.FirstOrDefault(item => item.Name == "include");
                        MetaNamedValue prefixValue = idObject.Members.FirstOrDefault(item => item.Name == "prefix");
                        MetaNamedValue suffixValue = idObject.Members.FirstOrDefault(item => item.Name == "suffix");
                        string registryValueString = registryValue?.Value?.TypeValue?.LiteralValue is not null ? registryValue.Value.TypeValue.LiteralValue.ToString() : "";
                        #endregion

                        #region 提取包含列表与排除列表
                        HashSet<string> excludeValueStringList = [];
                        HashSet<string> includeValueStringList = [];

                        if (excludeValue?.Value?.Items is not null)
                        {
                            string text = "";
                            for (int j = 0; j < excludeValue.Value.Items.Count; j++)
                            {
                                text = excludeValue.Value.Items[j].TypeValue.LiteralValue.ToString();
                                excludeValueStringList.Add(text);
                            }
                        }
                        if (includeValue?.Value?.Items is not null)
                        {
                            string text = "";
                            for (int j = 0; j < includeValue.Value.Items.Count; j++)
                            {
                                text = includeValue.Value.Items[j].TypeValue.LiteralValue.ToString();
                                includeValueStringList.Add(text);
                            }
                        }
                        #endregion

                        #region 提取前后缀
                        string prefixValueString = prefixValue?.Value?.TypeValue?.LiteralValue is not null ? prefixValue.Value.TypeValue.LiteralValue.ToString() : "";
                        string suffixValueString = suffixValue?.Value?.TypeValue?.LiteralValue is not null ? suffixValue.Value.TypeValue.LiteralValue.ToString() : "";
                        #endregion

                        #region 添加目标资源数组、处理添加与删除列表
                        if (!string.IsNullOrEmpty(registryValueString) && resource.RunningDataObject[version][registryValueString] is JArray targetRegistryArray)
                        {
                            //路径过滤
                            HashSet<string> pathedEnumValueSet = [.. targetRegistryArray.Values<string>()];
                            if (pathValue is not null && pathValue.Value?.TypeValue?.LiteralValue is not null)
                            {
                                string targetPathString = pathValue.Value.TypeValue.LiteralValue.ToString();
                                EnumOptionList.AddRange(pathedEnumValueSet.Where(item => item.StartsWith(targetPathString)));
                            }
                            else
                            {
                                EnumOptionList.AddRange(targetRegistryArray.Values<string>());
                            }
                        }

                        //删除排除列表的所有成员
                        EnumOptionList.RemoveAll(excludeValueStringList.Contains);
                        //添加包含列表的所有成员
                        EnumOptionList.AddRange(includeValueStringList);
                        #endregion

                        #region 添加前缀与后缀
                        if (!string.IsNullOrEmpty(prefixValueString) || !string.IsNullOrEmpty(suffixValueString))
                        {
                            for (int j = 1; j < EnumOptionList.Count; j++)
                            {
                                EnumOptionList[j] = prefixValueString + EnumOptionList[j] + suffixValueString;
                            }
                        }
                        #endregion
                    }

                    #region 添加处理完毕的枚举列表
                    if (EnumOptionList[0].Contains('='))
                    {
                        for (int j = 0; j < EnumOptionList.Count; j++)
                        {
                            var enumList = EnumOptionList[j].Split('=');
                            enumDTO.EnumOptionList.Add(new EnumMember { Name = enumList[0], Value = new MetaValue { LiteralValue = enumList[^1] } });
                        }
                    }
                    else
                    {
                        for (int j = 0; j < EnumOptionList.Count; j++)
                        {
                            enumDTO.EnumOptionList.Add(new EnumMember { Name = EnumOptionList[j], Value = new MetaValue { LiteralValue = EnumOptionList[j] } });
                        }
                    }
                    if(!enumDTO.IsRequired && enumDTO.SelectedEnumOption is null)
                    {
                        enumDTO.SelectedEnumOption = enumDTO.EnumOptionList[0];
                    }
                    enumDTO.SelectedEnumItemUpdated = () => helper.SelectedEnumItemUpdated(enumDTO, version);
                    #endregion
                }
                #endregion

                #region 处理UUID
                if (target.FeatureMap.ContainsKey("uuid"))
                {
                    target.ReFreshCommand = helper.CreateReFreshCommand(target, version);
                    target.TypeKind = MetaTypeKind.UUIDArray;
                    target.Items ??= [];
                    target.Items.Clear();
                    return;
                }
                #endregion

                #region 识别颜色资源
                if (target.FeatureMap.TryGetValue("color", out MetaValue colorObject) && colorObject is not null && colorObject.Kind is MetaValueKind.Literal)
                {
                    string colorType = colorObject.LiteralValue.ToString();
                    switch (colorType)
                    {
                        case "hex_rgb":
                            {
                                target.TypeKind = MetaTypeKind.HexRGB;
                                break;
                            }
                        case "hex_rgba":
                            {
                                target.TypeKind = MetaTypeKind.HexARGB;
                                break;
                            }
                        case "dec_rgb":
                            {
                                target.TypeKind = MetaTypeKind.DecRGB;
                                break;
                            }
                        case "dec_rgba":
                            {
                                target.TypeKind = MetaTypeKind.DecRGBA;
                                break;
                            }
                        case "composite_rgb":
                            {
                                target.TypeKind = MetaTypeKind.CompositeRGB;
                                break;
                            }
                        case "composite_rgba":
                            {
                                target.TypeKind = MetaTypeKind.CompositeARGB;
                                break;
                            }
                    }
                }
                #endregion
            }
            #endregion

            #region 处理Dispatch
            if (target.FeatureMap.ContainsKey("Resource") && target.FeatureMap.ContainsKey("Index") && !target.FeatureMap.ContainsKey("id"))
            {
                if (string.IsNullOrEmpty(target.FieldName))
                {
                    target.IsVisible = false;
                    return;
                }
                helper.GetDispatchResource(target, version);
            } 
            #endregion

            #region 对于没有 FeatureMap 的普通枚举节点，也需要设置默认选中项
            if (target.EnumOptionList?.Count > 0 && target.SelectedEnumOption is null)
            {
                target.SelectedEnumOption = target.EnumOptionList[0];
            }
            #endregion
        }

        /// <summary>
        /// 处理基础类型的通用属性
        /// </summary>
        /// <param name="target"></param>
        public static void BaseDataHandler(MetaTypeEditorFieldDTO target)
        {
            if (!target.IsRequired)
            {
                target.IsFalse = target.IsTrue = false;
            }
        }
    }
}
