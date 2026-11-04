using System.Collections.Generic;
using System.Linq;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// 节点表现器：misode McdocRenderer 里 Head/Body 两组组件函数的 OO 形态。
    /// 所有按 McdocTypeKind 的分派都收敛在这里与子类，投影层、行、模板选择器不再判断类型。
    /// </summary>
    public abstract class McdocNodePresenter
    {
        /// <summary>union 下拉里的"未设置"项（misode 的 unset）。</summary>
        public const string UnsetLabel = "（未设置）";
        public abstract bool CanPresent(McdocType type);

        public abstract McdocNodeHead Describe(McdocTreeNode node);

        /// <summary>
        /// 行自身状态的准备（候选值 / 枚举项 / JSON 值摊到 Value）。
        /// 由工厂在**建完节点后无条件调用**——懒建不会展开子级，但行必须一出生就是对的控件。
        /// </summary>
        public virtual void Prepare(McdocTreeNode node, McdocType type, McdocJsonValue json, McdocBuildContext context)
        {
        }

        /// <summary>按类型展开子级（misode 的 Body）。工厂只调用这里，不再自己 switch 类型。</summary>
        public virtual void PopulateChildren(McdocTreeNode node, McdocType type, McdocJsonValue json, McdocBuildContext context)
        {
        }

        /// <summary>子行用下标做标签（列表 / 元组），投影层不再自己判断类型。</summary>
        public virtual bool IsListLike => false;

        /// <summary>根节点是否摊平子级（misode McdocRoot 的 struct 分支）。</summary>
        public virtual bool FlattensRoot => false;

        /// <summary>union 切分支时把编辑状态镜像到行上（misode 的 UnionHead + 标量分支合并）。</summary>
        public virtual void ApplyBranch(McdocTreeNode node, McdocTreeNode branchNode)
        {
        }

        /// <summary>撤销刚建好的分支（切到"解不出来"的成员时用）：清分支子级与镜像状态。</summary>
        public virtual void ClearBranch(McdocTreeNode node)
        {
        }

        /// <summary>是否可能有子级（懒建模式下展开按钮据此显示，不能靠 Count）。</summary>
        public virtual bool CanHaveChildren(McdocTreeNode node) => true;

        /// <summary>该节点的子行来源（union 走分支、列表走 Items、其余走 Children）。</summary>
        public virtual IReadOnlyList<McdocTreeNode> Children(McdocTreeNode node) => node.Children;

        /// <summary>列表子行的标签：默认下标，元组覆盖成 entry（行/投影层不再判类型）。</summary>
        /// <summary>列表子行固定叫 Entry（不挂序号）：序号只是位置，用户加元素时看到的是同一个名字。</summary>
        public virtual string ChildLabel(McdocTreeNode parent, int index) => "Entry";

        /// <summary>节点上的诊断（求值失败等）→ 行上的 Errors 指示。</summary>
        /// <summary>任意 JSON 的推断类型短名（string/int/object/list...）。</summary>
        protected static string TypeHintFor(McdocTreeNode node)
        {
            if (node?.Type?.Kind != McdocTypeKind.Any) return "";
            var inferred = node.Json?.InferType();
            if (inferred is null) return "";
            return inferred.Kind switch
            {
                McdocTypeKind.Struct => "object",
                McdocTypeKind.List => "list",
                McdocTypeKind.Literal => inferred.LiteralKind.ToString().ToLowerInvariant(),
                _ => inferred.Kind.ToString().ToLowerInvariant(),
            };
        }

        /// <summary>#[color] 的形态；只认声明类型或求值类型上带的属性。</summary>
        protected static string ColorKindFor(McdocTreeNode node)
        {
            foreach (var type in new[] { node?.DeclaredType, node?.Type })
            {
                var attribute = type?.Attributes?.FirstOrDefault(item => item?.Name == "color");
                var kind = attribute?.Value?.AsString();
                if (!string.IsNullOrEmpty(kind)) return kind;
            }
            return "";
        }

        /// <summary>
        /// 动态键 / map 条目：标签必须保持原文。用户自己起的名字（sdgsdfhgd）不许过 formatIdentifier，
        /// 否则会被拆词 + 首字母大写（Sdgsdfhgd），行上看到的键就和 JSON 里的键对不上了。
        /// </summary>
        protected static bool IsRawKeyNode(McdocTreeNode node)
            => node is not null && (node.IsDynamicKey || node.IsMapEntry);

        /// <summary>
        /// 可选字段的"⊕ 设默认值"：只给结构行。标量的落点是它自己的编辑器、列表靠 add、
        /// 联合靠下拉里的 unset —— 给它们加 ⊕ 会变成"点了没反应"的死按钮。
        /// </summary>
        protected static bool CanSetDefaultField(McdocTreeNode node)
            => node is not null && node.Json is null && node.Parent is not null && !node.IsRequired
               && !node.IsDynamicKey && !node.IsMapEntry;

        /// <summary>可选字段的删除入口：有值、且文档里确实有这个键（删得掉才给按钮）。</summary>
        protected static bool CanRemoveField(McdocTreeNode node)
            => node is not null && node.Json is not null && !node.IsRequired
               && !node.IsDynamicKey && !node.IsMapEntry
               && node.RemoveFieldCommand?.CanExecute(null) == true;

        protected static IReadOnlyList<string> DiagnosticsFor(McdocTreeNode node)
        {
            var list = new List<string>();
            if (!string.IsNullOrEmpty(node.Error)) list.Add(node.Error);
            if (node.Diagnostics is { Count: > 0 }) list.AddRange(node.Diagnostics);
            return list.Count == 0 ? null : list;
        }

        /// <summary>空值占位（旧管线拿字段名当水印，这里给类型提示，对应 misode 的 placeholder）。</summary>
        protected static string PlaceholderFor(McdocTreeNode node)
        {
            if (!string.IsNullOrEmpty(node?.Watermark)) return node.Watermark;
            var type = node?.Type ?? node?.DeclaredType;
            if (type is null || type.Kind is McdocTypeKind.Enum or McdocTypeKind.Union) return "";
            if (ColorKindFor(node) == "hex_rgb") return "#rrggbb";
            var registry = McdocSelectOptions.Resolve(type)?.Registry;
            if (!string.IsNullOrEmpty(registry)) return registry;
            return type.Kind switch
            {
                McdocTypeKind.String => "string",
                McdocTypeKind.Byte or McdocTypeKind.Short or McdocTypeKind.Int or McdocTypeKind.Long => "integer",
                McdocTypeKind.Float or McdocTypeKind.Double => "number",
                McdocTypeKind.Boolean => "true / false",
                _ => "",
            };
        }

        /// <summary>物品/方块 id 字段：走带图标的下拉框。</summary>
        protected static bool IsIconSelectFor(McdocTreeNode node)
        {
            // misode 的 StringHead 拿的是 simplify 之后的类型：只要最终是 string（Reference 本该被解析成 string），
            // 且 #[id] 是 item/block，就给图标位（McdocRenderer.tsx:209）。未解析的 Reference 也算——
            // 候选值已经由 McdocSelectOptions 填过，图标位与候选同源。
            if (node?.Type?.Kind is not (McdocTypeKind.String or McdocTypeKind.Enum or McdocTypeKind.Reference)) return false;
            var registry = McdocSelectOptions.Resolve(node.DeclaredType ?? node.Type)?.Registry;
            return registry is "item" or "block";
        }

        protected static McdocRowKind RowKindFor(McdocTreeNode node)
            => node.IsDynamicKey
                ? (node.Json is null ? McdocRowKind.Placeholder : McdocRowKind.Keyed)
                : node.IsMapEntry ? McdocRowKind.Keyed : McdocRowKind.Plain;

        protected static McdocEditorKind EditorKindFor(McdocType type, bool useSelect)
            => type?.Kind switch
            {
                McdocTypeKind.String or McdocTypeKind.Reference => useSelect ? McdocEditorKind.Enum : McdocEditorKind.Text,
                McdocTypeKind.Enum => McdocEditorKind.Enum,
                McdocTypeKind.Boolean => McdocEditorKind.Bool,
                McdocTypeKind.Byte or McdocTypeKind.Short or McdocTypeKind.Int
                    or McdocTypeKind.Long or McdocTypeKind.Float or McdocTypeKind.Double => McdocEditorKind.Number,
                McdocTypeKind.Literal => McdocEditorKind.Enum,
                McdocTypeKind.Any or McdocTypeKind.Unsafe => McdocEditorKind.Raw,
                _ => McdocEditorKind.None,
            };

        /// <summary>union 把标量分支镜像到行上时会写 ScalarEditorKind，这里统一还原成枚举。</summary>
        protected static McdocEditorKind MirroredEditorKind(McdocTreeNode node)
            => string.IsNullOrEmpty(node.ScalarEditorKind)
                ? McdocEditorKind.None
                : node.ScalarEditorKind switch
                {
                    "Text" => McdocEditorKind.Text,
                    "Number" => McdocEditorKind.Number,
                    "Bool" => McdocEditorKind.Bool,
                    "Enum" => McdocEditorKind.Enum,
                    _ => McdocEditorKind.None,
                };
    }
}