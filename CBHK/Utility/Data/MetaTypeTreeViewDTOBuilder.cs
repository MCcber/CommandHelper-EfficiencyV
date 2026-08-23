using CBHK.Model.Data;
using MinecraftLanguageModelLibrary.Data;
using System;
using System.Collections.Generic;

namespace CBHK.Utility.Data
{
    public class MetaTypeTreeViewDTOBuilder
    {
        /// <summary>
        /// 根据全文索引构建DTO树与全文索引字典
        /// </summary>
        /// <param name="anchorList"></param>
        /// <returns></returns>
        public static DTOInstanceContext BuildDTOTree(List<KeyValueAnchors> anchorList)
        {
            DTOInstanceContext result = new([], []);

            // 栈中存放 (容器 dto, 容器的结束偏移量)
            Stack<(MetaTypeEditorFieldDTO DTO, int EndOffset)> stack = new();

            for (int i = 0; i < anchorList.Count; i++)
            {
                //分配当前节点的ID
                string guid = Guid.NewGuid().ToString();

                //为roots的字典增加成员
                result.anchorMap.Add(guid, anchorList[i]);

                // 闭合所有已结束的容器
                while (stack.Count > 0 && anchorList[i].ValueStart.Offset >= stack.Peek().EndOffset)
                {
                    stack.Pop();
                }

                // 创建当前节点 dto
                MetaTypeEditorFieldDTO dto = new()
                {
                    ID = guid,
                    Path = new(""),
                    FieldName = anchorList[i].Key,
                    TypeKind = (anchorList[i].IsContainer
                        ? (anchorList[i].IsArray ? MetaTypeKind.List : MetaTypeKind.Struct)
                        : MetaTypeKind.String),   // 简单类型先统一视为 String，后续可由模板修正
                };
                anchorList[i].Key = null;

                // 挂到父节点下
                if (stack.Count > 0)
                {
                    var parent = stack.Peek().DTO;
                    //绑定父节点引用
                    dto.Parent = parent;
                    if (parent.TypeKind == MetaTypeKind.List
                        || parent.TypeKind == MetaTypeKind.LongArray
                        || parent.TypeKind == MetaTypeKind.IntArray
                        || parent.TypeKind == MetaTypeKind.ByteArray)
                    {
                        parent.Items ??= [];
                        parent.Items.Add(dto);
                    }
                    else
                    {
                        parent.Children ??= [];
                        parent.Children.Add(dto);
                    }
                }
                else
                {
                    result.dtoInstanceList.Add(dto);
                }

                // 若是容器，压入栈中（记录其结束偏移）
                if (anchorList[i].IsContainer)
                {
                    stack.Push((dto, anchorList[i].ValueEnd.Offset));
                }
            }

            return result;
        }
    }
}
