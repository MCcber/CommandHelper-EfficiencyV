using CBHK.Model.Constant;
using MinecraftLanguageModelLibrary.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CBHK.Utility.Data
{
    public class UsePathParser
    {
        public static ResolvedTypeReference Parse(Resource resource, DocumentPath documentPath,string targetResourceName)
        {
            ResolvedTypeReference result = null;
            string resultPath = "";
            MetaTypeEditorFieldDTO resultDTO = null;
            if(documentPath is null)
            {
                return result;
            }
            string documentItemPathString = documentPath.TargetPath;
            string parentPathString = documentPath.GetParentPath();
            //找出离单冒号最近的左侧双冒号起始位置
            Match dispatchFeatureMatch = resource.RegexService.GetDispatchFeaturePath().Match(documentItemPathString);
            if (dispatchFeatureMatch.Success)
            {
                parentPathString = documentItemPathString[..(dispatchFeatureMatch.Index - 2)];
            }

            //精准匹配目标结构
            if (!string.IsNullOrEmpty(parentPathString) && resource.DocumentPathItemMap.TryGetValue(parentPathString, out List<string> targetFileUseList) &&
               targetFileUseList is not null)
            {
                string targetUseFullPath = parentPathString + "::" + targetResourceName;
                string targetUsePath = targetFileUseList.FirstOrDefault(item => item.EndsWith(targetResourceName));
                //查找当前上下文的文件引用列表，找出目标引用路径
                if (!string.IsNullOrEmpty(targetUsePath))
                {
                    _ = resource.DocumentItemMap.TryGetValue(targetUsePath, out resultDTO);
                    resultPath = targetUsePath;
                    //检测引用语句的继承用例
                    if(targetUsePath.StartsWith("super"))
                    {
                        string[] targetUsePathSinglePartList = targetUsePath.Split("::");
                        //先去掉末尾资源名，方便向上计算路径
                        documentItemPathString = parentPathString;
                        int superCount = 0;
                        for (int i = 0; i < targetUsePathSinglePartList.Length; i++)
                        {
                            if (targetUsePathSinglePartList[i] == "super")
                            {
                                int lastColonIndex = documentItemPathString.LastIndexOf("::");
                                if(lastColonIndex > -1)
                                {
                                    documentItemPathString = parentPathString[..lastColonIndex];
                                    superCount++;
                                }
                            }
                        }
                        //根据super数量去除末尾的路径后与当前资源名拼接
                        string superPathString = "super::";
                        string rightPartUsePath = targetUsePath[(superPathString.Length * superCount)..];
                        targetUsePath = documentItemPathString + "::" + rightPartUsePath;
                        _ = resource.DocumentItemMap.TryGetValue(targetUsePath, out resultDTO);
                        resultPath = targetUsePath;
                    }
                }
                //找出当前文档内部的结构资源
                else if (resource.DocumentItemMap.TryGetValue(targetUseFullPath, out resultDTO))
                {
                    resultPath = targetUseFullPath;
                }
                else
                {
                    resultPath = "";
                    resultDTO = null;
                }
            }
            result = new(resultPath, resultDTO);
            return result;
        }
    }
}
