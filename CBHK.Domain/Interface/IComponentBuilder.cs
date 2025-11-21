namespace CBHK.Domain.Interface
{
    public interface IComponentBuilder
    {
        public List<IComponent> Build(object model);
    }
}
