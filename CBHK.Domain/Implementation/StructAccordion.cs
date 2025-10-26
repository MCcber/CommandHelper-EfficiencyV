using CBHK.Domain.Interface;
using CBHK.Model.Common;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Windows.Controls;

namespace CBHK.Domain.Implementation
{
    public class StructAccordion : Expander, IComponent
    {
        public IEventAggregator EventAggregator { get; set; }

        public RemoveComponentEvent RemoveComponentEvent { get; set; }

        public StringBuilder Result { get; set; }
        public string Version { get; set; }
        public string TargetVersion { get; init; }
        public string ExternFilePath { get; set; }
        public JToken ExternallyData { get; set; }
        public bool ImportMode { get; set; }
        public bool IsContainer { get; set; }
        public List<IComponent> Children { get; set; }

        public void Build(StringBuilder Result)
        {
        }

        public void CollectionData(StringBuilder Result)
        {
        }

        public StringBuilder Create()
        {
            return new();
        }

        public void UpdateVersion(string SelectedVersion)
        {
        }
    }
}
