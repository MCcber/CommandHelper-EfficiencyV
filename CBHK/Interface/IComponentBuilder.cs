using CBHK.Model.Common;
using Newtonsoft.Json.Linq;
using Prism.Events;
using System.Text;

namespace CBHK.Interface
{
    public interface IComponentBuilder
    {
        public IEventAggregator EventAggregator { get; }
        public RemoveComponentEvent RemoveComponentEvent { get; }
        public void UpdateVersion(string SelectedVersion);
        public string ExternFilePath { get; set; }
        public JToken ExternallyData { get; set; }
        public bool ImportMode { get; set; }
        public StringBuilder Create();
        public void CollectionData(StringBuilder Result);
        public void Build(StringBuilder Result);
    }
}
