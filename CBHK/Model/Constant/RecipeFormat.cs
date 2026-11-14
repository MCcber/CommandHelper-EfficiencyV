using System;

namespace CBHK.Model.Constant
{
    /// <summary>
    /// 按版本输出配方 JSON 的格式约定，依据本地 mcdoc（vanilla-mcdoc/java/data/recipe.mcdoc）的 since/until 标注：
    /// 1.20.5 起结果物品用 ItemStack（id 字段），1.21.2 起材料改成字符串写法，1.21.5 起锻造台的 trim 需要 pattern。
    /// </summary>
    public sealed class RecipeFormat
    {
        private readonly string version;

        public RecipeFormat(string version)
        {
            this.version = string.IsNullOrWhiteSpace(version) ? "" : version.Trim();
        }

        public static RecipeFormat Legacy { get; } = new("");

        public static RecipeFormat ForVersion(string version)
        {
            return string.IsNullOrWhiteSpace(version) ? Legacy : new RecipeFormat(version);
        }

        /// <summary>
        /// 结果物品字段名：1.20.5 起是 id，之前是 item
        /// </summary>
        public string ResultItemKey => AtLeast("1.20.5") ? "id" : "item";

        /// <summary>
        /// 结果是否已是 ItemStack 对象（1.20.5 起）
        /// </summary>
        public bool ResultUsesItemStack => AtLeast("1.20.5");

        /// <summary>
        /// 材料写法：1.21.2 起用字符串 / 字符串数组
        /// </summary>
        public bool IngredientAsString => AtLeast("1.21.2");

        /// <summary>
        /// 锻造台输入是否可缺省：1.21.2 起 base/addition/template 变可选
        /// </summary>
        public bool SmithingPartsOptional => AtLeast("1.21.2");

        /// <summary>
        /// smithing_trim 是否需要 pattern：1.21.5 起必填
        /// </summary>
        public bool TrimNeedsPattern => AtLeast("1.21.5");

        /// <summary>
        /// 结果物品对象（不含结尾大括号，调用方自行补 count 与收尾）
        /// </summary>
        public string ResultObject(string itemId)
        {
            return "{\"" + ResultItemKey + "\":\"minecraft:" + itemId + "\"";
        }

        /// <summary>
        /// 一个材料项：老版本是 {item}/{tag} 对象，1.21.2 起是 "minecraft:x" 或 "#minecraft:tag"
        /// </summary>
        public string Ingredient(string itemId, string tag)
        {
            if (IngredientAsString)
            {
                if (!string.IsNullOrEmpty(tag))
                    return "\"#" + (tag.Contains(':') ? tag : "minecraft:" + tag) + "\"";
                return "\"minecraft:" + itemId + "\"";
            }

            return "{\"item\":\"minecraft:" + itemId + "\"" + (string.IsNullOrEmpty(tag) ? "}" : ",\"tag\":\"" + tag + "\"}");
        }

        private bool AtLeast(string target)
        {
            return CompareVersion(version, target) >= 0;
        }

        private static int CompareVersion(string left, string right)
        {
            string[] leftParts = (left ?? "").Split('.');
            string[] rightParts = (right ?? "").Split('.');
            int length = Math.Max(leftParts.Length, rightParts.Length);

            for (int i = 0; i < length; i++)
            {
                string leftPart = i < leftParts.Length ? leftParts[i] : "0";
                string rightPart = i < rightParts.Length ? rightParts[i] : "0";

                int result = int.TryParse(leftPart, out int leftNumber) && int.TryParse(rightPart, out int rightNumber)
                    ? leftNumber.CompareTo(rightNumber)
                    : string.CompareOrdinal(leftPart, rightPart);
                if (result != 0)
                    return result;
            }
            return 0;
        }
    }
}
