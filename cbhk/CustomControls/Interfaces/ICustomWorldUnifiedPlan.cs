using cbhk.CustomControls.JsonTreeViewComponents;
using System.Collections.Generic;
using System.Windows;

namespace cbhk.CustomControls.Interfaces
{
    public interface ICustomWorldUnifiedPlan
    {
        public Dictionary<string, JsonTreeViewContext> KeyValueOffsetDictionary { get; set; }
        public void UpdateAllSubsequentOffsetMembers(JsonTreeViewItem currentItem, FrameworkElement frameworkElement);
        public void UpdateValueBySpecifyingInterval(JsonViewInterval interval,string newValue = "");
        public JsonTreeViewItem FindNodeBySpecifyingOffset(JsonViewInterval interval);
        public bool VerifyCorrectnessLayerByLayer(JsonTreeViewItem currentItem);
    }

    public class JsonViewInterval
    {
        public int StartOffset { get; set; }
        public int EndOffset { get; set; }
    }
}
