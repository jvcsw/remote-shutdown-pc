﻿using System;
using System.Windows.Forms;
using Karpach.RemoteShutdown.Controller.Interfaces;
using Karpach.RemoteShutdown.Controller.Properties;
using log4net;
using Microsoft.Win32;

namespace Karpach.RemoteShutdown.Controller
{
    public class ControllerApplicationContext: ApplicationContext
    {
        private readonly ITrayCommandHelper _trayCommandHelper;
        private readonly ILog _logger;
        private readonly SettingsForm _settingsForm;
        private readonly IHostHelper _hostHelper;
        private readonly NotifyIcon _trayIcon;        
        private readonly ToolStripMenuItem _commandButton;

        public ControllerApplicationContext(ITrayCommandHelper trayCommandHelper, SettingsForm settingsForm, IHostHelper hostHelper, ILog logger)
        {
            _trayCommandHelper = trayCommandHelper;
            _settingsForm = settingsForm;
            _hostHelper = hostHelper;
            _logger = logger;

            var notifyContextMenu = new ContextMenuStrip();

            _commandButton = new ToolStripMenuItem(_trayCommandHelper.GetText((TrayCommandType)Settings.Default.DefaultCommand))
            {
                Image = Resources.Shutdown.ToBitmap()
            };
            _commandButton.Click += ShutDownClick;
            notifyContextMenu.Items.Add(_commandButton);

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
            _trayIcon = new NotifyIcon
            {
                Icon = Resources.AppIcon,
                ContextMenuStrip = notifyContextMenu,
                Visible = true
            };

            _hostHelper.SecretCode = Settings.Default.SecretCode;
            _hostHelper.DefaultCommand = (TrayCommandType)Settings.Default.DefaultCommand;
            _hostHelper.Start(Settings.Default.RemotePort);
        }

        private void SettingsClick(object sender, EventArgs e)
        {
            _logger.Info("Openning settings form...");

            if (_settingsForm.ShowDialog() == DialogResult.OK)
            {
                _logger.Info("Applying settings...");

                if (Settings.Default.RemotePort != _settingsForm.Port)
                {
                    _hostHelper.Start(_settingsForm.Port);
                }
                if (Settings.Default.AutoStart != _settingsForm.AutoStart)
                {
                    SetAutoStart(_settingsForm.AutoStart);
                }
                _commandButton.Text = _trayCommandHelper.GetText(_settingsForm.CommandType);
                Settings.Default.AutoStart = _settingsForm.AutoStart;
                Settings.Default.DefaultCommand = (int)_settingsForm.CommandType;
                Settings.Default.RemotePort = _settingsForm.Port;
                Settings.Default.SecretCode = _settingsForm.SecretCode;                
                Settings.Default.Save();
                // Update host helper
                _hostHelper.SecretCode = Settings.Default.SecretCode;
                _hostHelper.DefaultCommand = (TrayCommandType)Settings.Default.DefaultCommand;

                _logger.Info("Settings applied.");
            }

            _logger.Info("Settings form closed.");
        }

        private void SetAutoStart(bool autoStart)
        {
            RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (autoStart)
            {
                // Add the value in the registry so that the application runs at startup
                rkApp?.SetValue("Karpach.RemoteShutdown", Application.ExecutablePath);

                _logger.Info("Autostart enabled.");
            }
            else
            {
                rkApp?.DeleteValue("Karpach.RemoteShutdown", false);

                _logger.Info("Autostart disabled.");
            }
        }

        private void ShutDownClick(object sender, EventArgs e)
        {
            _logger.Info("Executing shutdown command...");

            _trayCommandHelper.RunCommand((TrayCommandType)Settings.Default.DefaultCommand);
        }

        void Exit(object sender, EventArgs e)
        {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            _trayIcon.Visible = false;            
            _hostHelper.Stop();

            _logger.Info("Exiting application...");

            Application.Exit();
        }        
    }
}