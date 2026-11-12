using System.Collections.Generic;
using System.Linq;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>presenter 注册表：类型 → 表现器的唯一解析点（misode Head/Body 的 if 链，收敛成有序策略表）。</summary>
    public sealed class McdocPresenterRegistry
    {
        public static McdocPresenterRegistry Default { get; } = new(
        [
            new UnionNodePresenter(),
            new StructNodePresenter(),
            new ListNodePresenter(),
            new TupleNodePresenter(),
            new AnyNodePresenter(),
            new ScalarNodePresenter(),
        ]);

        private readonly IReadOnlyList<McdocNodePresenter> presenters;

        public McdocPresenterRegistry(IReadOnlyList<McdocNodePresenter> presenters) => this.presenters = presenters;

        /// <summary>未声明键（版本更迭后遗留）：只按节点分派，不参与类型竞争。</summary>
        private static readonly UnknownFieldPresenter UnknownFields = new();

        public McdocNodePresenter For(McdocType type)
            => presenters.FirstOrDefault(presenter => presenter.CanPresent(type)) ?? presenters[^1];

        /// <summary>按节点解析表现器：未声明键走专用表现器，其余仍按求值类型分派。</summary>
        public McdocNodePresenter For(McdocTreeNode node)
            => node?.IsUnknownField == true ? UnknownFields : For(node?.Type);

        public McdocNodeHead Describe(McdocTreeNode node) => For(node).Describe(node);

        public IReadOnlyList<McdocTreeNode> Children(McdocTreeNode node) => For(node).Children(node);

        public string ChildLabel(McdocTreeNode parent, int index) => For(parent).ChildLabel(parent, index);

        public bool IsListLike(McdocTreeNode node) => For(node).IsListLikeFor(node) || node?.UnionListLike == true;

        /// <summary>有没有子级：懒建时不能数 Children.Count（那时子级还没建）。</summary>
        public bool CanHaveChildren(McdocTreeNode node) => For(node).CanHaveChildren(node);

        /// <summary>union 切分支：路由到 union presenter（节点的状态机交给策略，不在节点里 switch）。</summary>
        public void ApplyBranch(McdocTreeNode node, McdocTreeNode branch)
            => presenters.OfType<UnionNodePresenter>().FirstOrDefault()?.ApplyBranch(node, branch);

        /// <summary>撤销分支：同样路由到 union presenter。</summary>
        public void ClearBranch(McdocTreeNode node)
            => presenters.OfType<UnionNodePresenter>().FirstOrDefault()?.ClearBranch(node);
    }
}
