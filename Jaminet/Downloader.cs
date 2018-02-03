using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Jaminet
{
    public class Downloader
    {
        private int timeOut;

        public Downloader(int timeOutSeconds = 5)
        {
            timeOut = timeOutSeconds;
        }

        public string GetPage(string url)
        {
            Task<string> task = GetPageAsync(url);
            return task.Result;
        }

        public async Task<string> GetPageAsync(string url)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.Timeout = new TimeSpan(0, 0, timeOut);

            HttpResponseMessage response = await httpClient.GetAsync(url);

            return await response.Content.ReadAsStringAsync();
        }





        public async Task<long> XDownloadAsync(string url, string fileName)
        {
            long length = 0;
            HttpClient httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromDays(1);
            //httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue();

            Stream response = await httpClient.GetStreamAsync(url);
            using (var fs = new FileStream(fileName, FileMode.Create))
            {
                await response.CopyToAsync(fs);
                length = fs.Length;
            }
            return length;
        }


        public long Download(string fileName, string url, string login = null, string password = null)
        {
            NetworkCredential credential = null;

            if (login != null && password != null)
            {
                credential = new NetworkCredential(login, password);
            }

            Task<long> task = DownloadAsync(url, fileName, credential);
            return task.Result;
        }


        public async Task<long> DownloadAsync(string url, string fileName, NetworkCredential credential)
        {
            byte[] buffer = new byte[8192];

            bool endOfStream = false;
            long totalRead = 0;
            long totalReads = 0;

            HttpClientHandler httpClientHandler = new HttpClientHandler()
            {
                Credentials = credential,
                UseDefaultCredentials = false,
                PreAuthenticate = true,
                UseProxy = false
            };

            HttpClient httpClient = new HttpClient(httpClientHandler);
            httpClient.Timeout = TimeSpan.FromHours(1);

            Console.WriteLine("Downloading {0}", url);

            HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using (Stream cs = await response.Content.ReadAsStreamAsync())
            {
                using (var fs = new FileStream(fileName, FileMode.Create))
                {
                    do
                    {
                        int read = await cs.ReadAsync(buffer, 0, buffer.Length);
                        if (read == 0)
                        {
                            endOfStream = true;
                        }
                        else
                        {
                            await fs.WriteAsync(buffer, 0, read);

                            totalRead += read;
                            totalReads++;

                            if (totalReads % 1000 == 0)
                            {
                                Console.Write("{0} MB... ", totalRead/(1024*1024));
                            }
                            
                        }
                    } while (!endOfStream);
                }
            }
            Console.WriteLine("\nDownload finished ({0} Bytes)", totalRead);

            return totalRead;
        }

    }
}
