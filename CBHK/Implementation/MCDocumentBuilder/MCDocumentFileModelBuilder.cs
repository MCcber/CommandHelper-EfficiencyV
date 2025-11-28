using CBHK.CustomControl.MCDocument;
using CBHK.Domain.Interface;
using MinecraftLanguageModelLibrary.Model.MCDocument;
using MinecraftLanguageModelLibrary.Model.MCDocument.InjectionContent;
using Newtonsoft.Json;
using Prism.Ioc;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CBHK.Implementation.MCDocumentBuilder
{
    public class MCDocumentFileModelBuilder(IContainerProvider container)
    {
        #region Field
        private readonly NamedPipeClientStream client = new("MCDocumentLanguageServerPipe");
        private IContainerProvider container = container;
        #endregion

        #region Property
        #endregion

        #region Method
        public async Task<List<IComponent>> DataFileMappingToComponentList(string targetFilePath)
        {
            if (!client.IsConnected)
            {
                await client.ConnectAsync();
            }

            await client.WriteAsync(Encoding.UTF8.GetBytes(targetFilePath));
            byte[] resultArray = new byte[10240];
            await client.ReadAsync(resultArray);
            string data = Encoding.UTF8.GetString(resultArray);
            List<IComponent> result = [];
            if (data.Length > 0)
            {
                MCDocumentFileModel DocumentFileModel = JsonConvert.DeserializeObject<MCDocumentFileModel>(data);
                result = Build(DocumentFileModel);
            }
            return result;
        }

        #region 生成最终控件
        public List<IComponent> Build(MCDocumentFileModel fileModel)
        {
            List<IComponent> result = [];
            List<IComponent> structList = GetStructList(fileModel);
            List<IComponent> enumList = GetEnumList(fileModel);
            List<IComponent> typeAliaList = GetTypeAlias(fileModel);
            List<IComponent> useStatementList = GetUsestatementList(fileModel);
            List<IComponent> injectionList = GetInjectionList(fileModel);
            List<IComponent> dispatchStatementList = GetDispatchStatement(fileModel);
            return result;
        }

        public List<IComponent> GetStructList(MCDocumentFileModel fileModel)
        {
            List<IComponent> result = [];
            return result;
        }

        public List<IComponent> GetEnumList(MCDocumentFileModel fileModel)
        {
            List<IComponent> result = [];
            return result;
        }

        public List<IComponent> GetUsestatementList(MCDocumentFileModel fileModel)
        {
            List<IComponent> result = [];
            Dictionary<string, string> UseStatementDictionary = [];
            foreach (var item in fileModel.UseStatementList)
            {
                if((item.Name is null || item.Name.Length == 0) && item.Path is not null && item.Path.Length > 0)
                {
                    if(!UseStatementDictionary.TryAdd("", item.Path))
                    {
                        UseStatementDictionary[""] = item.Path;
                    }
                }
            }
            return result;
        }

        public List<IComponent> GetTypeAlias(MCDocumentFileModel fileModel)
        {
            List<IComponent> result = [];
            return result;
        }

        public List<IComponent> GetInjectionList(MCDocumentFileModel fileModel)
        {
            List<IComponent> result = [];
            return result;
        }

        public List<IComponent> GetDispatchStatement(MCDocumentFileModel fileModel)
        {
            List<IComponent> result = [];
            return result;
        }
        #endregion

        #region 获取控件子数据或修饰器
        /// <summary>
        /// 整合文档注释、普通注释和标注的属性
        /// </summary>
        /// <returns></returns>
        public IComponent GetPrelim(Prelim prelimList)
        {
            IComponent result = null;
            return result;
        }

        /// <summary>
        /// 整合结构字段数据
        /// </summary>
        /// <param name="Index"></param>
        /// <returns></returns>
        public List<IComponent> GetStructFieldList(StructField structField)
        {
            List<IComponent> result = [];
            string description = structField.StructKey.String;
            if(description is null || description.Length == 0)
            {
                description = structField.StructKey.Identifier;
            }
            description ??= "";

            foreach (var structureItem in structField.AttributeList)
            {
                ColumnDefinition column = new()
                {
                    Width = new GridLength(1, GridUnitType.Auto)
                };

                AttributeGrid attributeGrid = new()
                {
                    Description = description
                };
                //attributeGrid.ColumnDefinitions.Add(column);
                attributeGrid.AddChild([new(null, null)]);

                result.Add(attributeGrid);
            }
            return result;
        }

        /// <summary>
        /// 整合枚举字段数据
        /// </summary>
        /// <param name="enumerationList"></param>
        /// <returns></returns>
        public IComponent GetEnumField(Enumeration enumerationList)
        {
            IComponent result = null;
            return result;
        }

        /// <summary>
        /// 整合类型语句
        /// </summary>
        /// <returns></returns>
        public IComponent GetTypeSentence(MCDocumentType typeSentenceList)
        {
            IComponent result = null;
            return result;
        }

        /// <summary>
        /// 整合枚举注入列表
        /// </summary>
        /// <returns></returns>
        public IComponent GetEnumInjection(EnumInjection enumInjectionList)
        {
            IComponent result = null;
            return result;
        }

        /// <summary>
        /// 整合结构注入列表
        /// </summary>
        /// <returns></returns>
        public List<IComponent> GetStructInjection(StructInjection structInjectionList)
        {
            List<IComponent> result = [];
            return result;
        }

        /// <summary>
        /// 整合类型参数数据
        /// </summary>
        /// <returns></returns>
        public List<IComponent> GetTypeParam(List<string> typeParamList)
        {
            List<IComponent> result = [];
            return result;
        }
        #endregion

        #endregion
    }
}