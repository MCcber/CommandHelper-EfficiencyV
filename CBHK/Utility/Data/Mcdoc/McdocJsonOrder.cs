using System;
using System.Collections.Generic;

namespace CBHK.Utility.Data.Mcdoc
{
    /// <summary>
    /// 对象成员的位置：已经写进文档的键按文档自己的顺序（不排序），新写的键按 mcdoc 声明序插到对应位置，
    /// 声明里没有的键（Any / 动态键 / 未知键）追加到末尾。列表项不参与 —— 下标就是它的身份。
    /// </summary>
    public static class McdocJsonOrder
    {
        /// <summary>
        /// 新键插到哪个下标：插在第一个「声明序比它靠后」的现存键之前；声明外的现存键排在末尾，
        /// 新声明键要插在它们之前；没有声明序（Any / 映射表）就是追加到末尾。
        /// </summary>
        public static int InsertIndex(IList<KeyValuePair<string, McdocJsonValue>> properties, string key,
            IReadOnlyList<string> declarationOrder = null)
        {
            if (properties is null) return 0;
            var rank = RankOf(declarationOrder, key);
            if (rank < 0) return properties.Count;
            for (var index = 0; index < properties.Count; index++)
            {
                var other = RankOf(declarationOrder, properties[index].Key);
                if (other < 0) other = int.MaxValue;
                if (other > rank) return index;
            }
            return properties.Count;
        }

        /// <summary>键在声明序里的下标；没有声明序、或键不在声明里返回 -1（= 没有位置，只能追加）。</summary>
        public static int RankOf(IReadOnlyList<string> declarationOrder, string key)
        {
            if (declarationOrder is null || key is null) return -1;
            for (var index = 0; index < declarationOrder.Count; index++)
            {
                if (string.Equals(declarationOrder[index], key, StringComparison.Ordinal)) return index;
            }
            return -1;
        }
    }
}
