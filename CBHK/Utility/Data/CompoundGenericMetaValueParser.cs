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
            if(featureMap.TryGetValue("id", out MetaValue idValue) && idValue.TypeValue.LiteralValue is not null)
            {
                string currentResourceLocation = idValue.TypeValue.LiteralValue.ToString();
                //优先搜索版本资源
                if (resource.RunningDataObject[version][currentResourceLocation] is JArray targetDataArray)
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
                    List<MetaTypeEditorFieldDTO> targetResourceDispatchList = [..dispatchDTOList.Where(item => item.FeatureMap?["Resource"]?.LiteralValue?.ToString() == currentResourceLocation)];
                    for (int i = 0; i < targetResourceDispatchList.Count; i++)
                    {
                        if (targetResourceDispatchList[i].FeatureMap.TryGetValue("Index", out MetaValue indexValue))
                        {
                            //单值类型
                            if(indexValue.Kind is MetaValueKind.Literal && indexValue.LiteralValue is not null)
                            {
                                result.EnumOptionList.Add(new EnumMember() { Name = indexValue.LiteralValue.ToString(), Value = new() { Kind = MetaValueKind.Literal, LiteralValue = indexValue.LiteralValue.ToString() } });
                            }//列表类型
                            else if (indexValue.Kind is MetaValueKind.List && indexValue.Members?.Count > 0)
                            {
                                result.EnumOptionList.AddRange(indexValue.Members.Select(item => new EnumMember() { Name = item.Name, Value = item.Value }));
                            }
                        }
                    }
                }
                if (result.TypeKind is MetaTypeKind.Enum && !result.IsRequired)
                {
                    result.EnumOptionList.Insert(0, new EnumMember() { Name = "- unset -", Value = new() { Kind = MetaValueKind.Literal, LiteralValue = "unset" } });
                }
            }

            return result;
        }
    }
}
