using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
            _logger.LogInformation(durationStr);

            var duration = 0;

            if (!int.TryParse(durationStr, out duration))
                duration = 5;


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
                           

                            _logger.LogInformation($"Connecting too: {pingUrl}");

                            var result = await webClient.DownloadDataTaskAsync(new Uri(pingUrl));

                            
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
    }
}
