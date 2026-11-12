using System;

namespace CBHK.Utility.Data
{
    /// <summary>
    /// 版本区间判定：给一个区间字符串和一个目标版本号，回答"目标在不在区间里"。
    /// 比较沿用 <see cref="VersionFilter"/> 的口径（Major/Minor/Build/Revision 逐段，未指定段按 0），
    /// 但它只做单点判定，不涉及 since/until 的可见性语义。
    /// 支持的写法："1.20.5"（单版本，精确相等）、"1.20-1.21"（闭区间，两端都含）、"1.13+"（上不封顶）、
    /// "-1.21"（左端缺省，只有上界）、"1.20-"（右端缺省，只有下界）。
    /// 空区间（含 "-" 与 "+"）视为无约束 → true；区间或目标无法解析 → false（宁可判不中，不静默放行）。
    /// 另提供 <see cref="IsGreaterThan"/> / <see cref="IsLessThan"/>：两个单版本号的严格大小比较（相等不算），
    /// 带 "+" 的写法按它的下界参与比较。
    /// </summary>
    public static class VersionRangeChecker
    {
        public static bool IsInRange(string versionRange, string targetVersion)
        {
            if (!TryParse(targetVersion, out Version target)) return false;
            if (string.IsNullOrWhiteSpace(versionRange)) return true;

            string range = versionRange.Trim();

            // "1.13+"：只有下界；单独的 "+" 两端都不限
            if (range.EndsWith('+'))
            {
                string lowerText = range[..^1].Trim();
                if (lowerText.Length == 0) return true;
                return TryParse(lowerText, out Version lowerBound) && Compare(target, lowerBound) >= 0;
            }

            string[] parts = range.Split('-');

            // "1.20.5"：单版本，精确相等
            if (parts.Length == 1)
            {
                return TryParse(parts[0], out Version exact) && Compare(target, exact) == 0;
            }

            if (parts.Length != 2) return false;

            string leftText = parts[0].Trim();
            string rightText = parts[1].Trim();

            // "-"：两端都缺，等同无约束
            if (leftText.Length == 0 && rightText.Length == 0) return true;

            // "-1.21"：左端缺省，只有上界（含）
            if (leftText.Length == 0)
            {
                return TryParse(rightText, out Version upperBound) && Compare(target, upperBound) <= 0;
            }

            // "1.20-"：右端缺省，只有下界（含）
            if (rightText.Length == 0)
            {
                return TryParse(leftText, out Version lowerOnly) && Compare(target, lowerOnly) >= 0;
            }

            // "1.20-1.21"：闭区间（与 VersionFilter 把用户区间右端当作包含端一致）
            if (!TryParse(leftText, out Version rangeLeft) || !TryParse(rightText, out Version rangeRight)) return false;
            return Compare(target, rangeLeft) >= 0 && Compare(target, rangeRight) <= 0;
        }

        /// <summary>判断 <paramref name="version"/> 是否严格大于 <paramref name="targetVersion"/>（相等不算大于）。</summary>
        public static bool IsGreaterThan(string version, string targetVersion)
        {
            return TryCompare(version, targetVersion, out int comparison) && comparison > 0;
        }

        /// <summary>判断 <paramref name="version"/> 是否严格小于 <paramref name="targetVersion"/>（相等不算小于）。</summary>
        public static bool IsLessThan(string version, string targetVersion)
        {
            return TryCompare(version, targetVersion, out int comparison) && comparison < 0;
        }

        /// <summary>两个单版本号比较；任一侧解析不了 → 返回 false（调用方一律判不中）。</summary>
        private static bool TryCompare(string version, string targetVersion, out int comparison)
        {
            comparison = 0;
            if (!TryParse(version, out Version left) || !TryParse(targetVersion, out Version right)) return false;
            comparison = Compare(left, right);
            return true;
        }

        private static bool TryParse(string text, out Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(text)) return false;

            // "1.13+" 这类写法单独参与比较时取它的下界
            string trimmed = text.Trim();
            if (trimmed.EndsWith('+')) trimmed = trimmed[..^1].Trim();

            return trimmed.Length > 0 && Version.TryParse(trimmed, out version);
        }

        private static int Compare(Version a, Version b)
        {
            int cmp = a.Major.CompareTo(b.Major);
            if (cmp != 0) return cmp;

            cmp = a.Minor.CompareTo(b.Minor);
            if (cmp != 0) return cmp;

            int aBuild = a.Build == -1 ? 0 : a.Build;
            int bBuild = b.Build == -1 ? 0 : b.Build;
            cmp = aBuild.CompareTo(bBuild);
            if (cmp != 0) return cmp;

            int aRevision = a.Revision == -1 ? 0 : a.Revision;
            int bRevision = b.Revision == -1 ? 0 : b.Revision;
            return aRevision.CompareTo(bRevision);
        }
    }
}
