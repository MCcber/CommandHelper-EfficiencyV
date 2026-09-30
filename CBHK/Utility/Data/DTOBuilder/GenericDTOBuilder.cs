using CBHK.Interface.Data;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using MinecraftLanguageModelLibrary.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CBHK.Utility.Data.DTOBuilder
{
    /// <summary>
    /// 泛型应用的构建策略：解析定义、按位置绑定形参与实参、替换形参占位符，
    /// 再把节点交回注册表按解析出的类型继续构建。
    /// </summary>
    public class GenericDTOBuilder(Resource resource, MCDocumentMetaTypeDTOHelper helper, DocumentDTOBuildStrategyRegistry registry) : IDocumentDTOBuildStrategy
    {
        #region Field
        private readonly Resource resource = resource;
        private readonly MCDocumentMetaTypeDTOHelper helper = helper;
        private readonly DocumentDTOBuildStrategyRegistry registry = registry;
        #endregion

        public void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template, string version, DocumentPath documentPath, Dictionary<string, KeyValueAnchors> anchorMap, RenderDepth depth, string typeName = "")
        {
            string targetTypeName = target.TypeName ?? typeName;
            if (documentPath is null || string.IsNullOrEmpty(targetTypeName))
            {
                ResolveAsAny(target);
                return;
            }

            var targetContext = UsePathParser.Parse(resource, target.Path ?? documentPath, targetTypeName);
            if (targetContext.Item is not MetaTypeEditorFieldDTO targetTypeDTO
                || targetTypeDTO.TypeParameterNameList is null
                || targetTypeDTO.TypeParameterNameList.Count == 0)
            {
                //定义解析不到时按 Any 走完管线
                ResolveAsAny(target);
                return;
            }

            #region 按位置绑定形参与实参
            List<Tuple<string, MetaValue>> formalParamMap = targetTypeDTO.TypeParameterNameList;
            List<MetaValue> actualArgList = target.ActualTypeArguments ?? [];
            target.BindingScope = MCDocumentMetaTypeDTOHelper.CreateBindingScope(formalParamMap, actualArgList, target.BindingScope, target.Path?.TargetPath ?? documentPath?.TargetPath);
            #endregion

            #region 替换形参占位符
            var substituteResult = helper.SubstituteGenericIterative(targetTypeDTO, formalParamMap, actualArgList, version, target.BindingScope);
            MCDocumentMetaTypeDTOHelper.VerifyVersion(substituteResult, version);
            List<MetaTypeEditorFieldDTO> expandedChildrenList = [.. substituteResult.Where(item => item.IsVisible)];

            for (int i = 0; i < expandedChildrenList.Count; i++)
            {
                AttachBindingScopeRecursively(expandedChildrenList[i], target.BindingScope);
                MCDocumentResourceBuilder.BaseDataHandler(expandedChildrenList[i]);
                MCDocumentResourceBuilder.BuildResource(expandedChildrenList[i], expandedChildrenList[i], version, documentPath, resource, helper);
                registry.Get(expandedChildrenList[i].TypeKind).Build(expandedChildrenList[i], expandedChildrenList[i], version, documentPath, anchorMap, depth, targetTypeName);
            }

            target.TypeKind = targetTypeDTO.TypeKind;
            if (targetTypeDTO.UnionTypeNameList is not null)
            {
                target.UnionTypeNameList = [.. targetTypeDTO.UnionTypeNameList];
            }
            if (targetTypeDTO.FeatureMap is not null)
            {
                target.FeatureMap = new(targetTypeDTO.FeatureMap);
            }
            if (targetTypeDTO.Value is not null)
            {
                target.Value = targetTypeDTO.Value;
            }
            target.IsTrue = targetTypeDTO.IsTrue;
            target.IsFalse = targetTypeDTO.IsFalse;
            target.Children = new ObservableCollection<MetaTypeEditorFieldDTO>(expandedChildrenList);
            #endregion

            #region 交回注册表继续
            if (target.TypeKind is MetaTypeKind.Generic)
            {
                //定义体本身仍是泛型应用时无法继续展开，按 Any 收尾
                ResolveAsAny(target);
                return;
            }
            registry.Get(target.TypeKind).Build(target, target, version, documentPath, anchorMap, depth, targetTypeName);
            #endregion
        }

        /// <summary>
        /// 无法解析的泛型应用按 Any 类型走完管线，同时保留泛型来源。
        /// </summary>
        private static void ResolveAsAny(MetaTypeEditorFieldDTO target)
        {
            target.OriginKind = MetaTypeKind.Generic;
            target.TypeKind = MetaTypeKind.Any;
        }

        /// <summary>
        /// 把当前泛型作用域递归挂到整棵子树上，供嵌套泛型与别名继续消费外层实参。
        /// </summary>
        private static void AttachBindingScopeRecursively(MetaTypeEditorFieldDTO node, TypeBindingScope scope)
        {
            node.BindingScope ??= scope;

            if (node.Children is not null)
            {
                foreach (MetaTypeEditorFieldDTO child in node.Children)
                {
                    AttachBindingScopeRecursively(child, scope);
                }
            }

            if (node.SelectedUnionChildren is not null)
            {
                foreach (MetaTypeEditorFieldDTO child in node.SelectedUnionChildren)
                {
                    AttachBindingScopeRecursively(child, scope);
                }
            }

            if (node.Items is not null)
            {
                foreach (MetaTypeEditorFieldDTO child in node.Items)
                {
                    AttachBindingScopeRecursively(child, scope);
                }
            }
        }

        public bool CanHandle(MetaTypeKind kind)
        {
            return kind is MetaTypeKind.Generic;
        }
    }
}
