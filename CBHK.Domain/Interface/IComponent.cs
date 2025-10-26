using CBHK.Model.Common;
using Newtonsoft.Json.Linq;
using System.Text;

namespace CBHK.Domain.Interface
{
    public interface IComponent
    {
        public IEventAggregator EventAggregator { get; }
        public RemoveComponentEvent RemoveComponentEvent { get; }
        public StringBuilder Result { get; set; }
        public string Version { get; set; }
        public string TargetVersion { get; init; }
        public void UpdateVersion(string SelectedVersion);
        public string ExternFilePath { get; set; }
        public JToken ExternallyData { get; set; }
        public bool ImportMode { get; set; }
        public StringBuilder Create();
        public void CollectionData(StringBuilder Result);
        public void Build(StringBuilder Result);
        public bool IsContainer { get; set; }
        public List<IComponent> Children { get; set; }
    }
}
