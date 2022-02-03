namespace Karpach.RemoteShutdown.Controller.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
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

        public IList<string> BlockingProcesses { get; set; }

        public TrayCommandType DefaultCommand { get; set; }

        public HostService(ITrayCommandHelper trayCommandHelper, ILog logger)
        {
            this.trayCommandHelper = trayCommandHelper ?? throw new ArgumentNullException(nameof(trayCommandHelper));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task Start(int port)
        {
            await this.Stop();

            this.cancellationTokenSource = new CancellationTokenSource();

            this.hostTask = Task.Run(() => this.CreateHost(port), this.cancellationTokenSource.Token);
        }

        public async Task Stop()
        {
            if (this.hostTask != null)
            {
                logger.Info("Stopping http service...");

                this.cancellationTokenSource.Cancel();
                await this.hostTask.ConfigureAwait(false);

                this.logger.Info("Http service stopped.");
            }
        }

        private void CreateHost(int port)
        {
            this.logger.Info("Starting http service...");

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
                                this.ProcessRequestAsync(context.Request.Path.Value);
#pragma warning restore 4014
                                await context.Response.WriteAsync("Ok");
                            });
                        })
                        .Build();

                host.Run(this.cancellationTokenSource.Token);

                this.logger.Info($"Http service started.");
            }
            catch (Exception e)
            {
                this.logger.Error("Error starting http service.", e);
            }
        }

        internal async Task ProcessRequestAsync(string url)
        {
            try
            {
                this.logger.Info($"Processing '{url}' request...");

                if (!this.ExistBlockingProcessRunning())
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
                    TrayCommandType? commandType = this.trayCommandHelper.GetCommandType(commandName);
                    if (!string.IsNullOrEmpty(commandName) && commandType == null)
                    {
                        return;
                    }

                    commandType = commandType ?? this.DefaultCommand;

                    this.logger.Info($"Executing '{commandType}' request...");

                    this.trayCommandHelper.RunCommand(commandType.Value);

                    this.logger.Info($"Request executed.");
                }
                else 
                {
                    this.logger.Info($"Request cancelled.");
                }
            }
            catch (Exception e)
            {
                this.logger.Error("Error on processing the request.", e);
            }
        }

        private bool ExistBlockingProcessRunning()
        {
            if (this.BlockingProcesses != null && this.BlockingProcesses.Any())
            {
                this.logger.Info($"Checking blocking processes...");

                Process[] processes = Process.GetProcesses();

                foreach (string process in this.BlockingProcesses)
                {
                    if (this.IsProcessRunning(processes, process))
                    {
                        return true;
                    }
                }

                this.logger.Info($"No blocking processes found.");
            }
            else
            {
                this.logger.Info($"No blocking processes configured.");
            }

            return false;
        }

        private bool IsProcessRunning(Process[] processes, string process)
        {
            if (string.IsNullOrWhiteSpace(process))
            {
                return false;
            }

            if (processes.Any(o => o.ProcessName.ToLower().Contains(process.ToLower())))
            {
                this.logger.Info($"Detected a blocking process running: '{process}'");
                return true;
            }

            return false;
        }
    }
}