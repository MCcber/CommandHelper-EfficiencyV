using CBHK.Domain.Interface;
using MinecraftLanguageModelLibrary.Model.MCDocument;
using MinecraftLanguageModelLibrary.Model.MCDocument.EnumContent;
using MinecraftLanguageModelLibrary.Model.MCDocument.InjectionContent;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Text;
using System.Windows;

namespace CBHK.Domain.Implementation
{
    public class BaseNBTComponentBuilder : INBTComponentBuilder
    {
        #region Field
        private readonly NamedPipeClientStream client = new("MCDocumentLanguageServerPipe");
        #endregion

        #region Property
        public List<IComponent> ComponentList { get; set; } = [];
        public MCDocumentFileModel? DocumentFileModel { get; set; }

        public List<IComponent> GetEnumMetaDataList()
        {
            List<IComponent> result = [];
            return result;
        }
        #endregion

        #region Method
        public async Task<MCDocumentFileModel?> GetMCDocumentFileModel(string targetFilePath)
        {
            if (!client.IsConnected)
            {
                await client.ConnectAsync();
            }
            await client.WriteAsync(Encoding.UTF8.GetBytes(targetFilePath));
            byte[] resultArray = new byte[10240];
            await client.ReadAsync(resultArray);
            string data = Encoding.UTF8.GetString(resultArray);
            if (data.Length > 0)
            {
                DocumentFileModel = JsonConvert.DeserializeObject<MCDocumentFileModel>(data);
            }
            return DocumentFileModel;
        }

        public List<IComponent> GetStructList()
        {
            List<IComponent> result = [];
            if(DocumentFileModel is null)
            {
                return result;
            }
            foreach (var item in DocumentFileModel.StructureList)
            {
                
            }
            return result;
        }

        public List<IComponent> GetUsestatementList()
        {
            List<IComponent> result = [];
            if(DocumentFileModel is null)
            {
                return result;
            }
            Dictionary<string, string> UseStatementDictionary = [];
            foreach (var item in DocumentFileModel.UseStatementList)
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

        #endregion
    }
}