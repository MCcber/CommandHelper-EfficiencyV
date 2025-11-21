using CBHK.Domain.Interface;
using System.Collections.Generic;

namespace CBHK.Utility.Modifier
{
    public class TypeSentenceModifier : IComponentModifier
    {
        public List<IComponent> Modifier(IComponent targetComponent)
        {
            return [];
        }

        public void SetRawdata(string data)
        {

        }

        public bool Verify(string targetData)
        {
            return false;
        }
    }
}
