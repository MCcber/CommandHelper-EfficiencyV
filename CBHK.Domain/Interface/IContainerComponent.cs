namespace CBHK.Domain.Interface
{
    public interface IContainerComponent : IComponent
    {
        public string Description { get; set; }
        public void AddChild(List<Tuple<IComponent, List<PubSubEvent>>> ChildrenMetaDataList);
        public void RemoveChild(object Child);
        public List<IComponent> GetChildren();
    }
}