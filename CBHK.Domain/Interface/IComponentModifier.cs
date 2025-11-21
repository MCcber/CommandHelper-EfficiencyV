namespace CBHK.Domain.Interface
{
    public interface IComponentModifier
    {
        public void SetRawdata(string data);

        public bool Verify(string targetData);

        public List<IComponent> Modifier(IComponent targetComponent);
    }
}
