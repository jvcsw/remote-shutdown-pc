using System.Threading.Tasks;

namespace Karpach.RemoteShutdown.Controller.Interfaces
{
    public interface IHostHelper
    {
        Task Start(int port);
        
        Task Stop();

        string SecretCode { get; set; }
        
        TrayCommandType DefaultCommand { get; set; }
    }
}