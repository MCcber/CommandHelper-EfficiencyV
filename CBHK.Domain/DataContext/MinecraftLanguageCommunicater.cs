using MinecraftLanguageModelLibrary.Model.MCDocument;
using Newtonsoft.Json;
using System.IO.Pipes;
using System.Text;

namespace CBHK.Domain.DataContext
{
    public class MinecraftLanguageCommunicater
    {
        #region Field
        public NamedPipeClientStream mcdocumentPiperClientStream = new(".", "MCDocumentLanguageServerPipe", PipeDirection.InOut);
        public NamedPipeClientStream mcfunctionPiperClientStream = new(".", "MCFunctionLanguageServerPipe", PipeDirection.InOut);
        #endregion

        #region Method
        public MinecraftLanguageCommunicater()
        {
            Task.Run(mcdocumentPiperClientStream.ConnectAsync);
            Task.Run(mcfunctionPiperClientStream.ConnectAsync);
        }

        public async Task<MCDocumentFileModel?> AnalysisMCDocumentFileOrContent(string filePathOrContent)
        {
            byte[] dataArray = Encoding.Default.GetBytes(filePathOrContent);
            await mcdocumentPiperClientStream.WriteAsync(dataArray);
            byte[] resultArray = new byte[2048];
            await mcdocumentPiperClientStream.ReadAsync(resultArray);
            MCDocumentFileModel? result = JsonConvert.DeserializeObject<MCDocumentFileModel>(Encoding.Default.GetString(resultArray));
            return result;
        }

        public async Task<string?> AnalysisMCFunctionFileOrContent(string filePathOrContent)
        {
            byte[] dataArray = Encoding.Default.GetBytes(filePathOrContent);
            await mcfunctionPiperClientStream.WriteAsync(dataArray);
            byte[] resultArray = new byte[2048];
            await mcfunctionPiperClientStream.ReadAsync(resultArray);
            string? result = Encoding.Default.GetString(resultArray);
            return result;
        }
        #endregion
    }
}
