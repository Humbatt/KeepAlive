using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TurnerSoftware.SitemapTools;

namespace KeepAlive
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting KeepAlive worker");

            string durationStr = Environment.GetEnvironmentVariable("KEEP_ALIVE_DELAY");
            string isSitemapStr = Environment.GetEnvironmentVariable("URL_IS_SITEMAP");

            _logger.LogInformation($"Duration: {durationStr}");
            _logger.LogInformation($"Is Sitemap: {isSitemapStr}");

            var duration = 0;
            var isSitemapInt = 0;

            if (!int.TryParse(durationStr, out duration))
                duration = 5;

            if (!int.TryParse(isSitemapStr, out isSitemapInt))
                isSitemapInt = 0;

            var isSiteMap = (isSitemapInt == 1) ? true : false;


            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    string pingUrl = Environment.GetEnvironmentVariable("KEEP_ALIVE_URL");
                    _logger.LogInformation(pingUrl);

                    if (!string.IsNullOrEmpty(pingUrl))
                    {
                        using (var webClient = new WebClient())
                        {
                            if (isSiteMap)
                            {
                                try
                                {
                                    //find the sitemap entries
                                    var sitemapQuery = new SitemapQuery();
                                    var sitemapEntries = await sitemapQuery.GetSitemapAsync(new Uri(pingUrl));

                                    if (sitemapEntries.Urls.Any())
                                    {
                                        foreach (var entry in sitemapEntries.Urls)
                                        {
                                            var result = await webClient.DownloadDataTaskAsync(entry.Location);
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                    //fall back to simple download
                                    _logger.LogInformation($"Connecting to: {pingUrl}");

                                    var result = await webClient.DownloadDataTaskAsync(new Uri(pingUrl));
                                }


                            }
                            else
                            {
                                _logger.LogInformation($"Connecting to: {pingUrl}");

                                var result = await webClient.DownloadDataTaskAsync(new Uri(pingUrl));
                            }
                        }
                            
                        
                    }


                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Worker exception at: {DateTimeOffset.Now} - {ex.Message}");

                }
               

                
                await Task.Delay(TimeSpan.FromMinutes(duration), stoppingToken);


            }
        }

        private async Task ProcessSiteMapAsync(string xmlData)
        {

            

        }
    }
}
