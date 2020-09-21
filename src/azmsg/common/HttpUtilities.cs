using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace azmsg.common
{
    class HttpUtilities
    {
        public static async Task<HttpResponseMessage> PostAsync(string url, string data)
        {
            var content = new StringContent(data, Encoding.UTF8, "application/json");

            var client = new HttpClient();

            var response = await client.PostAsync(url, content);

            return response;
        }
    }
}
