using MinecraftLanguageModelLibrary.Data;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CBHK.Domain.DataContext
{
    /// <summary>语言服务器不可用 / 往返失败。调用方按"这一个文档失败"处理，不要让它掀翻整张符号表。</summary>
    public sealed class McdocLanguageServerException : Exception
    {
        public McdocLanguageServerException(string message, Exception? inner = null) : base(message, inner) { }
    }

    public class MinecraftLanguageCommunicater
    {
        private static readonly JsonSerializerOptions DeserializeOptions = new() { PropertyNameCaseInsensitive = true };

        /// <summary>连不上管道就等这么久。没超时的话服务器没起时 Init 会永久挂起（骨架屏不消失）。</summary>
        public static TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>单次请求的读写预算：服务器中途死掉时不会把 UI 线程挂住。</summary>
        public static TimeSpan IoTimeout { get; set; } = TimeSpan.FromSeconds(30);

        #region Method
        public static async Task<MCDocumentFile?> AnalysisMCDocumentFileOrContent(
            string filePathOrContent,
            string baseDocumentPath,
            CancellationToken cancellationToken = default)
            => (await AnalysisMCDocumentFileOrContentWithRaw(filePathOrContent, baseDocumentPath, cancellationToken)).File;

        /// <summary>
        /// 同上，但把服务器**原始 JSON** 一起交出去：磁盘缓存要存的就是这一份（反序列化走同一份选项，
        /// 零保真风险）。没有响应时 Raw 为 null。
        /// </summary>
        public static async Task<(MCDocumentFile File, string Raw)> AnalysisMCDocumentFileOrContentWithRaw(
            string filePathOrContent,
            string baseDocumentPath,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await SendAsync(filePathOrContent, baseDocumentPath, cancellationToken);
            }
            catch (Exception error) when (error is IOException or TimeoutException && !cancellationToken.IsCancellationRequested)
            {
                // 服务器可能没起 / 刚崩：拉一次再重试。二次失败交给调用方记账。
                McdocLanguageServerStatus status = McdocLanguageServerHost.Shared.EnsureRunning();
                if (!status.IsRunning)
                {
                    throw new McdocLanguageServerException(status.Reason, error);
                }

                return await SendAsync(filePathOrContent, baseDocumentPath, cancellationToken);
            }
        }

        private static async Task<(MCDocumentFile File, string Raw)> SendAsync(string filePathOrContent, string baseDocumentPath, CancellationToken cancellationToken)
        {
            if (filePathOrContent is null)
            {
                throw new ArgumentNullException(nameof(filePathOrContent));
            }

            if (baseDocumentPath is null || baseDocumentPath.Contains('+') || filePathOrContent.Contains('+'))
            {
                // 服务端按第一个 '+' 切帧，路径里带 '+' 会被静默截断，这里直接拒绝
                throw new ArgumentException("文档路径里不能出现 '+'（会破坏管道帧格式）", nameof(baseDocumentPath));
            }

            using NamedPipeClientStream pipe = new(".", McdocLanguageServerHost.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            using (CancellationTokenSource connectSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                connectSource.CancelAfter(ConnectTimeout);
                try
                {
                    await pipe.ConnectAsync(connectSource.Token);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException("连接语言服务器超时（" + (int)ConnectTimeout.TotalSeconds + " 秒）");
                }
            }

            byte[] contentBytes = Encoding.UTF8.GetBytes(baseDocumentPath + '+' + filePathOrContent);
            byte[] lengthBytes = BitConverter.GetBytes(contentBytes.Length);

            using CancellationTokenSource ioSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            ioSource.CancelAfter(IoTimeout);
            try
            {
                await pipe.WriteAsync(lengthBytes, ioSource.Token);
                await pipe.WriteAsync(contentBytes, ioSource.Token);
                await pipe.FlushAsync(ioSource.Token);

                byte[] lenBuf = new byte[4];
                await pipe.ReadExactlyAsync(lenBuf, ioSource.Token);
                int resultLength = BitConverter.ToInt32(lenBuf, 0);
                if (resultLength <= 0)
                {
                    throw new McdocLanguageServerException("语言服务器返回了空响应");
                }

                byte[] resultArray = new byte[resultLength];
                await pipe.ReadExactlyAsync(resultArray, ioSource.Token);

                string json = Encoding.UTF8.GetString(resultArray);
                return (JsonSerializer.Deserialize<MCDocumentFile>(json, DeserializeOptions), json);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException("语言服务器响应超时（" + (int)IoTimeout.TotalSeconds + " 秒）");
            }
        }
        #endregion
    }
}
