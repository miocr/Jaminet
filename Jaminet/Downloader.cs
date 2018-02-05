using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Jaminet
{
    public static class Downloader
    {
        //public static int TimeOut { get; set; }

        //public Downloader(int timeOutSeconds = 15)
        //{
        //    TimeOut = timeOutSeconds;
        //}

        public static string GetPage(string url)
        {
            Task<string> task = GetPageAsync(url);
            return task.Result;
        }

        private static async Task<string> GetPageAsync(string url)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.Timeout = new TimeSpan(0, 0, 15);
                using (HttpResponseMessage response = await httpClient.GetAsync(url))
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }

        public static long Download(string fileName, string url, string login = null, string password = null)
        {
            NetworkCredential credential = null;

            if (login != null && password != null)
            {
                credential = new NetworkCredential(login, password);
            }

            Task<long> task = DownloadAsync(url, fileName, credential);
            return task.Result;
        }

        private static async Task<long> DownloadAsync(string url, string fileName, NetworkCredential credential)
        {
            byte[] buffer = new byte[8192];

            bool endOfStream = false;
            long totalRead = 0;
            long totalReads = 0;

            using (HttpClientHandler httpClientHandler = new HttpClientHandler())
            {
                httpClientHandler.Credentials = credential;
                httpClientHandler.UseDefaultCredentials = false;
                httpClientHandler.PreAuthenticate = true;
                httpClientHandler.UseProxy = false;


                using (HttpClient httpClient = new HttpClient(httpClientHandler))
                {
                    httpClient.Timeout = TimeSpan.FromHours(1);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Downloading from url '{0}' to '{1}'", url, fileName);

                    HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();

                    using (Stream cs = await response.Content.ReadAsStreamAsync())
                    {
                        using (var fs = new FileStream(fileName, FileMode.Create))
                        {
                            Console.Write("{0} MB... ", 0);
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
                                        Console.Write("{0} MB... ", totalRead / (1024 * 1024));
                                    }

                                }
                            } while (!endOfStream);
                        }
                    }
                }
            }
            Console.WriteLine("\nDownload finished ({0} Bytes)", totalRead);
            Console.ResetColor();

            return totalRead;
        }

    }
}
