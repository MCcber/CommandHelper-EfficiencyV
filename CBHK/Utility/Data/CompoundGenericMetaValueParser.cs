using CBHK.Model.Constant;
using MinecraftLanguageModelLibrary.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CBHK.Utility.Data
{
    public static class CompoundGenericMetaValueParser
    {
        /// <summary>
        /// 从 id 注解提取可选值名称：简单形式（#[id="registry"]）直接取版本资源数组，
        /// 复合形式（#[id(registry=…, path/include/exclude/prefix/suffix …)]）合并过滤后再加前后缀。
        /// 动态 map 的键枚举与字段的 id 枚举共用这一份，避免两边各写一套后行为分叉。
        /// </summary>
        public static List<string> ParseIdOptionNames(Resource resource, string version, MetaValue idObject)
        {
            List<string> optionNames = [];
            if (idObject is null)
            {
                return optionNames;
            }

            //简单形式：id 指向版本资源里的注册表数组。
            //取值可能是类型形式（#[id=xxx]），也可能是字符串字面量形式（#[id="xxx"]），两种都要认，
            //否则动态 map 的键会退化成自由输入框
            string simpleIdName = idObject.TypeValue?.LiteralValue?.ToString() ?? idObject.LiteralValue?.ToString();
            if (!string.IsNullOrEmpty(simpleIdName)
                && resource.GetRegistriesData(version) is JToken versionToken
                && versionToken.SelectToken(simpleIdName.Trim('"')) is JArray literalResourceArray)
            {
                optionNames.AddRange(literalResourceArray.Values<string>());
                return optionNames;
            }

            //复合形式：registry 取表，path/include/exclude 过滤，prefix/suffix 修饰
            if (idObject.Kind is not MetaValueKind.Tuple || idObject.Members is null)
            {
                return optionNames;
            }

            #region 收集数据模型
            MetaNamedValue registryValue = idObject.Members.FirstOrDefault(item => item.Name == "registry");
            MetaNamedValue pathValue = idObject.Members.FirstOrDefault(item => item.Name == "path");
            MetaNamedValue excludeValue = idObject.Members.FirstOrDefault(item => item.Name == "exclude");
            MetaNamedValue includeValue = idObject.Members.FirstOrDefault(item => item.Name == "include");
            MetaNamedValue prefixValue = idObject.Members.FirstOrDefault(item => item.Name == "prefix");
            MetaNamedValue suffixValue = idObject.Members.FirstOrDefault(item => item.Name == "suffix");
            string registryValueString = registryValue?.Value?.TypeValue?.LiteralValue is not null ? registryValue.Value.TypeValue.LiteralValue.ToString() : "";
            #endregion

            #region 提取包含列表与排除列表
            HashSet<string> excludeValueStringList = [];
            HashSet<string> includeValueStringList = [];

            if (excludeValue?.Value?.Items is not null)
            {
                for (int j = 0; j < excludeValue.Value.Items.Count; j++)
                {
                    excludeValueStringList.Add(excludeValue.Value.Items[j].TypeValue.LiteralValue.ToString());
                }
            }
            if (includeValue?.Value?.Items is not null)
            {
                for (int j = 0; j < includeValue.Value.Items.Count; j++)
                {
                    includeValueStringList.Add(includeValue.Value.Items[j].TypeValue.LiteralValue.ToString());
                }
            }
            #endregion

            #region 提取前后缀
            string prefixValueString = prefixValue?.Value?.TypeValue?.LiteralValue is not null ? prefixValue.Value.TypeValue.LiteralValue.ToString() : "";
            string suffixValueString = suffixValue?.Value?.TypeValue?.LiteralValue is not null ? suffixValue.Value.TypeValue.LiteralValue.ToString() : "";
            #endregion

            #region 添加目标资源数组、处理添加与删除列表
            if (!string.IsNullOrEmpty(registryValueString) && resource.GetRegistriesData(version) is JToken registryVersionToken && registryVersionToken.SelectToken(registryValueString) is JArray targetRegistryArray)
            {
                //路径过滤
                if (pathValue?.Value?.TypeValue?.LiteralValue is not null)
                {
                    string targetPathString = pathValue.Value.TypeValue.LiteralValue.ToString();
                    optionNames.AddRange(targetRegistryArray.Values<string>().Where(item => item.StartsWith(targetPathString)));
                }
                else
                {
                    optionNames.AddRange(targetRegistryArray.Values<string>());
                }
            }

            //删除排除列表的所有成员，添加包含列表的所有成员
            optionNames.RemoveAll(excludeValueStringList.Contains);
            optionNames.AddRange(includeValueStringList);
            #endregion

            #region 添加前缀与后缀
            if (!string.IsNullOrEmpty(prefixValueString) || !string.IsNullOrEmpty(suffixValueString))
            {
                for (int j = 0; j < optionNames.Count; j++)
                {
                    optionNames[j] = prefixValueString + optionNames[j] + suffixValueString;
                }
            }
            #endregion

            return optionNames;
        }

        /// <summary>
        /// 将目标资源的复合泛型元值解析为MetaTypeEditorFieldDTO对象。
        /// </summary>
        /// <param name="resource">资源上下文</param>
        /// <param name="documentPath">路径</param>
        /// <param name="featureMap">特性映射表</param>
        /// <returns></returns>
        public static MetaTypeEditorFieldDTO Parse(Resource resource, string version, Dictionary<string, MetaValue> featureMap)
        {
            MetaTypeEditorFieldDTO result = null;

            if(featureMap is null || featureMap.Count == 0)
            {
                return null;
            }

            //暂时只处理id类型
            if(featureMap.TryGetValue("id", out MetaValue idValue) && idValue is not null)
            {
                //id 的取值有类型形式与字符串字面量形式两种，统一取出资源名再判断是什么形态
                string idResourceName = idValue.TypeValue?.LiteralValue?.ToString() ?? idValue.LiteralValue?.ToString();
                //复合 id（registry=/prefix= 这类）没有可查的资源名，键枚举直接走共享的注册表收集逻辑
                if (string.IsNullOrEmpty(idResourceName))
                {
                    List<string> compoundOptionNameList = ParseIdOptionNames(resource, version, idValue);
                    if (compoundOptionNameList.Count > 0)
                    {
                        result = new()
                        {
                            ID = Guid.NewGuid().ToString(),
                            TypeKind = MetaTypeKind.Enum,
                            EnumOptionList = [..compoundOptionNameList.Select(name => new EnumMember() { Name = name, Value = new MetaValue() { Kind = MetaValueKind.Literal, LiteralValue = name } })]
                        };
                    }
                }
                //简单 id：资源名可能省略命名空间，未命中时补一次 minecraft: 前缀
                else
                {
                    string currentResourceLocation = idResourceName;
                    List<string> candidateLocationList = [currentResourceLocation];
                    if (!currentResourceLocation.Contains(':'))
                    {
                        candidateLocationList.Add("minecraft:" + currentResourceLocation);
                    }

                    foreach (string candidateLocation in candidateLocationList)
                    {
                        //优先搜索版本资源
                        if (resource.GetRegistriesData(version) is JToken versionToken && versionToken.SelectToken(candidateLocation) is JArray targetDataArray)
                        {
                            result = new()
                            {
                                ID = Guid.NewGuid().ToString(),
                                TypeKind = MetaTypeKind.Enum,
                                EnumOptionList = [..targetDataArray.Select(item=> new EnumMember() { Name = item.Value<string>(),Value = new MetaValue() { Kind = MetaValueKind.Literal,LiteralValue = item.Value<string>() } })]
                            };
                        }//搜索调度器
                        else
                        {
                            result = new()
                            {
                                ID = Guid.NewGuid().ToString(),
                                TypeKind = MetaTypeKind.Enum,
                                EnumOptionList = []
                            };
                            //搜素调度器
                            var dispatchPairList = resource.DocumentItemMap.Where(item => item.Value?.TypeKind is MetaTypeKind.Dispatch || item.Value?.OriginKind is MetaTypeKind.Dispatch);
                            var dispatchDTOList = dispatchPairList.Select(item => item.Value);
                            //搜索资源键
                            List<MetaTypeEditorFieldDTO> targetResourceDispatchList = [..dispatchDTOList.Where(item => item.FeatureMap?["Resource"]?.LiteralValue?.ToString() == candidateLocation)];
                            for (int i = 0; i < targetResourceDispatchList.Count; i++)
                            {
                                if (targetResourceDispatchList[i].FeatureMap.TryGetValue("Index", out MetaValue indexValue))
                                {
                                    //单值类型
                                    if (indexValue.Kind is MetaValueKind.Literal && indexValue.LiteralValue is not null
                                        && !indexValue.LiteralValue.ToString().StartsWith("%"))
                                    {
                                        result.EnumOptionList.Add(new EnumMember() { Name = indexValue.LiteralValue.ToString(), Value = new() { Kind = MetaValueKind.Literal, LiteralValue = indexValue.LiteralValue.ToString() } });
                                    }
                                    // 列表类型：mcdoc 解析出的 Index 列表存放在 Items，
                                    // 这里直接展开为 key Enum，供动态 Map / registry 缺省回退使用。
                                    else if (indexValue.Kind is MetaValueKind.List && indexValue.Items?.Count > 0)
                                    {
                                        result.EnumOptionList.AddRange(indexValue.Items
                                            .Where(item => item.LiteralValue is not null && !item.LiteralValue.ToString().StartsWith("%"))
                                            .Select(item => new EnumMember() { Name = item.LiteralValue!.ToString(), Value = item }));
                                    }
                                }
                            }
                        }

                        if (result.EnumOptionList.Count > 0)
                        {
                            break;
                        }
                    }
                }

                if (result?.TypeKind is MetaTypeKind.Enum && !result.IsRequired)
                {
                    result.EnumOptionList.Insert(0, new EnumMember() { Name = "- unset -", Value = new() { Kind = MetaValueKind.Literal, LiteralValue = "unset" } });
                }
            }

            return result;
        }
    }
}
