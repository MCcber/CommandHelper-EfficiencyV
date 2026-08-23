using MinecraftLanguageModelLibrary.Data;
using System;
using System.Collections.Generic;

namespace CBHK.Utility.Data
{
    /// <summary>
    /// 版本过滤（自 MCDocumentMetaTypeDTOHelper 抽出）：
    /// 根据 since/until 约束和用户目标版本决定节点是否可见。
    /// since 为包含下限（≥），until 为排除上限（&lt;）。
    /// 用户版本可以是单版本 "1.20.5" 或范围 "1.20-1.21"。
    /// </summary>
    public static class VersionFilter
    {
        public static void VerifyVersion(List<MetaTypeEditorFieldDTO> targetDTOList, string version)
        {
            for (int i = 0; i < targetDTOList.Count; i++)
            {
                if (targetDTOList[i].TypeKind is MetaTypeKind.Dispatch && string.IsNullOrEmpty(targetDTOList[i].FieldName))
                {
                    continue;
                }

                MetaTypeEditorFieldDTO dto = targetDTOList[i];
                dto.IsVisible = false;

                //从FeatureMap提取since/until版本约束
                Version sinceVersion = TryParseFeatureVersion(dto.FeatureMap, "since");
                Version untilVersion = TryParseFeatureVersion(dto.FeatureMap, "until");

                //无任何版本约束则始终可见
                if (sinceVersion is null && untilVersion is null)
                {
                    dto.IsVisible = true;
                    continue;
                }

                //用户版本是范围（如"1.20-1.21"）：区间重叠即视为可见
                if (version.Contains('-'))
                {
                    string[] parts = version.Split('-');
                    if (parts.Length == 2
                        && Version.TryParse(parts[0], out Version rangeLeft)
                        && Version.TryParse(parts[1], out Version rangeRight))
                    {
                        dto.IsVisible = IsVersionRangeOverlapping(rangeLeft, rangeRight, sinceVersion, untilVersion);
                    }
                }
                else if (Version.TryParse(version, out Version fullVersion))
                {
                    dto.IsVisible = IsVersionInRange(fullVersion, sinceVersion, untilVersion);
                }
            }
        }

        /// <summary>
        /// 从FeatureMap提取"since"或"until"版本值
        /// </summary>
        private static Version TryParseFeatureVersion(Dictionary<string, MetaValue> featureMap, string key)
        {
            if (featureMap is not null
                && featureMap.TryGetValue(key, out MetaValue metaValue)
                && metaValue?.TypeValue?.LiteralValue is not null
                && Version.TryParse(metaValue.TypeValue.LiteralValue.ToString(), out Version parsed))
            {
                return parsed;
            }
            return null;
        }

        /// <summary>
        /// 判断单版本是否落在[since, until)区间内。
        /// since为包含（≥），until为排除（&lt;）。
        /// </summary>
        private static bool IsVersionInRange(Version target, Version since, Version until)
        {
            if (since is { } sinceVer && CompareVersions(target, sinceVer) < 0)
                return false;

            if (until is { } untilVer && CompareVersions(target, untilVer) >= 0)
                return false;

            return true;
        }

        /// <summary>
        /// 判断用户版本范围[rangeLeft, rangeRight]是否与字段约束[since, until)有交集。
        /// </summary>
        private static bool IsVersionRangeOverlapping(Version rangeLeft, Version rangeRight, Version since, Version until)
        {
            // 用户区间完全在 until 之后则无交集
            if (until is { } untilVer && CompareVersions(rangeLeft, untilVer) >= 0)
                return false;

            // 用户区间完全在 since 之前则无交集
            if (since is { } sinceVer && CompareVersions(rangeRight, sinceVer) < 0)
                return false;

            return true;
        }

        /// <summary>
        /// 逐段比较两个Version，Build/Revision=-1视为"未指定"（等同0参与比较）。
        /// </summary>
        private static int CompareVersions(Version a, Version b)
        {
            int cmp = a.Major.CompareTo(b.Major);
            if (cmp != 0) return cmp;

            cmp = a.Minor.CompareTo(b.Minor);
            if (cmp != 0) return cmp;

            int aBuild = a.Build == -1 ? 0 : a.Build;
            int bBuild = b.Build == -1 ? 0 : b.Build;
            cmp = aBuild.CompareTo(bBuild);
            if (cmp != 0) return cmp;

            int aRev = a.Revision == -1 ? 0 : a.Revision;
            int bRev = b.Revision == -1 ? 0 : b.Revision;
            return aRev.CompareTo(bRev);
        }
    }
}
