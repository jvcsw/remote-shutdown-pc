﻿namespace Karpach.RemoteShutdown.Controller.Helpers
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    public class TrayCommandHelper : ITrayCommandHelper
    {
        private TrayCommand[] commands;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(
            IntPtr hWnd,
            UInt32 msg,
            IntPtr wParam,
            IntPtr lParam
        );

        public TrayCommand[] Commands => commands ?? (commands = new[]
        {
            new TrayCommand {CommandType = TrayCommandType.Hibernate, Name = "Hibernate"},
            new TrayCommand {CommandType = TrayCommandType.TurnScreenOff, Name = "Turn screen off"},
            new TrayCommand {CommandType = TrayCommandType.Suspend, Name = "Suspend"},
            new TrayCommand {CommandType = TrayCommandType.Shutdown, Name = "Shutdown"},
            new TrayCommand {CommandType = TrayCommandType.ForceShutdown, Name = "Force Shutdown"}
        });

        public string GetText(TrayCommandType commandType)
        {
            return Commands.SingleOrDefault(c => c.CommandType == commandType)?.Name;
        }

        public TrayCommandType? GetCommandType(string commandName)
        {
            return Commands.SingleOrDefault(c => string.Equals(c.Name, commandName, StringComparison.InvariantCultureIgnoreCase))?.CommandType;
        }

        public void RunCommand(TrayCommandType commandType)
        {            
            switch (commandType)
            {
                case TrayCommandType.Hibernate:
                    Application.SetSuspendState(PowerState.Hibernate, true, true);
                    break;
                case TrayCommandType.Shutdown:
                    Process.Start("shutdown", "/s /t 0");
                    break;
                case TrayCommandType.Suspend:
                    Application.SetSuspendState(PowerState.Suspend, true, true);
                    break;
                case TrayCommandType.TurnScreenOff:
                    SendMessage(
                        (IntPtr)0xffff, // HWND_BROADCAST
                        0x0112,         // WM_SYSCOMMAND
                        (IntPtr)0xf170, // SC_MONITORPOWER
                        (IntPtr)0x0002  // POWER_OFF
                    );
                    break;
                case TrayCommandType.ForceShutdown:
                    Process.Start("shutdown", "/s /f /t 10");
                    break;                    
            }
        }
    }
}