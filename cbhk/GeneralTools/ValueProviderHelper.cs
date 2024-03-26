using cbhk.CustomControls.Interfaces;
using cbhk.CustomControls.JsonTreeViewComponents;
using cbhk.CustomControls.JsonTreeViewComponents.ValueComponents;
using static cbhk.CustomControls.JsonTreeViewComponents.Enums;

namespace cbhk.GeneralTools
{
    public static class ValueProviderHelper
    {
        public static void UpdateIntProviderType(ICustomWorldUnifiedPlan context,JsonTreeViewItem jsonTreeViewItem,IntProvider provider)
        {
            jsonTreeViewItem.Children.Clear();
            if (provider.CurrentStructure is IntProviderStructures.Constant)
            {
                jsonTreeViewItem.IsNumberType = true;
            }
            else
            {
                jsonTreeViewItem.IsNumberType = false;
                jsonTreeViewItem.IsCompoundType = true;
                foreach (var item in IntProvider.StructureChildren[provider.CurrentStructure])
                {
                    jsonTreeViewItem.Children.Add(item);
                }
                #region 设置type节点的键值对偏移
                provider.TypeKeyStartOffset = context.KeyValueOffsetDictionary[jsonTreeViewItem.Path].ValueStartOffset + 2 + (provider.LayerCount + 1) * 4;
                provider.TypeKeyEndOffset = provider.TypeKeyStartOffset + jsonTreeViewItem.Children[0].Key.Length + 2;
                provider.TypeValueStartOffset = provider.TypeKeyEndOffset + 2;
                provider.TypeValueEndOffset = provider.TypeValueStartOffset + jsonTreeViewItem.Children[0].Value.Length + 2;
                #endregion
            }
        }
    }
}
