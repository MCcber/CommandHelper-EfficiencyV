using MinecraftLanguageModelLibrary.Data;
using System.Collections.Generic;

namespace CBHK.Utility.Data
{
    public class UnionTypeNameParser
    {
        public static List<string> Parse(List<MetaTypeEditorFieldDTO> targetList)
        {
            List<string> result = [];
            for (int i = 0; i < targetList.Count; i++)
            {
                //访问器名优先作为分支名，例如 [value] / [modifier]
                string accessorName = "";
                if (targetList[i].FeatureMap.TryGetValue("Accessor", out MetaValue accessor) && accessor is not null)
                {
                    accessorName = accessor.Kind is MetaValueKind.List && accessor.Items?.Count > 0
                        ? accessor.Items[0].LiteralValue?.ToString()
                        : accessor.LiteralValue?.ToString();
                }
                if (!string.IsNullOrEmpty(accessorName))
                {
                    result.Add(accessorName);
                    continue;
                }

                switch (targetList[i].TypeKind)
                {
                    case MetaTypeKind.Byte:
                    case MetaTypeKind.Short:
                    case MetaTypeKind.Int:
                    case MetaTypeKind.Long:
                    case MetaTypeKind.Float:
                    case MetaTypeKind.Double:
                    case MetaTypeKind.String:
                    case MetaTypeKind.Boolean:
                    case MetaTypeKind.List:
                    case MetaTypeKind.Union:
                        {
                            result.Add(targetList[i].TypeKind.ToString());
                            break;
                        }
                    case MetaTypeKind.Enum:
                    case MetaTypeKind.NamedColor:
                        {
                            result.Add("Enum");
                            break;
                        }
                    case MetaTypeKind.CompositeRGB:
                    case MetaTypeKind.CompositeARGB:
                    case MetaTypeKind.DecRGB:
                    case MetaTypeKind.DecRGBA:
                    case MetaTypeKind.HexRGB:
                    case MetaTypeKind.HexARGB:
                    case MetaTypeKind.Identifier:
                    case MetaTypeKind.Literal:
                        {
                            result.Add("String");
                            break;
                        }
                    case MetaTypeKind.Struct:
                        {
                            string name = targetList[i].FieldName;
                            if(string.IsNullOrEmpty(name))
                            {
                                name = targetList[i].DisplayName;
                            }
                            if (!string.IsNullOrEmpty(name) && i + 1 < targetList.Count && targetList[i + 1].TypeKind is not MetaTypeKind.List)
                            {
                                result.Add(name);
                            }
                            else
                            {
                                result.Add("Object");
                            }
                            break;
                        }
                    case MetaTypeKind.Dispatch:
                    case MetaTypeKind.Indexed:
                        {
                            result.Add("Object");
                            break;
                        }
                    case MetaTypeKind.Generic:
                    case MetaTypeKind.Reference:
                    case MetaTypeKind.Tuple:
                        {
                            result.Add("Object");
                            break;
                        }
                    case MetaTypeKind.ByteArray:
                    case MetaTypeKind.IntArray:
                    case MetaTypeKind.LongArray:
                    case MetaTypeKind.UUIDArray:
                        {
                            result.Add("List");
                            break;
                        }
                }
            }
            return result;
        }
    }
}
