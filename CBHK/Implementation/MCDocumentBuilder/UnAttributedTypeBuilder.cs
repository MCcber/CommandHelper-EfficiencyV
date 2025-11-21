using CBHK.Domain.Interface;
using System.Collections.Generic;

namespace CBHK.Implementation.MCDocumentBuilder
{
    public class UnAttributedTypeBuilder : IComponentBuilder
    {
        #region SubBuilder
        private StringTypeBuilder stringTypeBuilder = new();
        #endregion

        public List<IComponent> Build(object model)
        {
            return [];
        }
    }
}
