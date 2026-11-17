using System;
using System.Collections.Generic;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// 「新键」下拉的选中记忆，和 McdocExpandState 一样按字段路径记在页面级：
    /// 节点每次重建都是新实例，挂在节点上活不过一次刷新。
    /// 语义：用 ⊕ 起过某个键之后，占位下拉落到它的下一位成员，而不是空白。
    /// </summary>
    public static class McdocKeyMemory
    {
        private static readonly Dictionary<string, string> LastKeys = new(StringComparer.Ordinal);

        public static void Reset() => LastKeys.Clear();

        public static void Remember(string containerPath, string key)
        {
            if (string.IsNullOrEmpty(containerPath) || string.IsNullOrEmpty(key)) return;
            LastKeys[containerPath] = key;
        }

        public static string Lookup(string containerPath)
            => !string.IsNullOrEmpty(containerPath) && LastKeys.TryGetValue(containerPath, out var key) ? key : null;
    }
}
