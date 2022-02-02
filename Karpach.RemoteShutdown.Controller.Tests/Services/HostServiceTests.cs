namespace Karpach.RemoteShutdown.Controller.Tests.Services
{
    using System.Threading.Tasks;
    using Karpach.RemoteShutdown.Controller.Helpers;
    using Karpach.RemoteShutdown.Controller.Services;
    using Moq;
    using Moq.AutoMock;
    using NUnit.Framework;

    [TestFixture]
    public class HostServiceTests
    {
        private AutoMocker mocker;
        private HostService hostService;

        [SetUp]
        public void Init()
        {
            this.mocker = new AutoMocker();
            this.hostService = mocker.CreateInstance<HostService>();
            this.hostService.DefaultCommand = TrayCommandType.Suspend;
        }

        [Test]  
        [TestCase("shutdown", TrayCommandType.Shutdown)]
        [TestCase("hibernate", TrayCommandType.Hibernate)]
        public async Task ProcessRequestAsync_Ignore_Secret(string commandText, TrayCommandType command)
        {
            // Arrange                        
            this.mocker.GetMock<ITrayCommandHelper>().Setup(x => x.GetCommandType(commandText)).Returns(command);            

            // Act
            await this.hostService.ProcessRequestAsync($"/someCode/{commandText}").ConfigureAwait(false);

            // Assert
            this.mocker.Verify<ITrayCommandHelper>(x => x.RunCommand(command), Times.Once);
        }                

        [Test]
        [TestCase("shutdown", TrayCommandType.Shutdown)]
        [TestCase("hibernate", TrayCommandType.Hibernate)]
        public async Task ProcessRequestAsync_No_Secret(string commandText, TrayCommandType command)
        {
            // Arrange            
            this.mocker.GetMock<ITrayCommandHelper>().Setup(x => x.GetCommandType(commandText)).Returns(command);

            // Act
            await this.hostService.ProcessRequestAsync($"/{commandText}").ConfigureAwait(false);

            // Assert
            this.mocker.Verify<ITrayCommandHelper>(x => x.RunCommand(command), Times.Once);
        }

        [Test]        
        public async Task ProcessRequestAsync_No_Secret_Passed()
        {
            // Arrange            
            this.hostService.SecretCode = "secret_code";
            string commandText = "hibernate";
            this.mocker.GetMock<ITrayCommandHelper>().Setup(x => x.GetCommandType(commandText)).Returns(TrayCommandType.Hibernate);

            // Act
            await this.hostService.ProcessRequestAsync($"/{commandText}").ConfigureAwait(false);

            // Assert
            this.mocker.Verify<ITrayCommandHelper>(x => x.RunCommand(It.IsAny<TrayCommandType>()), Times.Never);
        }

        [Test]
        public async Task ProcessRequestAsync_Invalid_Command()
        {
            // Arrange            
            string commandText = "some_command";            

            // Act
            await this.hostService.ProcessRequestAsync($"/{commandText}").ConfigureAwait(false);

            // Assert
            this.mocker.Verify<ITrayCommandHelper>(x => x.RunCommand(It.IsAny<TrayCommandType>()), Times.Never);
        }
    }
}
