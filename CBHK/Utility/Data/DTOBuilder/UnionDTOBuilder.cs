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
    public class UnionDTOBuilder(Resource resource, MCDocumentMetaTypeDTOHelper helper, DocumentDTOBuildStrategyRegistry registry) : IDocumentDTOBuildStrategy
    {
        #region Field
        private readonly Resource resource = resource;
        private readonly MCDocumentMetaTypeDTOHelper helper = helper;
        private readonly DocumentDTOBuildStrategyRegistry registry = registry;
        #endregion

        public void Build(MetaTypeEditorFieldDTO target, MetaTypeEditorFieldDTO template, string version, DocumentPath documentPath, Dictionary<string, KeyValueAnchors> anchorMap, RenderDepth depth, string typeName = "")
        {
            //分支模板可能被落点挪进了桩（渲染集合只留实际子行），构建要用的是模板表：先还原再照常构建
            if (target.Value is ObservableCollection<MetaTypeEditorFieldDTO> branchTemplates)
            {
                target.Children = branchTemplates;
                target.Value = null;
            }
            #region Field
            int index = 0;
            bool isListOrArrayOrValueType = false, isContainerOrReference = true, isUnion = false;
            ResolvedTypeReference realData = new("", default);
            string targetUsePath = string.Empty;
            MetaTypeEditorFieldDTO targetTemplateDTO = null;
            MetaTypeEditorFieldDTO targetDTO = null;
            #endregion

            #region 模板没有分支表时的补建判定
            //模板只是字段定义（没有分支表）时，分支表可能已经在解析阶段落到实例上（定义引用解析的结果）。
            //必选字段还挂着“未设置”项，说明它只是从定义拷来的半成品（分支从未真正构建过），
            //这时按实例分支补建行内宿主，否则节点会停在名称表多一项、切换时落到邻支的状态。
            //可选字段与已定型的联合保持懒形态，补建期间沿用结构体的重入表打断间接递归。
            string branchBuildKey = null;
            if (template.Children is null || template.Children?.Count == 0)
            {
                if (!target.IsRequired || target.UnionTypeNameList?.FirstOrDefault()?.Name != "- unset -")
                {
                    return;
                }
                branchBuildKey = target.Path?.TargetPath;
                if (string.IsNullOrEmpty(branchBuildKey))
                {
                    branchBuildKey = target.TypeName ?? target.FieldName ?? target.TemplateReference?.Path?.TargetPath;
                }
                if (!string.IsNullOrEmpty(branchBuildKey) && !helper.BuildingStructures.Add(branchBuildKey))
                {
                    return;
                }
            }
            #endregion

            //补建期间占用的重入位必须在所有出口上释放：中途 return 或展开过程中的异常一旦漏掉移除，
            //该路径就被重入表永久锁死，之后同一结构的联合体再也构建不出来
            try
            {
                #region 去除版本之外的节点
                if(target.Children is null || target.Children.Count == 0)
                {
                    return;
                }

                MCDocumentMetaTypeDTOHelper.VerifyVersion([.. target.Children], version);
                for (index = 0; index < target.Children.Count; index++)
                {
                    if (!target.Children[index].IsVisible)
                    {
                        target.Children.RemoveAt(index);
                        index--;
                    }
                }
                #endregion

                #region 仍然是联合体则执行展平
                index = 0;
                do
                {
                    isUnion = index > 0;

                    //搜索真实的路径与DTO实例
                    if (!string.IsNullOrEmpty(target.Children[index].Value?.ToString()))
                    {
                        realData = UsePathParser.Parse(resource, target.Children[index].Path ?? documentPath, target.Children[index].Value.ToString());
                        //解析不到真实文档项时按成员自身处理
                        if (realData?.Item is not null)
                        {
                            targetUsePath = realData.Path;
                            targetTemplateDTO = realData.Item;
                            targetDTO = helper.InstantiateDTO(targetTemplateDTO, version);
                            //新建实例继承上下文：父级、动态 Map 的键载荷、绑定作用域
                            targetDTO.Parent ??= target;
                            targetDTO.Items ??= target.Children[index].Items;
                            targetDTO.BindingScope ??= target.BindingScope;
                        }
                    }

                    //若已提前替换则直接赋值
                    if (target.Children[index].TypeKind is not MetaTypeKind.Literal && targetDTO is null)
                    {
                        targetDTO = target.Children[index];
                        targetUsePath ??= target.Children[index].Path?.ToString();
                    }

                    if (targetDTO is not null)
                    {
                        if (!isListOrArrayOrValueType && MCDocumentMetaTypeDTOHelper.IsListOrArrayOrValueType(targetDTO.TypeKind))
                        {
                            isListOrArrayOrValueType = true;
                        }

                        MCDocumentResourceBuilder.BaseDataHandler(targetDTO);
                        MCDocumentResourceBuilder.BuildResource(targetDTO, targetDTO, version, targetDTO.Path, resource, helper);
                        registry.Get(targetDTO.TypeKind).Build(targetDTO, targetDTO, version, targetDTO.Path ?? documentPath, anchorMap, depth, typeName);

                        if (MCDocumentMetaTypeDTOHelper.IsContainerType(targetDTO.TypeKind))
                        {
                            //容器分支保留字段名，避免被当成展开残余而拍平掉自己的子结构
                            targetDTO.DisplayName = targetDTO.FieldName;
                        }
                        target.Children[index] = targetDTO;

                        //处理子联合体
                        if (targetDTO.TypeKind is MetaTypeKind.Union && targetDTO.Children?.Count > 1)
                        {
                            //被拼接的成员正是父级选中的分支时，选中内容换成内层选中的那一支，
                            //否则父级会一直挂着已经被移除的联合壳
                            if (target.SelectedUnionChildren?.IndexOf(targetDTO) is int selectedIndex && selectedIndex > -1)
                            {
                                target.SelectedUnionChildren[selectedIndex] = targetDTO.SelectedUnionChildren?.FirstOrDefault() ?? targetDTO.Children[0];
                            }
                            target.Children.RemoveAt(index);
                            int insertIndex = index;
                            for (int i = 0; i < targetDTO.Children.Count; i++)
                            {
                                if (!MCDocumentMetaTypeDTOHelper.IsIndirectType(targetDTO.Children[i].TypeKind))
                                {
                                    targetDTO.Children[i].Path = new(targetUsePath);
                                }
                                target.Children.Insert(insertIndex, targetDTO.Children[i]);
                                insertIndex++;
                            }
                            index += targetDTO.Children.Count - 1;
                        }

                        #region 判断是否为容器或引用
                        if (!isContainerOrReference && (MCDocumentMetaTypeDTOHelper.IsContainerType(targetDTO.TypeKind) || MCDocumentMetaTypeDTOHelper.IsIndirectType(targetDTO.TypeKind)) && targetDTO.TypeKind is not (MetaTypeKind.List or MetaTypeKind.ByteArray or MetaTypeKind.IntArray or MetaTypeKind.LongArray))
                        {
                            isContainerOrReference = true;
                        }
                        #endregion
                    }
                    //执行资源解释
                    else
                    {
                        if (!isListOrArrayOrValueType && MCDocumentMetaTypeDTOHelper.IsListOrArrayOrValueType(target.Children[index].TypeKind))
                        {
                            isListOrArrayOrValueType = true;
                        }
                        MCDocumentResourceBuilder.BaseDataHandler(target.Children[index]);
                        MCDocumentResourceBuilder.BuildResource(target.Children[index], target.Children[index], version, target.Children[index].Path, resource, helper);
                    }
                    targetUsePath = string.Empty;
                    targetDTO = null;
                }
                while (++index < target.Children.Count && target.Children.Count > 1);
                #endregion
            }
            finally
            {
                if (!string.IsNullOrEmpty(branchBuildKey))
                {
                    helper.BuildingStructures.Remove(branchBuildKey);
                }
            }

            #region 只剩一支时把唯一成员提升上来
            //版本过滤（#[until]）或声明本身就只留一支时，联合外壳没有意义：界面上只会多出一个没用的
            //分支下拉框（进度条件的 trigger 在 1.20.5 就只剩 trigger_type 这一支），而且枚举被联合包着之后，
            //同级字段驱动的调度器重解析拿不到索引字段名，表现为"选了 trigger 类型但内容不出来"
            if (!isUnion)
            {
                PromoteSoleBranch(target, version, documentPath, anchorMap, depth, typeName);
                return;
            }
            #endregion

            #region 处理默认选中的数据
            target.UnionTypeNameList ??= [];
            target.UnionTypeNameList?.Clear();
            //联合成员先过版本校验：被过滤掉的成员从成员表移除，名称表与 Children 下标才能对齐
            MCDocumentMetaTypeDTOHelper.VerifyVersion([.. target.Children], version);
            int prunedCount = 0;
            for (int i = target.Children.Count - 1; i >= 0; i--)
            {
                if (!target.Children[i].IsVisible && !MCDocumentMetaTypeDTOHelper.IsPlaceHolderNode(target.Children[i]))
                {
                    target.Children.RemoveAt(i);
                    prunedCount++;
                }
            }
            //处理可选节点
            if (!target.IsRequired && (target.UnionTypeNameList?.Count > 0 && target.UnionTypeNameList[0].Name != "- unset -" || target.UnionTypeNameList?.Count == 0))
            {
                target.UnionTypeNameList.Insert(0, new EnumMember() { Name = "- unset -", Value = new() { Kind = MetaValueKind.Literal, LiteralValue = "unset" } });
            }
            //根据成员类型计算所有联合体名称；此时成员往往尚未定型，最终名称由构建收尾处按当前成员重建
            //单成员联合（版本过滤后只剩一支，如 predicate 的 until/since 两支）同样要有名称表，否则下拉框空白
            if (target.Children?.Count > 0)
            {
                //模板已给出名称且成员没被裁减时沿用模板名称：类型兜底名不如模板名准确（可能撞出两个 String）
                bool hasTemplateNames = target.UnionTypeNameList?.Any(item => item.Name != "- unset -") == true;
                if (!hasTemplateNames || prunedCount > 0)
                {
                    //只按可见成员建名称表：被版本过滤掉的成员不能进来，否则名称与 Children 下标会错位
                    List<MetaTypeEditorFieldDTO> visibleChildren = [.. target.Children.Where(child => child.IsVisible)];
                    List<string> unionNameTypeList = UnionTypeNameParser.Parse(visibleChildren);
                    //名称解析不出内容时用成员自身的名字/类型名兜底，保证名称表非空
                    if (unionNameTypeList.Count == 0 && visibleChildren.Count > 0)
                    {
                        unionNameTypeList = [.. visibleChildren.Select(child => !string.IsNullOrEmpty(child.FieldName)
                            ? child.FieldName
                            : child.TypeName ?? child.TypeKind.ToString())];
                    }
                    target.UnionTypeNameList.AddRange([.. unionNameTypeList.Select(item => new EnumMember() { Name = item, Value = new MetaValue() { Kind = MetaValueKind.Literal, LiteralValue = item } })]);
                }
                //模板自带的名称也可能撞车（例如四个分支都取到 String），统一在这里按其它有效特征去重
                List<MetaTypeEditorFieldDTO> disambiguateBranches = [.. target.Children.Where(child => child.IsVisible)];
                List<string> disambiguatedNames = [.. target.UnionTypeNameList.Select(item => item.Name)];
                UnionTypeNameParser.Disambiguate(disambiguatedNames, disambiguateBranches);
                for (int i = 0; i < target.UnionTypeNameList.Count && i < disambiguatedNames.Count; i++)
                {
                    target.UnionTypeNameList[i].Name = disambiguatedNames[i];
                }
            }
            //作为另一个联合体成员时保持联合形态，成员表由父级拼接（如颜色的 Int/List/String 与 modifier 并排）
            bool isUnionMember = target.Parent?.TypeKind is MetaTypeKind.Union;
            //确保联合体节点有默认选中项
            if (!isListOrArrayOrValueType || !isContainerOrReference || isUnionMember)
            {
                //重建时先摘掉旧事件，避免赋默认选中项时触发上一次构建注册的落点逻辑
                target.SelectedUnionItemUpdated = null;
                // 防御：某些动态分支可能被版本过滤或 accessor 裁剪后暂时没有 union 名称，
                // 此时不能直接访问 [0]，否则会抛 ArgumentOutOfRangeException。
                if (target.UnionTypeNameList?.Count > 0)
                {
                    target.SelectedUnionTypeName = target.UnionTypeNameList[0];
                }
                target.SelectedUnionItemUpdated = () => helper.SelectedUnionItemUpdated(target, version);
            }
            //拥有多个子级且至少有一个子级不是容器类型，则将当前节点提升为复合类型，并将第一个子级作为联合体的默认选中项
            else if (isListOrArrayOrValueType)
            {
                #region 分支类型完全相同时直接收敛
                //例如颜色字段 color?: ( #[color="hex_rgb"] string | #[color="named"] TextColor )，
                //两个分支都会命中颜色字典、变成同一个类型；此时字段不需要联合外壳，
                //直接采用该类型自己的编辑形态，字段名与必选性保持不变。
                //调度器结果不能收敛：压平会丢掉 Resource/Index 特征，索引联动时就无法再解析
                bool isSameLeafKind = target.OriginKind is not MetaTypeKind.Dispatch
                    && target.Children[0].Children is null
                    && target.Children.All(child => child.TypeKind == target.Children[0].TypeKind)
                    && target.Children[0].TypeKind is MetaTypeKind.NamedColor or MetaTypeKind.String or MetaTypeKind.Int
                        or MetaTypeKind.Float or MetaTypeKind.Boolean or MetaTypeKind.Enum;
                if (isSameLeafKind)
                {
                    string keepFieldName = target.FieldName;
                    bool keepRequired = target.IsRequired;
                    //注解按"就近优先"（mcdoc 语义）：字段自己的注解压过分支的，
                    //CopyFrom 会整份替换 FeatureMap，这里先把字段侧的留一份再合并回去
                    Dictionary<string, MetaValue> keepOwnFeatures = target.FeatureMap is null ? [] : new(target.FeatureMap);
                    target.CopyFrom(target.Children[0]);
                    target.FieldName = keepFieldName;
                    target.SetRequired(keepRequired);
                    //版本门控逐节点独立：分支的 since/until 不跟着搬过来
                    MCDocumentMetaTypeDTOHelper.ApplyOwnFeatures(target, keepOwnFeatures);
                    return;
                }
                #endregion

                target.TypeKind = MetaTypeKind.Composite;

                MetaTypeEditorFieldDTO unionItem = new()
                {
                    //行内选择器由自身模板显示字段名
                    FieldName = target.FieldName,
                    Path = target.Path,
                    ID = Guid.NewGuid().ToString(),
                    TypeKind = MetaTypeKind.Union,
                    Children = target.Children,
                    Parent = target,
                    UnionTypeNameList = target.UnionTypeNameList
                };
                unionItem.SelectedUnionTypeName = unionItem.UnionTypeNameList[0];
                unionItem.SelectedUnionChildren = target.Children[0].Children is not null
                    ? [.. target.Children[0].Children]
                    : [target.Children[0]];
                unionItem.SetRequired(target.IsRequired);
                unionItem.SelectedUnionItemUpdated = () => helper.SelectedUnionItemUpdated(unionItem, version);
                //重建选择器：把上一次的选择器及其后面的内容整体去掉，避免行内区累积
                target.Items ??= [];
                int previousSelectorIndex = target.Items.ToList().FindLastIndex(item => item.TypeKind is MetaTypeKind.Union);
                if (previousSelectorIndex > -1)
                {
                    while (target.Items.Count > previousSelectorIndex)
                    {
                        target.Items.RemoveAt(target.Items.Count - 1);
                    }
                }
                target.Items.Add(unionItem);
                            unionItem.Parent = target;
                //调度器结果的首次落点：选中成员的内容进行内区，与切换分支后的落点保持一致
                if (target.OriginKind is MetaTypeKind.Dispatch)
                {
                    unionItem.SelectedUnionItemUpdated();
                }

                target.SelectedUnionItemUpdated = null;
                target.SelectedUnionChildren ??= [];
                target.SelectedUnionChildren.Clear();
                target.SelectedUnionTypeName = null;
            }

            //处理必选节点（调度器结果的 Composite 已由落点进行内区，不再重复添加）
            bool isDispatchComposite = target.TypeKind is MetaTypeKind.Composite && target.OriginKind is MetaTypeKind.Dispatch;
            if (target.IsRequired && !isDispatchComposite)
            {
                MetaTypeEditorFieldDTO firstBranch = target.Children[0];
                bool isLazyStub = firstBranch.Children?.Count == 1 && MCDocumentMetaTypeDTOHelper.IsPlaceHolderNode(firstBranch.Children[0]);
                if (isLazyStub && helper.TryExpandLazyStub(firstBranch, version))
                {
                    //必选联合默认选中项是懒加载桩时先就地展开：真子级缓存在 Value 里（如动态键 map 的 Tuple），
                    //取回来后键选择器/增删按钮才在初始渲染里可见，否则行内区只有一个空桩
                    target.SelectedUnionChildren = firstBranch.Children is null ? [] : [.. firstBranch.Children];
                }
                else if (isLazyStub)
                {
                    //其它懒加载桩不能当内容挂，挂分支自身，展开时再物化
                    target.SelectedUnionChildren = [firstBranch];
                }
                else if (MCDocumentMetaTypeDTOHelper.IsContainerType(firstBranch.TypeKind))
                {
                    target.SelectedUnionChildren = firstBranch.Children is null ? new ObservableCollection<MetaTypeEditorFieldDTO>() : new ObservableCollection<MetaTypeEditorFieldDTO>(firstBranch.Children);
                }
                else if (target.Items is not null)
                {
                    var firstSubDTO = helper.InstantiateDTO(firstBranch, version);
                    firstSubDTO.FieldName = "";
                    target.Items.Add(firstSubDTO);
                            firstSubDTO.Parent = target;
                }
                else
                {
                    target.SelectedUnionChildren = new([firstBranch]);
                }
            }

            //可选联合停在“未设置”时不能带出成员内容：构建期必须自己收尾，不能只靠切换落点兜底。
            //用名称表首项判定“未设置”，因为 IsRequired 可能晚于联合构建才被写上
            bool isUnsetSelection = MCDocumentMetaTypeDTOHelper.IsUnsetUnionSelection(target);
            if (isUnsetSelection && target.TypeKind is MetaTypeKind.Composite)
            {
                target.SelectedUnionChildren?.Clear();
                //换新集合而不是 Clear：行内选择器与宿主可能共享同一个集合对象，Clear 会清掉它的分支表
                target.Children = [];
                if (target.Items is not null)
                {
                    int selectorTail = target.Items.ToList().FindLastIndex(item => item.TypeKind is MetaTypeKind.Union && item.FieldName == target.FieldName);
                    if (selectorTail > -1)
                    {
                        while (target.Items.Count > selectorTail + 1)
                        {
                            target.Items.RemoveAt(target.Items.Count - 1);
                        }
                    }
                }
            }
            else if (isUnsetSelection && target.TypeKind is MetaTypeKind.Union)
            {
                //普通联合没有转成行内宿主：分支表就在 Children 上，只能清已渲染的内容
                target.SelectedUnionChildren?.Clear();
            }

            #endregion
        }

        /// <summary>
        /// 联合只剩一个可见成员时，把该成员提升到联合节点上：字段名、必选性与父链保持不变，
        /// 成员的类型/子级/候选表照搬过来。事件委托不在 CopyFrom 的搬运范围内，要单独接管。
        /// </summary>
        private void PromoteSoleBranch(MetaTypeEditorFieldDTO target, string version, DocumentPath documentPath,
            Dictionary<string, KeyValueAnchors> anchorMap, RenderDepth depth, string typeName)
        {
            if (target?.Children is not { Count: > 0 })
            {
                return;
            }

            MetaTypeEditorFieldDTO branch = target.Children.FirstOrDefault(child =>
                child is not null && child.IsVisible && !MCDocumentMetaTypeDTOHelper.IsPlaceHolderNode(child));
            if (branch is null || ReferenceEquals(branch, target))
            {
                return;
            }

            string keepFieldName = target.FieldName;
            bool keepRequired = target.IsRequired;
            //字段自身的注解不能被分支的注解覆盖：#[until="1.20"] 这类版本门控属于字段，
            //分支的 #[since] 属于分支。不保留的话字段的 until 会被抹掉，版本过滤就失效
            //（advancement 触发器 item?: ItemPredicate 在 1.20.5 本该被过滤却又显示出来）
            Dictionary<string, MetaValue> keepOwnFeatures = target.FeatureMap is null ? [] : new(target.FeatureMap);
            EnumMember keepSelectedOption = branch.SelectedEnumOption;
            Action keepEnumUpdated = branch.SelectedEnumItemUpdated;
            Action keepUnionUpdated = branch.SelectedUnionItemUpdated;

            //分支表属于联合外壳：先丢掉，CopyFrom 只在成员自带子级/行内项时才回填，
            //否则提升出来的枚举会把整张分支表当成自己的子级留着
            target.Children = null;
            target.Items = null;
            target.CopyFrom(branch);
            target.FieldName = keepFieldName;
            target.SetRequired(keepRequired);
            //版本门控逐节点独立：分支的 since/until 不跟着搬过来
            MCDocumentMetaTypeDTOHelper.ApplyOwnFeatures(target, keepOwnFeatures);
            //联合外壳被提升掉了：名称表与选中表都不再有意义
            target.UnionTypeNameList = null;
            target.SelectedUnionTypeName = null;
            target.SelectedUnionChildren = null;

            if (target.TypeKind is MetaTypeKind.Enum)
            {
                //先摆好选中项再挂事件：SelectedEnumOption 首次赋值不触发处理，不会白跑一次重解析
                if (target.SelectedEnumOption is null && keepSelectedOption is not null)
                {
                    target.SelectedEnumOption = keepSelectedOption;
                }
                target.SelectedEnumItemUpdated = keepEnumUpdated ?? (() => helper.SelectedEnumItemUpdated(target, version));
                target.SelectedUnionItemUpdated = null;
            }
            else if (target.TypeKind is MetaTypeKind.Union)
            {
                target.SelectedUnionItemUpdated = keepUnionUpdated ?? (() => helper.SelectedUnionItemUpdated(target, version));
            }
            else
            {
                target.SelectedUnionItemUpdated = null;
            }

            //提升出来的是成员的原始模板（联合外壳只负责选分支），按各自动作补建一遍，
            //否则目标内部的名字载体解析不到、会停在字面量化的单选项枚举（progress 条件里 player: CompositeEntity）
            //子级里可能还有单分支联合，直接重入联合构建器会无限递归，用重入表兜住
            string promoteBuildKey = target.Path?.TargetPath ?? target.FieldName;
            bool holdsPromoteKey = !string.IsNullOrEmpty(promoteBuildKey) && helper.BuildingStructures.Add(promoteBuildKey);
            try
            {
                if (!string.IsNullOrEmpty(promoteBuildKey) && !holdsPromoteKey)
                {
                    return;
                }
                foreach (MetaTypeEditorFieldDTO child in target.Children?.ToList() ?? [])
                {
                    if (child is null || MCDocumentMetaTypeDTOHelper.IsPlaceHolderNode(child) || !child.IsVisible)
                    {
                        continue;
                    }
                    DocumentPath childDocumentPath = child.Path ?? target.Path ?? documentPath;
                    MCDocumentResourceBuilder.BaseDataHandler(child);
                    MCDocumentResourceBuilder.BuildResource(child, child, version, childDocumentPath, resource, helper);
                    registry.Get(child.TypeKind).Build(child, child, version, child.Path ?? childDocumentPath, anchorMap, depth, typeName);
                }
            }
            finally
            {
                if (holdsPromoteKey)
                {
                    helper.BuildingStructures.Remove(promoteBuildKey);
                }
            }

            //提升后子级的父链要指向本节点：同级字段驱动的调度器重解析依赖它
            MCDocumentMetaTypeDTOHelper.AttachParentLinks(target);
        }

        public bool CanHandle(MetaTypeKind kind)
        {
            return kind is MetaTypeKind.Union;
        }
    }
}



