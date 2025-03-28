using System.Windows.Documents;

namespace CBHK.CustomControls
{
    public class EnabledFlowDocument:FlowDocument
    {
        protected override bool IsEnabledCore
        {
            get { return true; }
        }
    }
}
