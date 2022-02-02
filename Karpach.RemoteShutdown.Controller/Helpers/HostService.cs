using System;
using System.Threading;
using System.Threading.Tasks;
using Karpach.RemoteShutdown.Controller.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Karpach.RemoteShutdown.Controller.Helpers
{
    public class HostService : IHostHelper
    {
        private readonly ITrayCommandHelper _trayCommandHelper;
        private readonly ILog _logger;

        private CancellationTokenSource _cancellationTokenSource;
        private Task _hostTask;

        public string SecretCode { get; set; }
        public TrayCommandType DefaultCommand { get; set; }

        public HostService(ITrayCommandHelper trayCommandHelper, ILog logger)
        {
            _trayCommandHelper = trayCommandHelper;
            _logger = logger;

            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task Start(int port)
        {
            await this.Stop();

            _cancellationTokenSource = new CancellationTokenSource();

            _hostTask = Task.Run(() => this.CreateHost(port), _cancellationTokenSource.Token);
        }

        public async Task Stop()
        {
            if (_hostTask != null)
            {
                _logger.Info("Stopping http service...");

                _cancellationTokenSource.Cancel();
                await _hostTask.ConfigureAwait(false);
            }
        }

        internal async Task ProcessRequestAsync(string url)
        {
            try
            {
                await Task.Delay(1000).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(SecretCode) && !url.StartsWith($"/{SecretCode}/"))
                {
                    return;
                }
                int lastSlashPosition = url.LastIndexOf("/", StringComparison.Ordinal);
                string commandName = String.Empty;
                if (lastSlashPosition >= 0 && url.Length > 1)
                {
                    commandName = url.Substring(lastSlashPosition + 1);
                }
                TrayCommandType? commandType = _trayCommandHelper.GetCommandType(commandName);
                if (!string.IsNullOrEmpty(commandName) && commandType == null)
                {
                    return;
                }
                commandType = commandType ?? DefaultCommand;
                _trayCommandHelper.RunCommand(commandType.Value);
            }
            catch (Exception e)
            {
                _logger.Error("Error processing http request.", e);
            }
        }

        private void CreateHost(int port)
        {
            _logger.Info("Starting http service...");

            try
            {
                var host = new WebHostBuilder()
                        .UseUrls($"http://+:{port}")
                        .UseKestrel()
                        .Configure(app =>
                        {
                            app.Run(async context =>
                            {
#pragma warning disable 4014
                            ProcessRequestAsync(context.Request.Path.Value);
#pragma warning restore 4014
                            await context.Response.WriteAsync("Ok");
                            });
                        })
                        .Build();
                host.Run(_cancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                _logger.Error("Error starting http service.", e);
            }
        }
    }
}