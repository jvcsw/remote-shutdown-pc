﻿namespace Karpach.RemoteShutdown.Controller
{
    using System;
    using System.Windows.Forms;
    using Karpach.RemoteShutdown.Controller.Helpers;
    using Karpach.RemoteShutdown.Controller.Properties;
    using Karpach.RemoteShutdown.Controller.Services;
    using log4net;
    using Microsoft.Win32;

    public class ApplicationController: ApplicationContext
    {
        private readonly ITrayCommandHelper trayCommandHelper;
        private readonly ILog logger;
        private readonly SettingsForm settingsForm;
        private readonly IHostService hostService;

        private NotifyIcon trayIcon;        
        private ToolStripMenuItem commandButton;

        public ApplicationController(ITrayCommandHelper trayCommandHelper, SettingsForm settingsForm, IHostService hostService, ILog logger)
        {
            this.trayCommandHelper = trayCommandHelper ?? throw new ArgumentNullException(nameof(trayCommandHelper));
            this.settingsForm = settingsForm ?? throw new ArgumentNullException(nameof(settingsForm));
            this.hostService = hostService ?? throw new ArgumentNullException(nameof(hostService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.InitializeController();

            System.Threading.Thread.Sleep(100); // Delay for write traces in order
            this.logger.Info("Application started.");
        }

        private void InitializeController()
        {
            var notifyContextMenu = new ContextMenuStrip();

            this.commandButton = new ToolStripMenuItem(this.trayCommandHelper.GetText((TrayCommandType)Settings.Default.DefaultCommand))
            {
                Image = Resources.Shutdown.ToBitmap()
            };

            this.commandButton.Click += ShutDownClick;
            notifyContextMenu.Items.Add(this.commandButton);

            notifyContextMenu.Items.Add("-");

            var settings = new ToolStripMenuItem("Settings")
            {
                Image = Resources.Settings.ToBitmap()
            };
            settings.Click += SettingsClick;
            notifyContextMenu.Items.Add(settings);

            notifyContextMenu.Items.Add("-");

            var exit = new ToolStripMenuItem("Exit")
            {
                Image = Resources.Exit.ToBitmap()
            };
            exit.Click += Exit;
            notifyContextMenu.Items.Add(exit);


            // Initialize Tray Icon            
            this.trayIcon = new NotifyIcon
            {
                Icon = Resources.AppIcon,
                ContextMenuStrip = notifyContextMenu,
                Visible = true
            };

            this.hostService.SecretCode = Settings.Default.SecretCode;
            this.hostService.CheckBlockingProcesses = Settings.Default.CheckBlockingProcesses;
            this.hostService.BlockingProcesses = Settings.Default.BlockingProcesses.Split(';');
            this.hostService.DefaultCommand = (TrayCommandType)Settings.Default.DefaultCommand;
            this.hostService.Start(Settings.Default.RemotePort);
        }

        private void SettingsClick(object sender, EventArgs e)
        {
            this.logger.Info("Openning settings form...");

            if (settingsForm.ShowDialog() == DialogResult.OK)
            {
                this.logger.Info("Applying settings...");

                if (Settings.Default.RemotePort != this.settingsForm.Port)
                {
                    this.hostService.Start(settingsForm.Port);
                }
                
                if (Settings.Default.AutoStart != this.settingsForm.AutoStart)
                {
                    this.SetAutoStart(settingsForm.AutoStart);
                }

                this.commandButton.Text = this.trayCommandHelper.GetText(this.settingsForm.CommandType);
                Settings.Default.AutoStart = this.settingsForm.AutoStart;
                Settings.Default.CheckBlockingProcesses = this.settingsForm.CheckBlockingProcesses;
                Settings.Default.DefaultCommand = (int)this.settingsForm.CommandType;
                Settings.Default.RemotePort = this.settingsForm.Port;
                Settings.Default.SecretCode = this.settingsForm.SecretCode;                
                Settings.Default.Save();

                // Update host helper
                this.hostService.SecretCode = Settings.Default.SecretCode;
                this.hostService.DefaultCommand = (TrayCommandType)Settings.Default.DefaultCommand;

                this.logger.Info("Settings applied.");
            }

            this.logger.Info("Settings form closed.");
        }

        private void SetAutoStart(bool autoStart)
        {
            RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (autoStart)
            {
                // Add the value in the registry so that the application runs at startup
                rkApp?.SetValue("Karpach.RemoteShutdown", Application.ExecutablePath);

                this.logger.Info("Autostart enabled.");
            }
            else
            {
                rkApp?.DeleteValue("Karpach.RemoteShutdown", false);

                this.logger.Info("Autostart disabled.");
            }
        }

        private void ShutDownClick(object sender, EventArgs e)
        {
            this.logger.Info("Executing shutdown command...");

            this.trayCommandHelper.RunCommand((TrayCommandType)Settings.Default.DefaultCommand);
        }

        void Exit(object sender, EventArgs e)
        {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            this.trayIcon.Visible = false;
            this.hostService.Stop();

            this.logger.Info("Stopping application...");

            Application.Exit();
        }        
    }
}