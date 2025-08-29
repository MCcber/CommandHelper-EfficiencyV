using System.Collections.Generic;

namespace CBHK.Model.Common
{
    public class SyntaxCompletionObject
    {
        public string Type { get; set; }
        public List<SyntaxCompletionObject> Children { get; set; }
        public string Parser { get; set; }
        public SyntaxCompletionProperties Properties { get; set; }
        public bool Executable { get; set; }
    }
}