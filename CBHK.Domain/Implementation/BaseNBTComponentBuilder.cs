using CBHK.Domain.Interface;
using CBHK.Interface;
using MinecraftLanguageModelLibrary.Model.MCDocument;
using MinecraftLanguageModelLibrary.Model.MCDocument.EnumContent;
using MinecraftLanguageModelLibrary.Model.MCDocument.InjectionContent;
using Newtonsoft.Json;
using System.IO.Pipes;
using System.Text;
using System.Windows;

namespace CBHK.Domain.Implementation
{
    public class BaseNBTComponentBuilder : INBTComponentBuilder
    {
        #region Field
        private NamedPipeClientStream client = new("MCDocumentLanguageServerPipe");
        #endregion

        #region Property
        public List<IComponent> ComponentList { get; set; } = [];
        #endregion

        #region Method
        public BaseNBTComponentBuilder()
        {
            Task.Run(client.ConnectAsync);
        }

        public async Task<MCDocumentFileModel?> GetMCDocumentFileModel(string targetFilePath)
        {
            MCDocumentFileModel? result = new();
            await client.WriteAsync(Encoding.UTF8.GetBytes(targetFilePath));
            byte[] resultArray = new byte[10240];
            await client.ReadAsync(resultArray);
            string data = Encoding.UTF8.GetString(resultArray);
            if (data.Length > 0)
            {
                result = JsonConvert.DeserializeObject<MCDocumentFileModel>(data);
            }
            return result;
        }

        public List<FrameworkElement> BuildNBTComponent()
        {
            return [];
        }

        public List<DispatchStatement> GetDispatchStatementList()
        {
            return [];
        }

        public List<EnumField> GetEnumField()
        {
            return [];
        }

        public List<EnumInjection> GetEnumInjectionList()
        {
            return [];
        }

        public List<Enumeration> GetEnumList()
        {
            return [];
        }

        public List<Injection> GetInjectionList()
        {
            return [];
        }

        public List<StructField> GetStructFieldList()
        {
            return [];
        }

        public List<StructInjection> GetStructInjectionList()
        {
            return [];
        }

        public List<Structure> GetStructureList()
        {
            return [];
        }

        public List<TypeAlia> GetTypeAliaList()
        {
            return [];
        }

        public List<string> GetUseStatementList()
        {
            return [];
        }
        #endregion
    }
}