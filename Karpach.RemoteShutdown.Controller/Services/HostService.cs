namespace Karpach.RemoteShutdown.Controller.Services
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Karpach.RemoteShutdown.Controller.Helpers;
    using log4net;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;

    public class HostService : IHostService
    {
        private readonly ITrayCommandHelper trayCommandHelper;
        private readonly ILog logger;

        private CancellationTokenSource cancellationTokenSource;
        private Task hostTask;

        public string SecretCode { get; set; }

        public TrayCommandType DefaultCommand { get; set; }

        public HostService(ITrayCommandHelper trayCommandHelper, ILog logger)
        {
            this.trayCommandHelper = trayCommandHelper;
            this.logger = logger;

            cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task Start(int port)
        {
            await this.Stop();

            cancellationTokenSource = new CancellationTokenSource();

            hostTask = Task.Run(() => this.CreateHost(port), cancellationTokenSource.Token);
        }

        public async Task Stop()
        {
            if (hostTask != null)
            {
                logger.Info("Stopping http service...");

                cancellationTokenSource.Cancel();
                await hostTask.ConfigureAwait(false);
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
                TrayCommandType? commandType = trayCommandHelper.GetCommandType(commandName);
                if (!string.IsNullOrEmpty(commandName) && commandType == null)
                {
                    return;
                }
                commandType = commandType ?? DefaultCommand;
                trayCommandHelper.RunCommand(commandType.Value);
            }
            catch (Exception e)
            {
                logger.Error("Error processing http request.", e);
            }
        }

        private void CreateHost(int port)
        {
            logger.Info("Starting http service...");

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
                host.Run(cancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                logger.Error("Error starting http service.", e);
            }
        }
    }
}