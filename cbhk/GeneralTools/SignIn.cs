using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace cbhk.GeneralTools
{
    public class SignIn
    {
		public async static Task<string> GetDataByPost(string account, string password)
		{
			string content = "https://api.metamo.cn/market/open/verifyEmail?token=0e805eb9ea5b2d7a266f29af992704c9&email=" + account+"&password="+password;

			HttpClient httpClient = new();
			httpClient.DefaultRequestHeaders.Add("ContentType", "application/x-www-form-urlencoded; charset=utf-8");
			var response = await httpClient.PostAsync(content, /*new StringContent(content, Encoding.UTF8, "application/x-www-form-urlencoded")*/null);
			response.EnsureSuccessStatusCode();
			string result = await response.Content.ReadAsStringAsync();
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