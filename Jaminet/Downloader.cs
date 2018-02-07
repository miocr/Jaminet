using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Jaminet
{
    public static class Downloader
    {

        private static HttpClient httpClient;

        static Downloader()
        {
            httpClient = new HttpClient()
            {
                Timeout = new TimeSpan(0, 0, 10)
            };
        }

        public static string GetPage(string url)
        {
            Task<string> task = GetPageAsync(url);
            return task.Result;
        }

        private static async Task<string> GetPageAsync(string url)
        {
            using (HttpResponseMessage response = await httpClient.GetAsync(url))
            {
                return await response.Content.ReadAsStringAsync();
            }
        }

        public static long Download(string fileName, string url, string login = null, string password = null)
        {
            // pro Download prodlouzime Timeout na hodinu a pak vratime na puvodni hodnotu
            TimeSpan defTimeOut = httpClient.Timeout;
            httpClient.Timeout = new TimeSpan(1, 0, 0);
            NetworkCredential credential = null;

            if (login != null && password != null)
            {
                credential = new NetworkCredential(login, password);
            }

            Task<long> task = DownloadAsync(url, fileName, credential);
            httpClient.Timeout = defTimeOut;
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
            Console.WriteLine("\nDownload finished ({0} Bytes)", totalRead);
            Console.ResetColor();

            return totalRead;
        }

    }
}
