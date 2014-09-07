using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace KuaFu
{
    class IwencaiContext
    {
        public T GetList<T>(string keyWord)
        {
            const string urlTemplate = "http://www.iwencai.com/stockpick/search?preParams=&ts=1&f=1&qs=1&selfsectsn=&querytype=&searchfilter=&tid=stockpick&w={0}";
            var url = string.Format(urlTemplate, keyWord);
            InvokeHttpRequest(url);

            throw new NotImplementedException();
        }

        private static string InvokeHttpRequest(string url)
        {
            url = Uri.EscapeUriString(url);

            while (true)
            {
                try
                {
                    var request = (HttpWebRequest) WebRequest.Create(url);
                    var response = (HttpWebResponse) request.GetResponse();
                    Debug.WriteLine(response.StatusCode);
                    if (response.StatusCode.Equals(HttpStatusCode.OK))
                    {
                        string responseValue;
                        using (var responseStream = response.GetResponseStream())
                        {
                            if (responseStream != null)
                            {
                                using (var reader = new StreamReader(responseStream))
                                {
                                    responseValue = reader.ReadToEnd();
                                }
                            }
                            else
                            {
                                throw new InvalidOperationException("返回数据有误");
                            }
                        }

                        Debug.WriteLine(responseValue);
                        return responseValue;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
        }
    }
}
