using MinecraftLanguageModelLibrary.Data;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace CBHK.Domain.DataContext
{
    public class MinecraftLanguageCommunicater
    {
        #region Method

        public static async Task<MCDocumentFile?> AnalysisMCDocumentFileOrContent(string filePathOrContent)
        {
            NamedPipeClientStream mcdocumentPiperClientStream = new(".", "MCDocumentLanguageServerPipe", PipeDirection.InOut);
            await mcdocumentPiperClientStream.ConnectAsync();

            byte[] pathBytes = Encoding.UTF8.GetBytes(filePathOrContent);
            byte[] lengthBytes = BitConverter.GetBytes(pathBytes.Length);

            // 先发长度
            await mcdocumentPiperClientStream.WriteAsync(lengthBytes);
            // 再发数据
            await mcdocumentPiperClientStream.WriteAsync(pathBytes);

            // 读取长度前缀
            byte[] lenBuf = new byte[4];
            await mcdocumentPiperClientStream.ReadExactlyAsync(lenBuf, 0, 4);
            int resultLength = BitConverter.ToInt32(lenBuf, 0);

            byte[] resultArray = new byte[resultLength];
            await mcdocumentPiperClientStream.ReadExactlyAsync(resultArray, 0, resultLength);

            string json = Encoding.UTF8.GetString(resultArray);
            MCDocumentFile? result = JsonSerializer.Deserialize<MCDocumentFile>(json);
            return result;
        }

        public static async Task<string> AnalysisMCFunctionFileOrContent(string filePathOrContent)
        {
            NamedPipeClientStream mcfunctionPiperClientStream = new(".", "MCFunctionLanguageServerPipe", PipeDirection.InOut);
            byte[] dataArray = Encoding.Default.GetBytes(filePathOrContent);
            await mcfunctionPiperClientStream.WriteAsync(dataArray);

            // 先读取 4 字节长度
            byte[] lenBuf = new byte[4];
            await mcfunctionPiperClientStream.ReadAsync(lenBuf, 0, 4);
            int resultLength = BitConverter.ToInt32(lenBuf, 0);

            // 再读取数据
            byte[] resultArray = new byte[resultLength];
            await mcfunctionPiperClientStream.ReadAsync(resultArray);

            string json = Encoding.UTF8.GetString(resultArray, 0, resultLength);
            string? result = JsonSerializer.Deserialize<string>(json);
            return result ?? "";
        }
        #endregion
    }
}
