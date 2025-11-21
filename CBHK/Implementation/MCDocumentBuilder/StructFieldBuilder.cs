using CBHK.Domain.Interface;
using MinecraftLanguageModelLibrary.Model.MCDocument;
using System.Collections.Generic;

namespace CBHK.Implementation.MCDocumentBuilder
{
    internal class StructFieldBuilder : IComponentBuilder
    {
        public List<IComponent> Build(object model)
        {
            List<IComponent> result = [];
            if(model is StructField structField)
            {

            }
            return result;
        }
    }
}
