using Syncfusion.Windows.Edit;
using System.Windows.Media;

namespace cbhk_environment.GeneralTools.Mcfunctrion
{
    public class McfunctionLanguage: ProceduralLanguageBase
    {
        public McfunctionLanguage(EditControl control): base(control)
        {
            Name = "Mcfunction";
            FileExtension = "mcfunction";
            ApplyColoring = true;
            SupportsIntellisense = true;
            SupportsOutlining = true;
            TextForeground = Brushes.Black;
            //this.blocksStack = new Stack<BlockListener>();
        }
    }
}
