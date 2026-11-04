using MinecraftLanguageModelLibrary.Data;
using System.Collections.Generic;

namespace CBHK.Utility.Data
{
    public class UnionTypeNameParser
    {
        /// <summary>
        /// 枚举成员是否代表"未设置"位（联合的可选标记，不是真实成员）
        /// </summary>
        private static bool IsUnsetEnumMember(EnumMember member)
        {
            return member is not null && (member.Value?.LiteralValue?.ToString() is "unset" || member.Name == "- unset -");
        }

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

                //成员自身名字优先于类型名兜底（例如 [modifier] 分支应显示 modifier 而不是 Object）
                string memberName = targetList[i].FieldName;
                if (string.IsNullOrEmpty(memberName))
                {
                    memberName = targetList[i].DisplayName;
                }
                if (!string.IsNullOrEmpty(memberName))
                {
                    result.Add(memberName);
                    continue;
                }

                //跨层合并上来的颜色成员按取值形态取名：列表 → List、数值 → Int、其余 → String
                if (targetList[i].FeatureMap?.ContainsKey("color") == true)
                {
                    string colorLabel = targetList[i].ElementType is not null
                        ? "List"
                        : targetList[i].Value?.ToString() is { Length: > 0 } colorValue && double.TryParse(colorValue, out _)
                            ? "Int"
                            : "String";
                    result.Add(colorLabel);
                    continue;
                }

                //跨层合并上来的无名成员：用解析出的类型名作为标签，比裸类型名更可读
                if (!string.IsNullOrEmpty(targetList[i].TypeName))
                {
                    result.Add(targetList[i].TypeName);
                    continue;
                }

                //带 id 注解的成员（如 #[id="sound_event"]）用注解值当分支名，
                //比"枚举当前选中项名"稳定：后者可能恰好是 - unset -，看起来像多出一个 unset 成员
                if (targetList[i].FeatureMap?.TryGetValue("id", out MetaValue idFeature) == true)
                {
                    //id 的值可能落在 LiteralValue，也可能落在 TypeValue.LiteralValue
                    string idName = idFeature?.LiteralValue?.ToString() ?? idFeature?.TypeValue?.LiteralValue?.ToString();
                    if (!string.IsNullOrEmpty(idName))
                    {
                        result.Add(idName);
                        continue;
                    }
                }

                //带 id 注解的成员被构建成枚举：复合 id（registry=… 这类）没有可直接用的名字，
                //类型标签取转换前的声明类型（如 string → String），比枚举选项名可读且不会带出未设置位
                if (targetList[i].TypeKind is MetaTypeKind.Enum
                    && targetList[i].FeatureMap?.ContainsKey("id") == true
                    && targetList[i].TemplateReference?.TypeKind is MetaTypeKind.String or MetaTypeKind.Int or MetaTypeKind.Long
                        or MetaTypeKind.Float or MetaTypeKind.Double or MetaTypeKind.Boolean)
                {
                    result.Add(targetList[i].TemplateReference.TypeKind.ToString());
                    continue;
                }

                //无名枚举成员用当前选项名（Int / List / String 这类类型名）作为标签，
                //- unset - 是联合的未设置位，不能拿来当成员名
                string enumName = IsUnsetEnumMember(targetList[i].SelectedEnumOption) ? null : targetList[i].SelectedEnumOption?.Name;
                if (string.IsNullOrEmpty(enumName) && targetList[i].EnumOptionList?.Count > 0 && !IsUnsetEnumMember(targetList[i].EnumOptionList[0]))
                {
                    enumName = targetList[i].EnumOptionList[0].Name;
                }
                if (targetList[i].TypeKind is MetaTypeKind.Enum && !string.IsNullOrEmpty(enumName))
                {
                    result.Add(enumName);
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

            #region 重名消解：按其它有效特征补区分
            Disambiguate(result, targetList);
            #endregion

            return result;
        }

        /// <summary>
        /// 重名消解：同型多支（如四个都写成 string 的分支）会拿到同一个类型兜底名，界面上分不清也选不准。
        /// 按"其它有效特征"逐个区分：类型名 → 字面量 → id 注解 → 元素类型 → 序号。模板自带的名称也走这里。
        /// </summary>
        public static void Disambiguate(List<string> names, List<MetaTypeEditorFieldDTO> branches)
        {
            for (int i = 0; i < names.Count; i++)
            {
                if (names[i] == "- unset -" || names.IndexOf(names[i]) == i)
                {
                    continue;
                }
                MetaTypeEditorFieldDTO branch = branches is not null && i < branches.Count ? branches[i] : null;
                string distinguished = branch?.TypeName;
                distinguished ??= branch?.Value?.ToString();
                if (string.IsNullOrEmpty(distinguished) && branch?.FeatureMap?.TryGetValue("id", out MetaValue idValue) == true)
                {
                    distinguished = idValue?.LiteralValue?.ToString() ?? idValue?.TypeValue?.LiteralValue?.ToString();
                }
                distinguished ??= branch?.ElementType?.TypeKind.ToString();
                if (string.IsNullOrEmpty(distinguished))
                {
                    distinguished = names[i] + " #" + i;
                }
                else if (names.Contains(distinguished))
                {
                    distinguished = distinguished + " #" + i;
                }
                names[i] = distinguished;
            }
        }
    }
}
