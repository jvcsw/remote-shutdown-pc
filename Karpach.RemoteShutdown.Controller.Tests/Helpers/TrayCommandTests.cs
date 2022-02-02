namespace Karpach.RemoteShutdown.Controller.Tests.Helpers
{
    using Karpach.RemoteShutdown.Controller.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class TrayCommandTests
    {
        [Test, Ignore("Local test for commands")]
        public void TestTurnScreenOff()
        {
            // Arrange
            var commandHelper = new TrayCommandHelper();

            // Act
            commandHelper.RunCommand(TrayCommandType.TurnScreenOff);
        }
    }
}
 