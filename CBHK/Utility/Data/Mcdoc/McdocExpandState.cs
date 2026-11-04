using System;
using System.Collections.Generic;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// misode useToggles 的等价物：折叠状态按"字段路径"记在页面级，而不是挂在节点上。
    /// 节点每次重建都会新建，所以不记忆就会在编辑后全部回到默认展开。
    /// </summary>
    public static class McdocExpandState
    {
        private static readonly Dictionary<string, bool> States = new(StringComparer.Ordinal);
        private static bool? all;
        private static string session;
        private static object owner;

        public static void Reset()
        {
            States.Clear();
            all = null;
            session = null;
            owner = null;
        }

        /// <summary>
        /// 进入某个页面入口的会话。入口变了才清空：同一页面内的重建（改值、加键、版本切换）
        /// 必须保留折叠状态（misode 的 useToggles 语义），而换页面不能让上一个页面的状态串过来。
        /// </summary>
        public static void BeginSession(string entryId, object sessionOwner)
        {
            var key = entryId ?? string.Empty;
            // 换入口、或换了页面实例（关窗体再开）都必须清空：静态表跨窗口串状态，
            // 会让"文档里根本没有的字段"在新会话里带着旧的展开态被撑开（子级凭空出现、看着像摊到同层）
            if (string.Equals(session, key, StringComparison.Ordinal) && ReferenceEquals(owner, sessionOwner)) return;
            session = key;
            owner = sessionOwner;
            States.Clear();
            all = null;
        }

        /// <summary>Ctrl+点击"全部展开/折叠"（misode toggleAll）：清掉逐行记录，统一个默认值。</summary>
        public static void SetAll(bool? value)
        {
            States.Clear();
            all = value;
        }

        public static bool? Lookup(string path)
            => !string.IsNullOrEmpty(path) && States.TryGetValue(path, out var expanded) ? expanded : all;

        public static void Set(string path, bool expanded)
        {
            if (string.IsNullOrEmpty(path)) return;
            States[path] = expanded;
        }
    }
}
