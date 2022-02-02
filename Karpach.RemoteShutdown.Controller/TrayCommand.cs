namespace Karpach.RemoteShutdown.Controller
{
    using System.Windows.Forms;

    public class TrayCommand
    {
        public TrayCommandType CommandType { get; set; }

        public string Name { get; set; }
    }
}