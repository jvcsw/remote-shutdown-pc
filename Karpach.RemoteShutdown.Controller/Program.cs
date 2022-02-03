[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]

namespace Karpach.RemoteShutdown.Controller
{
    using System;
    using System.Windows.Forms;
    using Autofac;
    using Karpach.RemoteShutdown.Controller.Helpers;
    using Karpach.RemoteShutdown.Controller.Services;
    using log4net;

    static class Program
    {
        public static IContainer Container { get; set; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            log4net.Config.XmlConfigurator.Configure();

            ILog logger = LogManager.GetLogger("Logger");
            logger.Info("Starting application...");

            var builder = new ContainerBuilder();
            builder.RegisterInstance<ILog>(logger).SingleInstance();
            builder.RegisterType<TrayCommandHelper>().As<ITrayCommandHelper>().SingleInstance();
            builder.RegisterType<ApplicationController>().AsSelf();
            builder.RegisterType<SettingsForm>().AsSelf();
            builder.RegisterType<HostService>().As<IHostService>();
            Container = builder.Build();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(Container.Resolve<ApplicationController>());
        }
    }
}
