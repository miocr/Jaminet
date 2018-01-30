using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Jaminet
{
    public class Downloader
    {
        public Downloader()
        {

        }

        public string GetPage(string url)
        {
            Task<string> task = GetPageAsync(url);
            return task.Result;
        }
        public async Task<string> GetPageAsync(string url)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.Timeout = new TimeSpan(0,0,5);

            HttpResponseMessage response = await httpClient.GetAsync(url);

            return await response.Content.ReadAsStringAsync();
        }
    }
}
