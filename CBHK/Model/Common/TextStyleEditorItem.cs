using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace CBHK.Model.Common
{
    public class TextStyleEditorItem
    {
        public ObservableCollection<InlineData> InlineList { get; set; }

        private string _fullTextCache;
        public string FullTextCache => _fullTextCache ??= string.Concat(InlineList.Select(x => x.Text));
        
        public bool IsSelected { get; set; }
        public Brush MemberBrush { get; set; } = Brushes.White;
        public Brush DisplayPanelBrush { get; set; } = Brushes.Black;
    }
}