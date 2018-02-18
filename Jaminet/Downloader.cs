using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using log4net;

namespace Jaminet
{
    public class Downloader
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Downloader));

        private HttpClient httpClient;
        private HttpResponseMessage httpResponse;
        private HttpClientHandler httpClientHandler;

        public Downloader(string login = null, string password = null)
        {
            httpClientHandler = new HttpClientHandler()
            {
                PreAuthenticate = true,
                UseProxy = false,
                UseDefaultCredentials = true
            };

            if (login != null && password != null)
            {
                httpClientHandler.Credentials = new NetworkCredential(login, password);
                httpClientHandler.UseDefaultCredentials = false;
            }


            httpClient = new HttpClient(httpClientHandler)
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        ~Downloader()
        {
            httpClientHandler.Dispose();
            httpClient.Dispose();
        }

        public string GetPage(string url)
        {
            Task<string> task = GetPageAsync(url);
            return task.Result;
        }

        private async Task<string> GetPageAsync(string url)
        {
            httpResponse = await httpClient.GetAsync(url);
            return await httpResponse.Content.ReadAsStringAsync();
        }

        public long DownloadFile(string url, string fileName)
        {
            Task<long> task = DownloadAsync(url, fileName);
            return task.Result;
        }

        private async Task<long> DownloadAsync(string url, string fileName)
        {
            byte[] buffer = new byte[8192];

            bool endOfStream = false;
            long totalRead = 0;
            long totalReads = 0;

            HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using (Stream cs = await response.Content.ReadAsStreamAsync())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Downloading from url '{0}' to '{1}'", url, fileName);
                Console.ForegroundColor = ConsoleColor.Yellow;

                log.InfoFormat("Downloading from url '{0}' to '{1}'", url, fileName);

                using (FileStream fs = new FileStream(fileName, FileMode.Create))
                {
                    do
                    {
                        if (totalReads % 1000 == 0)
                        {
                            Console.Write("{0} MB... ", totalRead / (1024 * 1024));
                        }

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

                        }
                    } while (!endOfStream);
                }

            }
            
            Console.WriteLine("\nDownload finished ({0} Bytes)", totalRead);
            Console.ResetColor();

            log.InfoFormat("Downloaded {0} bytes from url '{1}' to '{2}'", 
                           totalRead, url, fileName);

            return totalRead;
        }

    }
}
