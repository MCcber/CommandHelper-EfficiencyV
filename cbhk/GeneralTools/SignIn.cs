﻿using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace cbhk.GeneralTools
{
    public class SignIn
    {
		[Obsolete]
		public static string GetDataByPost(string account, string password)
		{
			string content = "https://api.metamo.cn/market/open/verifyEmail?token=0e805eb9ea5b2d7a266f29af992704c9&email=" + account+"&password="+password;
            using WebClient webClient = new();
            webClient.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded; charset=utf-8";
            string result = webClient.DownloadString(content);
            //string result = webClient.UploadString(url, content);
            return result;
        }

		public static string GetDataByGet(string url, string postDatastr)
		{
			string result = "";
			//try
			//{
			//	ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
			//	ServicePointManager.DefaultConnectionLimit = 200;
			//	//ServicePointManager.ServerCertificateValidationCallback = (object < p0 >, X509Certificate<p1>, X509Chain<p2>, SslPolicyErrors<p3>) => true;
			//	HttpWebRequest request2 = WebRequest.Create(url + ((postDatastr == "") ? "" : "?") + postDatastr) as HttpWebRequest;
			//	request2.Method = "GET";
			//	request2.Timeout = 5000;
			//	request2.KeepAlive = false;
			//	request2.ContentType = "application/json; charset=utf-8";
			//	HttpWebResponse response2 = request2.GetResponse() as HttpWebResponse;
			//	Stream result_stream = response2.GetResponseStream();
			//	StreamReader result_reader = new StreamReader(result_stream, new UTF8Encoding(false));
			//	result = result_reader.ReadToEnd();
			//	result_stream.Close();
			//	result_reader.Close();
			//	response2.Close();
			//	response2 = null;
			//	request2.Abort();
			//	request2 = null;
			//}
			//catch (Exception e)
			//{
			//             MessageBox.Show(e.Message);
			//}
			return result;
		}

		/// <summary>
		/// 设置证书
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="certificate"></param>
		/// <param name="chain"></param>
		/// <param name="errors"></param>
		/// <returns></returns>
		public static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
		{   // 总是接受  
			return true;
		}

		/// <summary>
		/// 下载用户头像到本地指定路径
		/// </summary>
		/// <param name="target_url"></param>
		/// <param name="target_file_path"></param>
		/// <returns></returns>
        public async static Task<bool> DownLoadUserImage(string target_url, string target_file_path)
        {
			//头像为空
			if (target_url == "/lib/img/default/bg-bar.jpg")
				return false;
            //验证服务器证书回调自动验证
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
            HttpWebRequest Myrq = WebRequest.Create(target_url) as HttpWebRequest;
            Myrq.KeepAlive = false;//持续连接
            Myrq.Timeout = 30 * 1000;//30秒，*1000是因为基础单位为毫秒
            Myrq.Method = "GET";//请求方法
            Myrq.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3";//network里面找
            Myrq.UserAgent = "Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/75.0.3770.100 Safari/537.36";

            //接受返回
            try
            {
                HttpWebResponse Myrp = await Myrq.GetResponseAsync() as HttpWebResponse;

                if (Myrp.StatusCode != HttpStatusCode.OK)
                { return false; }

                FileStream fs = new(target_file_path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);//展开一个流
                Myrp.GetResponseStream().CopyTo(fs);//复制到当前文件夹
                Myrp.Close();
                fs.Close();
            }
            catch { }
            return true;
        }
	}
}