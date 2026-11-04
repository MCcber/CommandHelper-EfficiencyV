using CBHK.Model.Constant;
using MinecraftLanguageModelLibrary.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
                    //版本门控逐节点独立：定义体的 since/until 不搬到使用点上
                    MCDocumentMetaTypeDTOHelper.ApplyOwnFeatures(target, ownFeatures);
                    //CopyFrom 只拷集合引用，子级仍指着定义实例：不补父链的话，
                    //同级字段驱动的调度器重解析会去定义实例上找宿主，切换静默失效
                    MCDocumentMetaTypeDTOHelper.AttachParentLinks(target);
                    if (definitionInstance.Path?.TargetPath.Length > 0)
                    {
                        //定义体内部的名字要在定义所在的文档命名空间里解析
                        target.Path = new(definitionInstance.Path.TargetPath);
                    }
                    //替换出来的子树要带上刚建的绑定作用域：否则内部 `to T` 这类泛型目标拿不到实参，
                    //调度解析失败后静默退化成 Any（表现为一行没有编辑器的空节点）
                    GenericDTOBuilder.AttachBindingScopeRecursively(target, target.BindingScope);
                }
            }
            #endregion

            #region 处理资源
            if (target.FeatureMap is not null && target.FeatureMap.Count > 0)
            {
                #region 处理ID
                List<string> EnumOptionList = [];
                //解析id键对应的文档资源
                //动态 map（[K]: V）的 id 属于它的键：这里把整个 map 改成枚举会吃掉 map 结构，
                //交给 Tuple 构建器去建键选择器（键自己的 id 由它解析）
                if (target.FeatureMap.TryGetValue("id", out MetaValue idObject) && target.TypeKind is not MetaTypeKind.Tuple)
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

                    //提取注册表数据：简单 id 与复合 id（registry/path/include/exclude/prefix/suffix）共用同一份收集逻辑
                    EnumOptionList.AddRange(CompoundGenericMetaValueParser.ParseIdOptionNames(resource, version, idObject));

                    #region 添加处理完毕的枚举列表
                    //同一节点可能被反复解释（切换分支时复用），每次重建而不是追加，否则选项会不断累积
                    enumDTO.EnumOptionList ??= [];
                    enumDTO.EnumOptionList.Clear();
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
                //列表形态的颜色（如 [float] @ 4）保留列表编辑器，不压成单值颜色，否则元素会丢
                if (target.ElementType is null && target.FeatureMap.TryGetValue("color", out MetaValue colorObject) && colorObject is not null)
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
                            dispatchResultDTO.Parent = target;
                    }
                    else
                    {
                        //调度结果物化进本节点，避免多一层调度器外壳
                        target.IsVisible = true;
                        string fieldName = target.FieldName;
                        string typeName = target.TypeName;
                        bool isRequired = target.IsRequired;
                        DocumentPath path = target.Path;
                        Dictionary<string, MetaValue> featureMap = target.FeatureMap;
                        target.CopyFrom(dispatchResultDTO);
                        //本节点无名时沿用调度结果自带的字段名：那正是目标定义里的外层字段
                        //（Conditions<C> 的 conditions），强行写回空名会把它变成一行无名节点
                        target.FieldName = string.IsNullOrEmpty(fieldName) ? target.FieldName : fieldName;
                        //调度结果自带的目标类型名不能被继承，否则本节点会被当成别名再次展开，调度器身份丢失
                        target.TypeName = typeName;
                        target.SetRequired(isRequired);
                        target.Path ??= path;
                        target.FeatureMap = featureMap;
                        target.OriginKind = MetaTypeKind.Dispatch;
                        //同上：调度结果物化进本节点后，子级的父链要指向本节点而不是调度结果实例
                        MCDocumentMetaTypeDTOHelper.AttachParentLinks(target);
                    }
                }
                else if (!string.IsNullOrEmpty(target.FieldName) && target.TypeKind is MetaTypeKind.Dispatch)
                {
                    //调度目标解析失败时按 Any 走完管线，调度器特征保留以便后续重新解释
                    //（已经物化出结构的节点不回退成 Any：重新解释失败不该丢掉上一次的解释结果）
                    target.OriginKind = MetaTypeKind.Dispatch;
                    target.TypeKind = MetaTypeKind.Any;
                }
                else
                {
                    //无名调度器（...resource[[field]] 展开）没解析出目标时没有任何可编辑内容，
                    //留着就是一行空壳；隐藏掉，等同级枚举定值后重解析成功再显示出来
                    target.IsVisible = false;
                }
            } 
            #endregion

            #region 对于没有 FeatureMap 的普通枚举节点，也需要设置默认选中项
            if (target.EnumOptionList?.Count > 0 && target.SelectedEnumOption is null)
            {
                target.SelectedEnumOption = target.EnumOptionList[0];
            }
            #endregion

            #region 按当前成员重建联合名称列表
            //成员（尤其跨层合并来的）可能在构建过程中才定型，名称列表要跟着最终成员走
            MCDocumentMetaTypeDTOHelper.RefreshUnionNameLists(target);
            #endregion

            #region 联合节点要能响应成员切换：补上初始选中项与切换事件
            if (target.TypeKind is MetaTypeKind.Union && target.SelectedUnionItemUpdated is null)
            {
                if (target.SelectedUnionTypeName is null && target.UnionTypeNameList?.Count > 0)
                {
                    target.SelectedUnionTypeName = target.UnionTypeNameList[0];
                }
                target.SelectedUnionItemUpdated = () => helper.SelectedUnionItemUpdated(target, version);
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

