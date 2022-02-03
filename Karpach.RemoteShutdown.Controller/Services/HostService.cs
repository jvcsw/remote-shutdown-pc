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
                    TrayCommandType? commandType = trayCommandHelper.GetCommandType(commandName);
                    if (!string.IsNullOrEmpty(commandName) && commandType == null)
                    {
                        return;
                    }

                    commandType = commandType ?? DefaultCommand;

                    this.logger.Info($"Executing '{commandType}' operation...");

                    trayCommandHelper.RunCommand(commandType.Value);
                }
                else 
                {
                    this.logger.Info($"Operation cancelled.");
                }
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

        private bool ExistBlockingProcessRunning()
        {
            if (this.BlockingProcesses != null && this.BlockingProcesses.Any())
            {
                Process[] processes = Process.GetProcesses();

                foreach (string process in this.BlockingProcesses)
                {
                    if (this.IsProcessRunning(processes, process))
                    {
                        return true;
                    }
                }
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
                this.logger.Info($"Detected process running: '{process}'");
                return true;
            }

            return false;
        }
    }
}