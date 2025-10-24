using Newtonsoft.Json.Linq;
using Prism.Events;
using System.Text;

namespace CBHK.Interface
{
    public partial interface IGenerator
    {
        public IEventAggregator EventAggregator { get; set; }
        public bool ShowResult { get; set; }
        public string SelectedVersion { get; set; }
        public bool SyncToFile { get; set; }
        public string ExternFilePath { get; set; }
        public JToken ExternallyData { get; set; }
        public bool ImportMode { get; set; }
        public StringBuilder Create();
        public void CollectionData(StringBuilder Result);
        public void Build(StringBuilder Result);
    }
}
