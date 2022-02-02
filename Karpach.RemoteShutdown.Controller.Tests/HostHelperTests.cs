﻿using System;
using System.Threading.Tasks;
using Karpach.RemoteShutdown.Controller.Helpers;
using Karpach.RemoteShutdown.Controller.Interfaces;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;

namespace Karpach.RemoteShutdown.Controller.Tests
{
    [TestFixture]
    public class HostHelperTests
    {
        private AutoMocker _mocker;
        private HostService _hostHelper;

        [SetUp]
        public void Init()
        {
            _mocker = new AutoMocker();
            _hostHelper = _mocker.CreateInstance<HostService>();
            _hostHelper.DefaultCommand = TrayCommandType.Suspend;
        }

        [Test]  
        [TestCase("shutdown", TrayCommandType.Shutdown)]
        [TestCase("hibernate", TrayCommandType.Hibernate)]
        public async Task ProcessRequestAsync_Ignore_Secret(string commandText, TrayCommandType command)
        {
            // Arrange                        
            _mocker.GetMock<ITrayCommandHelper>().Setup(x => x.GetCommandType(commandText)).Returns(command);            

            // Act
            await _hostHelper.ProcessRequestAsync($"/someCode/{commandText}").ConfigureAwait(false);

            // Assert
            _mocker.Verify<ITrayCommandHelper>(x => x.RunCommand(command), Times.Once);
        }                

        [Test]
        [TestCase("shutdown", TrayCommandType.Shutdown)]
        [TestCase("hibernate", TrayCommandType.Hibernate)]
        public async Task ProcessRequestAsync_No_Secret(string commandText, TrayCommandType command)
        {
            // Arrange            
            _mocker.GetMock<ITrayCommandHelper>().Setup(x => x.GetCommandType(commandText)).Returns(command);

            // Act
            await _hostHelper.ProcessRequestAsync($"/{commandText}").ConfigureAwait(false);

            // Assert
            _mocker.Verify<ITrayCommandHelper>(x => x.RunCommand(command), Times.Once);
        }

        [Test]        
        public async Task ProcessRequestAsync_No_Secret_Passed()
        {
            // Arrange            
            _hostHelper.SecretCode = "secret_code";
            string commandText = "hibernate";
            _mocker.GetMock<ITrayCommandHelper>().Setup(x => x.GetCommandType(commandText)).Returns(TrayCommandType.Hibernate);

            // Act
            await _hostHelper.ProcessRequestAsync($"/{commandText}").ConfigureAwait(false);

            // Assert
            _mocker.Verify<ITrayCommandHelper>(x => x.RunCommand(It.IsAny<TrayCommandType>()), Times.Never);
        }

        [Test]
        public async Task ProcessRequestAsync_Invalid_Command()
        {
            // Arrange            
            string commandText = "some_command";            

            // Act
            await _hostHelper.ProcessRequestAsync($"/{commandText}").ConfigureAwait(false);

            // Assert
            _mocker.Verify<ITrayCommandHelper>(x => x.RunCommand(It.IsAny<TrayCommandType>()), Times.Never);
        }
    }
}
