using System;
using System.Collections.Generic;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// 一次子级构建的现场。原来 CreateNode/BuildChildren 之间传的一长串参数收敛在这里，
    /// 表现器只依赖这个对象，不再各自复制签名。
    /// </summary>
    public sealed class McdocBuildContext
    {
        public McdocRuntime Runtime;
        public McdocNodeFactory Factory;
        public McdocPresenterRegistry Registry = McdocPresenterRegistry.Default;
        public McdocValueNode Self;
        public McdocRuntimeNode KeyNode;
        public int Depth;
        public bool Narrow;
        public string JsonPath;
        public System.Collections.Generic.List<McdocType> Visiting;
        internal McdocNodeBudget Budget;
        public McdocSimplifyContext SimplifyContext;
        public McdocCollectionEditor Editor = McdocCollectionEditor.Default;

        /// <summary>
        /// 建一个子节点。<paramref name="deferChildren"/> = "这个子级是缺省成员，别在饿建期把它的子结构建出来"：
        /// 必须在**这里**由父级说，不许回看 <c>node.Parent</c> —— 饿建期父级还没把 Parent 写上去，
        /// 表现器的"缺省字段不摊开"守卫在这条路上恒为 false（见 README 的踩坑清单）。
        /// </summary>
        public McdocTreeNode Child(string name, McdocType declaredType, McdocJsonValue json,
            McdocRuntimeNode keyNode, int depth, string jsonPath, bool deferChildren = false, bool unknownField = false)
            => Factory.CreateNode(name, declaredType, json, Runtime, Self, keyNode, depth, Narrow,
                OnValueChanged, jsonPath, Visiting, OnStructureChanged, Budget, deferChildren, unknownField);

        public Action<McdocTreeNode> OnValueChanged;
        public Action<McdocTreeNode> OnStructureChanged;
    }
}
