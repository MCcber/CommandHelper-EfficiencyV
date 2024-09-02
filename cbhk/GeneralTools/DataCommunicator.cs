using System;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace cbhk.GeneralTools
{
    public class DataCommunicator
    {
        private static SQLiteConnection connection = new("Data Source=" + AppDomain.CurrentDomain.BaseDirectory + "Minecraft.db");
        //private static NamedPipeClientStream pipeClient = null;

        /// <summary>
        /// 进程锁
        /// </summary>
        private static object obj = new();

        /// <summary>
        /// 单例,避免指令重排
        /// </summary>
        private volatile static DataCommunicator dataCommunicator;

        /// <summary>
        /// 获取通信器实例，建立与管道服务器的连接
        /// </summary>
        /// <returns></returns>
        public static DataCommunicator GetDataCommunicator()
        {
            if (dataCommunicator is null)
            {
                lock (obj)
                {
                    if (dataCommunicator is null)
                    {
                        dataCommunicator ??= new DataCommunicator();
                        //pipeClient = new(".", "cbhkDataSource", PipeDirection.InOut);
                    }
                }
            }
            return dataCommunicator;
        }

        public static void CloseCommunicator()
        {
            connection.Close();
        }

        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        //public async Task<DataTable> GetData(string cmd)
        //{
        //    ProcessStartInfo processStartInfo = new()
        //    {
        //        FileName = "cbhkDataServer.exe",
        //        CreateNoWindow = true,
        //        WindowStyle = ProcessWindowStyle.Hidden,
        //        UseShellExecute = false,
        //        Arguments = "\"" + cmd + "\""
        //    };
        //    Process process = new();
        //    process.ErrorDataReceived += (sender, args) =>
        //    {
        //        MessageBox.Show(args.Data);
        //    };
        //    process.OutputDataReceived += (sender, args) =>
        //    {
        //        MessageBox.Show(args.Data);
        //    };
        //    process.StartInfo = processStartInfo;
        //    process.Start();
        //    pipeClient.Connect();
        //    pipeClient.ReadMode = PipeTransmissionMode.Byte;
        //    using MemoryStream ms = new();
        //    byte[] buffer = new byte[512000];
        //    int bytesRead;
        //    while ((bytesRead = await pipeClient.ReadAsync(buffer)) > 0)
        //    {
        //        ms.Write(buffer, 0, bytesRead);
        //        if (!pipeClient.IsConnected) break;
        //        Array.Clear(buffer);
        //    }
        //    byte[] responseData = ms.ToArray();
        //    DataTable result = OnDataReceived(responseData);
        //    return result;
        //}

        public async Task<DataTable> GetData(string cmd)
        {
            if (connection.State is ConnectionState.Closed)
                connection.Open();
            SQLiteCommand command = new(cmd, connection);
            DbDataReader dataReader = await command.ExecuteReaderAsync();
            DataTable dataTable = new();
            dataTable.Load(dataReader);
            return dataTable;
        }

        /// <summary>
        /// 字节数据转DataTable实例
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        //private DataTable OnDataReceived(byte[] data)
        //{
        //    string result = Encoding.Default.GetString(data).Trim('\\').Replace("\\{", "{").Replace("\\}", "}").Replace("\\[", "[").Replace("\\]", "]").Replace("\\+","\\\\+");
        //    JsonSerializerSettings settings = new()
        //    {
        //        StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
        //    };
        //    settings.Converters.Add(new BinaryConverter());
        //    DataTable dataTable = JsonConvert.DeserializeObject<DataTable>(result, settings);
        //    return dataTable;
        //}

        /// <summary>
        /// 执行非查询操作
        /// </summary>
        /// <param name="cmd"></param>
        public async Task ExecuteNonQuery(string cmd)
        {
            if (connection.State is ConnectionState.Closed)
                connection.Open();
            SQLiteCommand command = new(cmd, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}