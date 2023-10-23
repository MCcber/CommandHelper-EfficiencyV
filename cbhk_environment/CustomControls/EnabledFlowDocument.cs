using System.Windows.Documents;

namespace cbhk.CustomControls
{
    public class EnabledFlowDocument:FlowDocument
    {
        protected override bool IsEnabledCore
        {
            get { return true; }
        }
    }
}
