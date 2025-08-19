using System.Text.RegularExpressions;

namespace CBHK.Common.Utility
{
    public partial class RegexService
    {
        [GeneratedRegex(@"(?<=\s*\s?\*+;?\s*\s?(如果|若|当)).+(?=为|是).+")]
        public partial Regex GetEnumRawKey();
        [GeneratedRegex(@"\[\[\#?((?<1>[\u4e00-\u9fff]+)\|(?<2>[\u4e00-\u9fff]+)|(?<1>[\u4e00-\u9fff]+))\]\]")]
        public partial Regex GetContextKey();

        [GeneratedRegex(@"^\s*\s?\:?\s*\s?(\*+)")]
        public partial Regex GetLineStarCount();

        [GeneratedRegex(@"(?<=<code>)(?<1>[a-z_]+)(?=</code>)")]
        public partial Regex GetEnumValueMode1();

        [GeneratedRegex(@"\{\{cd\|(?<1>[a-z:_]+)\}\}", RegexOptions.IgnoreCase)]
        public partial Regex GetEnumValueMode2();

        [GeneratedRegex(@"默认为\{\{cd\|(?<1>[a-z_]+)\}\}", RegexOptions.IgnoreCase)]
        public partial Regex GetDefaultStringValue();

        [GeneratedRegex(@"默认为(?<1>\d)+", RegexOptions.IgnoreCase)]
        public partial Regex GetDefaultNumberValue();

        [GeneratedRegex(@"默认为\{\{cd\|(?<1>true|false)\}\}", RegexOptions.IgnoreCase)]
        public partial Regex GetDefaultBoolValue();

        [GeneratedRegex(@"executeOptions")]
        public partial Regex ExecuteOptionsCount();

        [GeneratedRegex(@"(?<={)[a-zA-Z]+(?=})")]
        public partial Regex ScoreboardInlineType();

        [GeneratedRegex(@"[a-zA-Z{}0-9$*]+")]
        public partial Regex InputContent();

        [GeneratedRegex(@"[a-zA-Z0-9\:\._\-+]")]
        public partial Regex IsWord();

        [GeneratedRegex(@"\s*\s?\*+\s*\s?{{")]
        public partial Regex JudgeHead();
        [GeneratedRegex(@"{{slink\|\|(?<1>[\u4e00-\u9fff]+)}}")]
        public partial Regex GetSlinkData();
        [GeneratedRegex(@"\s?\s*\*+\s?\s*{{([nN]bt\s)?inherit(?<1>[a-z_/|=*\s]+)}}\s?\s*", RegexOptions.IgnoreCase)]
        public partial Regex GetInheritString();

        [GeneratedRegex(@"(其余的附加条件，)?取决于{{nbt\|string\|(?<1>[a-z_:]+)}}的值")]
        public partial Regex GetExtraKey();

        [GeneratedRegex(@"(?<=与).+(?=不能同时存在)")]
        public partial Regex GetMutexKey();

        [GeneratedRegex(@"\[\[(?<1>[a-zA-Z_\u4e00-\u9fff|#]+)\]\]")]
        public partial Regex GetEnumKey();

        [GeneratedRegex(@"{{interval(\|left=(?<1>\d+))?(\|right=(?<2>\d+))?}}", RegexOptions.IgnoreCase)]
        public partial Regex GetNumberRange();

        [GeneratedRegex(@"required=1", RegexOptions.IgnoreCase)]
        public partial Regex GetRequiredKey();

        [GeneratedRegex(@"(?<=默认为<code>)(?<1>[a-z:_]+)(?=</code>)", RegexOptions.IgnoreCase)]
        public partial Regex GetDefaultEnumValue();

        [GeneratedRegex(@"<''(?<1>[a-z_\u4e00-\u9fff]+)''>", RegexOptions.IgnoreCase)]
        public partial Regex GetEnumValueMode3();

        [GeneratedRegex(@"\s?\s*=+\s?\s*([#\u4e00-\u9fffa-z_/]+)\s?\s*=+\s?\s*", RegexOptions.IgnoreCase)]
        public partial Regex GetContextFileMarker();

        [GeneratedRegex(@"=+\s*\s?((minecraft\:)?[a-z_/]+)\s*\s?=+", RegexOptions.IgnoreCase)]
        public partial Regex GetEnumTypeKeywords();

        [GeneratedRegex(@"(?<=包版本-)[0-9]+")]
        public partial Regex PackVersionComparer();

        [GeneratedRegex(@"(?<={)[0-9\-]+(?=})")]
        public partial Regex ItemSlotMatcher();

        [GeneratedRegex(@"(?<=Command:\\\"")([\w \:""\[\]'()!@#$%^&*\-=+\\|;,./?<>`~]*)+(?=\\\"")")]
        public partial Regex GetCommand();
    }
}
