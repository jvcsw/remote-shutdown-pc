namespace Karpach.RemoteShutdown.Controller.Services
{
    using System.Threading.Tasks;

    public interface IHostService
    {
        Task Start(int port);
        
        Task Stop();

        string SecretCode { get; set; }
        
        TrayCommandType DefaultCommand { get; set; }
    }
}