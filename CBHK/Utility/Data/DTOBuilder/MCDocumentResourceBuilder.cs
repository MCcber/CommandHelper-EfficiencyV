using CBHK.Model.Constant;
using MinecraftLanguageModelLibrary.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CBHK.Utility.Data.DTOBuilder
{
    public static class MCDocumentResourceBuilder
    {
        private static Dictionary<string, MetaTypeKind> colorDictionary = new() { 
            { "named",MetaTypeKind.NamedColor } ,
            { "hex_rgb",MetaTypeKind.NamedColor },
            { "hex_rgba",MetaTypeKind.NamedColor },
            { "hex_argb",MetaTypeKind.NamedColor },
            { "dec_rgb",MetaTypeKind.NamedColor },
            { "dec_rgba",MetaTypeKind.NamedColor },
            { "composite_rgb",MetaTypeKind.NamedColor },
            { "composite_rgba",MetaTypeKind.NamedColor },
            { "composite_argb",MetaTypeKind.NamedColor }
        };

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
            #region 解析定义引用（别名与泛型应用）
            //名字载体（字面量/引用）用 Value 承载名字，其余类型用 TypeName 承载定义名
            string definitionName = target.TypeKind is MetaTypeKind.Literal or MetaTypeKind.Reference
                ? target.Value?.ToString() ?? ""
                : target.TypeName ?? "";
            if (!string.IsNullOrEmpty(definitionName) && !ReferenceEquals(target.TemplateReference, target))
            {
                ResolvedTypeReference reference = UsePathParser.Parse(resource, documentPath ?? target.Path, definitionName);
                if (reference?.Item is MetaTypeEditorFieldDTO definition && !ReferenceEquals(definition, target))
                {
                    MetaTypeEditorFieldDTO definitionInstance = helper.InstantiateDTO(definition, version);
                    List<Tuple<string, MetaValue>> formalParameters = definition.TypeParameterNameList ?? [];
                    List<MetaValue> actualArguments = target.ActualTypeArguments ?? [];
                    if (formalParameters.Count > 0 && actualArguments.Count > 0)
                    {
                        //按位置绑定形参后替换，结果落在定义实例上，不修改文档模板本身
                        target.BindingScope = MCDocumentMetaTypeDTOHelper.CreateBindingScope(formalParameters, actualArguments, target.BindingScope, target.Path?.TargetPath ?? documentPath?.TargetPath);
                        definitionInstance.Children = new(helper.SubstituteGenericIterative(definition, formalParameters, actualArguments, version, target.BindingScope));
                        definitionInstance.TypeKind = definition.TypeKind;
                    }

                    bool isRequired = target.IsRequired;
                    //字段自身的注解（如 #[color=...]）不能被定义实例的属性覆盖，先留一份
                    Dictionary<string, MetaValue> ownFeatures = target.FeatureMap is null ? [] : new(target.FeatureMap);
                    target.CopyFrom(definitionInstance);
                    target.SetRequired(isRequired);
                    target.FeatureMap ??= [];
                    foreach (KeyValuePair<string, MetaValue> pair in ownFeatures)
                    {
                        target.FeatureMap[pair.Key] = pair.Value;
                    }
                    if (definitionInstance.Path?.TargetPath.Length > 0)
                    {
                        //定义体内部的名字要在定义所在的文档命名空间里解析
                        target.Path = new(definitionInstance.Path.TargetPath);
                    }
                }
            }
            #endregion

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
                        target.Items = [enumDTO];
                        MetaTypeEditorFieldDTO addDTO = MCDocumentMetaTypeDTOHelper.BuildAddButton(target.Parent, documentPath ?? target.Path);

                        addDTO.AddItemCommand = helper.CreateAddCompositeOrCompoundItemCommand(target, version);

                        addDTO.RemoveItemCommand = helper.CreateRemoveCompositeOrCompoundItemCommand(target, addDTO);
                        target.Items.Add(addDTO);
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
                    if (idObject.TypeValue?.LiteralValue is not null && resource.RunningDataObject.TryGetValue(version, out JToken versionToken) && versionToken.SelectToken(idObject.TypeValue.LiteralValue.ToString().Trim('"')) is JArray literalResourceArray)
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
                        if (!string.IsNullOrEmpty(registryValueString) && resource.RunningDataObject.TryGetValue(version, out versionToken) && versionToken.SelectToken(registryValueString) is JArray targetRegistryArray)
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
                if (target.FeatureMap.TryGetValue("color", out MetaValue colorObject) && colorObject is not null)
                {
                    //属性的值可能是类型、字面量或列表，统一取出颜色种类名（hex_rgb、named 等）
                    string colorType = colorObject.Kind switch
                    {
                        MetaValueKind.Type => colorObject.TypeValue?.LiteralValue?.ToString(),
                        MetaValueKind.Literal => colorObject.LiteralValue?.ToString(),
                        MetaValueKind.List => colorObject.Items?.FirstOrDefault()?.LiteralValue?.ToString(),
                        _ => null
                    } ?? "";
                    if (colorDictionary.TryGetValue(colorType, out MetaTypeKind colorKind))
                    {
                        target.TypeKind = colorKind;
                    }
                }
                #endregion
            }
            #endregion

            #region 处理Dispatch
            //只有调度器节点（或曾由调度器解释的节点）才执行调度，避免物化后的结果再次触发调度
            bool isDispatcher = target.TypeKind is MetaTypeKind.Dispatch || target.OriginKind is MetaTypeKind.Dispatch;
            if (isDispatcher && target.FeatureMap.ContainsKey("Resource") && target.FeatureMap.ContainsKey("Index") && !target.FeatureMap.ContainsKey("id"))
            {
                //调度节点常无路径，继承当前构建文档，供目标内部的类型名解析使用
                target.Path ??= documentPath;
                var dispatchResultDTO = helper.GetDispatchResource(target, version);
                if (dispatchResultDTO is not null)
                {
                    if (target.TypeKind is MetaTypeKind.Composite)
                    {
                        target.SelectedUnionChildren ??= [];
                        target.SelectedUnionChildren.Add(dispatchResultDTO);
                    }
                    else
                    {
                        //调度结果物化进本节点，避免多一层调度器外壳
                        string fieldName = target.FieldName;
                        bool isRequired = target.IsRequired;
                        DocumentPath path = target.Path;
                        Dictionary<string, MetaValue> featureMap = target.FeatureMap;
                        target.CopyFrom(dispatchResultDTO);
                        target.FieldName = fieldName;
                        target.SetRequired(isRequired);
                        target.Path ??= path;
                        target.FeatureMap = featureMap;
                        target.OriginKind = MetaTypeKind.Dispatch;
                    }
                }
                else if (!string.IsNullOrEmpty(target.FieldName))
                {
                    //调度目标解析失败时按 Any 走完管线，调度器特征保留以便后续重新解释
                    target.OriginKind = MetaTypeKind.Dispatch;
                    target.TypeKind = MetaTypeKind.Any;
                }
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

