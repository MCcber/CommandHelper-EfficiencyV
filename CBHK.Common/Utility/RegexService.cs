using System.Text.RegularExpressions;

namespace CBHK.Common.Utility
{
    public static partial class RegexService
    {
        [GeneratedRegex(@"[-+]?\d+")]
        public static partial Regex GetIntegerData();
        [GeneratedRegex(@"(?<=\s*\s?\*+;?\s*\s?(如果|若|当)).+(?=为|是).+")]
        public static partial Regex GetEnumRawKey();
        [GeneratedRegex(@"\[\[\#?((?<1>[\u4e00-\u9fff]+)\|(?<2>[\u4e00-\u9fff]+)|(?<1>[\u4e00-\u9fff]+))\]\]")]
        public static partial Regex GetContextKey();

        [GeneratedRegex(@"^\s*\s?\:?\s*\s?(\*+)")]
        public static partial Regex GetLineStarCount();

        [GeneratedRegex(@"(?<=<code>)(?<1>[a-z_]+)(?=</code>)")]
        public static partial Regex GetEnumValueMode1();

        [GeneratedRegex(@"\{\{cd\|(?<1>[a-z:_]+)\}\}", RegexOptions.IgnoreCase)]
        public static partial Regex GetEnumValueMode2();

        [GeneratedRegex(@"默认为\{\{cd\|(?<1>[a-z_]+)\}\}", RegexOptions.IgnoreCase)]
        public static partial Regex GetDefaultStringValue();

        [GeneratedRegex(@"默认为(?<1>\d)+", RegexOptions.IgnoreCase)]
        public static partial Regex GetDefaultNumberValue();

        [GeneratedRegex(@"默认为\{\{cd\|(?<1>true|false)\}\}", RegexOptions.IgnoreCase)]
        public static partial Regex GetDefaultBoolValue();

        [GeneratedRegex(@"executeOptions")]
        public static partial Regex ExecuteOptionsCount();

        [GeneratedRegex(@"(?<={)[a-zA-Z]+(?=})")]
        public static partial Regex ScoreboardInlineType();

        [GeneratedRegex(@"[a-zA-Z{}0-9$*]+")]
        public static partial Regex InputContent();

        [GeneratedRegex(@"[a-zA-Z0-9\:\._\-+]")]
        public static partial Regex IsWord();

        [GeneratedRegex(@"\s*\s?\*+\s*\s?{{")]
        public static partial Regex JudgeHead();
        [GeneratedRegex(@"{{slink\|\|(?<1>[\u4e00-\u9fff]+)}}")]
        public static partial Regex GetSlinkData();
        [GeneratedRegex(@"\s?\s*\*+\s?\s*{{([nN]bt\s)?inherit(?<1>[a-z_/|=*\s]+)}}\s?\s*", RegexOptions.IgnoreCase)]
        public static partial Regex GetInheritString();

        [GeneratedRegex(@"(其余的附加条件，)?取决于{{nbt\|string\|(?<1>[a-z_:]+)}}的值")]
        public static partial Regex GetExtraKey();

        [GeneratedRegex(@"(?<=与).+(?=不能同时存在)")]
        public static partial Regex GetMutexKey();

        [GeneratedRegex(@"\[\[(?<1>[a-zA-Z_\u4e00-\u9fff|#]+)\]\]")]
        public static partial Regex GetEnumKey();

        [GeneratedRegex(@"{{interval(\|left=(?<1>\d+))?(\|right=(?<2>\d+))?}}", RegexOptions.IgnoreCase)]
        public static partial Regex GetNumberRange();

        [GeneratedRegex(@"required=1", RegexOptions.IgnoreCase)]
        public static partial Regex GetRequiredKey();

        [GeneratedRegex(@"(?<=默认为<code>)(?<1>[a-z:_]+)(?=</code>)", RegexOptions.IgnoreCase)]
        public static partial Regex GetDefaultEnumValue();

        [GeneratedRegex(@"<''(?<1>[a-z_\u4e00-\u9fff]+)''>", RegexOptions.IgnoreCase)]
        public static partial Regex GetEnumValueMode3();

        [GeneratedRegex(@"\s?\s*=+\s?\s*([#\u4e00-\u9fffa-z_/]+)\s?\s*=+\s?\s*", RegexOptions.IgnoreCase)]
        public static partial Regex GetContextFileMarker();

        [GeneratedRegex(@"=+\s*\s?((minecraft\:)?[a-z_/]+)\s*\s?=+", RegexOptions.IgnoreCase)]
        public static partial Regex GetEnumTypeKeywords();

        [GeneratedRegex(@"(?<=包版本-)[0-9]+")]
        public static partial Regex PackVersionComparer();

        [GeneratedRegex(@"(?<={)[0-9\-]+(?=})")]
        public static partial Regex ItemSlotMatcher();

        [GeneratedRegex(@"(?<=Command:\\\"")([\w \:""\[\]'()!@#$%^&*\-=+\\|;,./?<>`~]*)+(?=\\\"")")]
        public static partial Regex GetCommand();
    }
}
