using CBHK.Domain.Interface;
using Newtonsoft.Json.Linq;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;

namespace CBHK.CustomControl.MCDocument
{
    /// <summary>
    /// AttributeGrid.xaml 的交互逻辑
    /// </summary>
    public partial class AttributeGrid : UserControl, IContainerComponent
    {
        public required string Description { get; set; }
        public IEventAggregator EventAggregator { get; set; }
        public List<PubSubEvent> ComponentEventList { get; set; }
        public StringBuilder Result { get; set; }
        public bool? IsCompliantVersion { get; set; }
        public string Version { get; set; }
        public string ExternFilePath { get; set; }
        public JToken ExternallyData { get; set; }
        public bool ImportMode { get; set; }
        public PubSubEvent<string> VersionChanged { get; set; }

        public AttributeGrid()
        {
            InitializeComponent();
            //ColumnDefinitions.Add(column);
            TextBlock textBlock = new()
            {
                Text = Description,
                Background = new SolidColorBrush(Colors.Transparent),
                Foreground = new SolidColorBrush(Colors.White)
            };
            //Children.Add(textBlock);
            //SetColumn(textBlock, 0);
        }

        public void AddChild(List<Tuple<IComponent, List<PubSubEvent>>> ChildrenMetaDataList)
        {

        }

        public void RemoveChild(object Child)
        {

        }

        public List<IComponent> GetChildren()
        {
            return [];
        }

        public void Build(StringBuilder Result)
        {

        }

        public void ImportExternData(string Data)
        {

        }

        public void CollectionData(StringBuilder Result)
        {

        }

        public StringBuilder Create()
        {
            return new();
        }

        public void SetEnableState(bool state)
        {

        }

        public void UpdateVersion(string SelectedVersion)
        {

        }
    }
}
