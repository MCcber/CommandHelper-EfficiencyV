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
        public void UpdateVersion();
        public string SelectedVersion { get; }
        public StringBuilder Result { get; }
        public string ExternFilePath { get; set; }
        public JToken ExternallyData { get; set; }
        public bool ImportMode { get; set; }
        public void Create();
        public void CollectionData();
        public StringBuilder Build();
    }
}
