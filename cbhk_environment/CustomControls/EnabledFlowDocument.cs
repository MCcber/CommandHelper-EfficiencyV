using System.Windows.Documents;

namespace cbhk_environment.CustomControls
{
    public class EnabledFlowDocument:FlowDocument
    {
        protected override bool IsEnabledCore
        {
            get { return true; }
        }
    }
}
