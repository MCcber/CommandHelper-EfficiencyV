using CBHK.Domain.Interface;
using MinecraftLanguageModelLibrary.Model.MCDocument;
using System.Collections.Generic;

namespace CBHK.Implementation.MCDocumentBuilder
{
    public class PrelimBuilder : IComponentBuilder
    {
        public List<IComponentModifier> AttributeList = [];

        public List<IComponent> Build(object model)
        {
            List<IComponent> result = [];
            if(model is Prelim prelim)
            {
            }
            return result;
        }
    }
}