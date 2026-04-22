using CBHK.Domain.Interface;
using Newtonsoft.Json.Linq;
using Prism.Events;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;

namespace CBHK.CustomControl.MCDocument
{
    public class ColorButton : System.Windows.Controls.Button, IComponent
    {
        public IEventAggregator EventAggregator { get; set; }

        public PubSubEvent<string> VersionChanged { get; set; }

        public List<PubSubEvent> ComponentEventList { get; set; }

        public StringBuilder Result { get; set; }
        public bool? IsCompliantVersion { get; set; }
        public string Version { get; set; }
        public string ExternFilePath { get; set; }
        public JToken ExternallyData { get; set; }
        public bool ImportMode { get; set; }

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

        public void ImportExternData(string Data)
        {

        }

        public void SetEnableState(bool state)
        {

        }

        public void UpdateVersion(string SelectedVersion)
        {

        }
    }
}
