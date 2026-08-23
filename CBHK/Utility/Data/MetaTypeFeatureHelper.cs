using MinecraftLanguageModelLibrary.Data;
using System.Collections.Generic;
using System.Linq;

namespace CBHK.Utility.Data
{
    /// <summary>
    /// FeatureMap 特征判定工具（自 MCDocumentMetaTypeDTOHelper 抽出）
    /// </summary>
    public static class MetaTypeFeatureHelper
    {
        /// <summary>
        /// 判定是否为需要用户手写的定义类节点
        /// </summary>
        public static bool IsDefinitionItem(Dictionary<string, MetaValue> featureMap)
        {
            bool result = false;
            if (featureMap is not null && featureMap.Count > 0)
            {
                var targetTuplePairList = featureMap.Where(item => item.Value.Kind is MetaValueKind.Tuple);
                List<List<MetaNamedValue>> targetMemberList = [.. targetTuplePairList.Select(item => item.Value.Members)];
                for (int i = 0; i < targetMemberList.Count; i++)
                {
                    //有definition的同时不能有registry，那么就是需要用户手写的定义类节点
                    result = !targetMemberList[i].Any(item => item.Name == "registry" && item.Value?.TypeValue?.LiteralValue is not null) && targetMemberList[i].Any(item => item.Name == "definition" && item.Value?.TypeValue?.LiteralValue is not null && item.Value.TypeValue.LiteralValue.ToString() == "True");
                    if (result)
                    {
                        break;
                    }
                }
            }
            return result;
        }
    }
}
