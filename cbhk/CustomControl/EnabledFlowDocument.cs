using System.Windows.Documents;

namespace CBHK.CustomControl
{
    public class EnabledFlowDocument:FlowDocument
    {
        protected override bool IsEnabledCore
        {
            get { return true; }
        }
    }
}
