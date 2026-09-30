using CBHK.Interface.Data;
using CBHK.Model.Constant;
using CBHK.Model.Data;
using MinecraftLanguageModelLibrary.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CBHK.Utility.Data.DTOBuilder
{
    /// <summary>
    /// 调度器节点的构建策略：解析阶段已把调度目标挂到本节点上，
    /// 必选字段立刻物化目标结构，可选字段留给用户展开本节点时再物化。
    /// </summary>
    public class DispatchDTOBuilder(Resource resource, MCDocumentMetaTypeDTOHelper helper, DocumentDTOBuildStrategyRegistry registry) : IDocumentDTOBuildStrategy
    {
        #region Field
        private readonly Resource resource = resource;
        private readonly MCDocumentMetaTypeDTOHelper helper = helper;
        private readonly DocumentDTOBuildStrategyRegistry registry = registry;
        #endregion

        #region Method
        public void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template, string version, DocumentPath documentPath, Dictionary<string, KeyValueAnchors> anchorMap, RenderDepth depth, string typeName = "")
        {
            List<MetaTypeEditorFieldDTO> resolvedList = [];
            if (target.Children is not null)
            {
                resolvedList.AddRange(target.Children);
            }
            if (target.SelectedUnionChildren is not null)
            {
                resolvedList.AddRange(target.SelectedUnionChildren);
            }
            resolvedList.RemoveAll(MCDocumentMetaTypeDTOHelper.IsPlaceHolderNode);
            if (resolvedList.Count == 0)
            {
                return;
            }

            #region 可选字段留给展开时物化
            if (!target.IsRequired)
            {
                target.Value = new ObservableCollection<MetaTypeEditorFieldDTO>(resolvedList);
                target.Children = [new MetaTypeEditorFieldDTO() { ID = "placeHolder", TypeKind = MetaTypeKind.Any }];
                target.SelectedUnionChildren?.Clear();
                return;
            }
            #endregion

            #region 必选字段立刻物化
            target.Children = new(resolvedList);
            target.SelectedUnionChildren?.Clear();
            foreach (MetaTypeEditorFieldDTO resolved in resolvedList)
            {
                MCDocumentResourceBuilder.BaseDataHandler(resolved);
                registry.Get(resolved.TypeKind).Build(resolved, resolved, version, resolved.Path ?? documentPath, anchorMap, depth, typeName);
            }
            #endregion
        }

        public bool CanHandle(MetaTypeKind kind)
        {
            return kind is MetaTypeKind.Dispatch;
        }
        #endregion
    }
}
