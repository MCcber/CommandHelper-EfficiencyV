using Newtonsoft.Json.Linq;
using Prism.Events;
using System.Text;

namespace CBHK.Interface
{
    public partial interface IGeneratorBuilder
    {
        public IEventAggregator EventAggregator { get; set; }
        public bool ShowResult { get; set; }
        public string SelectedVersion { get; set; }
        public StringBuilder Result { get; }
        public bool SyncToFile { get; set; }
        public string ExternFilePath { get; set; }
        public JToken ExternallyData { get; set; }
        public bool ImportMode { get; set; }
        public void Create();
        public void CollectionData();
        public string Build();
    }
}
