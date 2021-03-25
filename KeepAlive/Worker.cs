using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using KeepAlive.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KeepAlive
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private SitesConfig _sitesConfig;
        private List<Trigger> _triggers = new List<Trigger>();

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

            await ProcessJsonAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                //check to see ifthe sites have been added to the triggers list
                if (_triggers != null && _triggers.Count > 0)
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                    var dueTriggers = _triggers.Where(x => x.Due < DateTime.Now);

                    if (!dueTriggers.Any())
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

                        continue;
                    }
                        
                    foreach (var trigger in dueTriggers)
                    {
                        try
                        {
                            var site = trigger.Site;

                            try
                            {
                                using (var webClient = new WebClient())
                                {

                                    if (site.IsSiteMap)
                                    {
                                        try
                                        {
                                            //find the sitemap entries
                                            _logger.LogInformation($"Connecting to sitemap at: {site.Url}");

                                            var result = await webClient.DownloadStringTaskAsync(new Uri(site.Url));

                                            if (!string.IsNullOrWhiteSpace(result))
                                                await ProcessSiteMapAsync(result, webClient);
                                        }
                                        catch (Exception ex)
                                        {
                                            //fall back to simple download
                                            _logger.LogInformation($"Connecting to: {site.Url}");

                                            var result = await webClient.DownloadDataTaskAsync(new Uri(site.Url));
                                        }


                                    }
                                    else
                                    {
                                        _logger.LogInformation($"Connecting to: {site.Url}");

                                        var result = await webClient.DownloadDataTaskAsync(new Uri(site.Url));
                                    }
                                }
                            }
                            catch (Exception ex)
                            {

                            }

                            trigger.Due = DateTime.Now.AddMinutes(site.Interval);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }

                    }

                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                else
                {
                    _logger.LogInformation("Worker building site Urls at: {time}", DateTimeOffset.Now);

                    if (_sitesConfig.Sites.Any())
                    {
                        var validSites = _sitesConfig.Sites.Where(x => !string.IsNullOrWhiteSpace(x.Url));

                        if (validSites.Any())
                        {
                            foreach (var site in validSites)
                            {
                                _triggers.Add(new Trigger()
                                {
                                    Site = site,
                                    Due = DateTime.Now,
                                });
                            }
                        }

                    }
                    else
                    {
                        try
                        {
                            string pingUrl = Environment.GetEnvironmentVariable("KEEP_ALIVE_URL");
                            _logger.LogInformation(pingUrl);

                            if (!string.IsNullOrEmpty(pingUrl))
                            {
                                _triggers.Add(new Trigger()
                                {
                                    Site = new SiteConfig()
                                    {
                                        Url = pingUrl,
                                        IsSiteMap = isSiteMap,
                                        Interval = duration,
                                    },
                                    Due = DateTime.Now,
                                });
                            }

                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation($"Worker exception at: {DateTimeOffset.Now} - {ex.Message}");

                        }

                    }
                }

            }
        }

        private async Task ProcessSiteMapAsync(string xmlData, WebClient client)
        {
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlData);

                var root = xmlDoc.DocumentElement;


                var urls = root.GetElementsByTagName("loc");

                if (urls.Count > 0)
                {
                   foreach (XmlNode node in urls)
                    {
                        var address = node.InnerText;

                        _logger.LogInformation($"Connecting to: {address}");

                        var result = await client.DownloadDataTaskAsync(new Uri(address));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            




        }

        private async Task ProcessJsonAsync()
        {
            var outPath = @"/keepaliveconfig";
            if (Directory.Exists(outPath))
            {
                
                var configFile = Path.Combine(outPath, "siteconfig.json");

                if (!File.Exists(configFile))
                {
                    //generate base file
                    await GenerateJsonConfigAsync(configFile);
                }

                var jString = await File.ReadAllTextAsync(configFile);

                var sitesConfig = JsonSerializer.Deserialize<SitesConfig>(jString);

                _sitesConfig = sitesConfig;

            }
        }

        private async Task GenerateJsonConfigAsync(string filePath)
        {
            var sites = new Models.SitesConfig()
            {
                Sites = new List<Models.SiteConfig>()
                {
                    new Models.SiteConfig()
                    {
                        Url = "",
                        IsSiteMap = true,
                        Interval = 15,
                    }
                }
            };

            var jString = JsonSerializer.Serialize<Models.SitesConfig>(sites, new JsonSerializerOptions()
            {
                WriteIndented = true,
            });

            await File.WriteAllTextAsync(filePath, jString);


        }
    }
}
