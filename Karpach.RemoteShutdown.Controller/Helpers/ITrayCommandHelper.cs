namespace Karpach.RemoteShutdown.Controller.Helpers
{
    public interface ITrayCommandHelper
    {
        TrayCommand[] Commands { get; }

        string GetText(TrayCommandType commandType);

        void RunCommand(TrayCommandType commandType);

        TrayCommandType? GetCommandType(string commandName);
    }
}