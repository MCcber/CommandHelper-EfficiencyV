using Newtonsoft.Json.Linq;
using System.Text;

namespace CBHK.Domain.Interface
{
    public interface IComponent
    {
        public IEventAggregator EventAggregator { get; }
        public PubSubEvent<string>? VersionChanged { get; set; }
        public List<PubSubEvent>? ComponentEventList { get; }
        public StringBuilder? Result { get; set; }
        public bool? IsCompliantVersion { get; set; }
        public string? Version { get; set; }
        public void UpdateVersion(string SelectedVersion);
        public string? ExternFilePath { get; set; }
        public JToken? ExternallyData { get; set; }
        public bool ImportMode { get; set; }
        public StringBuilder Create();
        public void CollectionData(StringBuilder Result);
        public void Build(StringBuilder Result);
        public void ImportExternData(string Data);
        public void SetEnableState(bool state);
    }
}
