﻿using System;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Renci.SshNet.Messages.Transport;

namespace Renci.SshNet.Tests.Classes
{
    [TestClass]
    public class BaseClientTest_Connected_KeepAliveInterval_NotNegativeOne : BaseClientTestBase
    {
        private BaseClient _client;
        private ConnectionInfo _connectionInfo;
        private TimeSpan _keepAliveInterval;
        private int _keepAliveCount;

        protected override void SetupData()
        {
            _connectionInfo = new ConnectionInfo("host", "user", new PasswordAuthenticationMethod("user", "pwd"));
            _keepAliveInterval = TimeSpan.FromMilliseconds(50d);
            _keepAliveCount = 0;
        }

        protected override void SetupMocks()
        {
            ServiceFactoryMock.Setup(p => p.CreateSocketFactory())
                               .Returns(SocketFactoryMock.Object);
            ServiceFactoryMock.Setup(p => p.CreateSession(_connectionInfo, SocketFactoryMock.Object))
                               .Returns(SessionMock.Object);
            SessionMock.Setup(p => p.Connect());
            SessionMock.Setup(p => p.IsConnected).Returns(true);
            SessionMock.Setup(p => p.TrySendMessage(It.IsAny<IgnoreMessage>()))
                        .Returns(true)
                        .Callback(() => Interlocked.Increment(ref _keepAliveCount));
        }

        protected override void Arrange()
        {
            base.Arrange();

            _client = new MyClient(_connectionInfo, false, ServiceFactoryMock.Object);
            _client.Connect();
        }

        protected override void TearDown()
        {
            if (_client != null)
            {
                SessionMock.Setup(p => p.OnDisconnecting());
                SessionMock.Setup(p => p.Dispose());
                _client.Dispose();
            }
        }

        protected override void Act()
        {
            _client.KeepAliveInterval = _keepAliveInterval;

            Thread.Sleep(200);
        }

        [TestMethod]
        public void KeepAliveIntervalShouldReturnConfiguredValue()
        {
            Assert.AreEqual(_keepAliveInterval, _client.KeepAliveInterval);
        }

        [TestMethod]
        public void CreateSocketFactoryOnServiceFactoryShouldBeInvokedOnce()
        {
            ServiceFactoryMock.Verify(p => p.CreateSocketFactory(), Times.Once);
        }

        [TestMethod]
        public void CreateSessionOnServiceFactoryShouldBeInvokedOnce()
        {
            ServiceFactoryMock.Verify(p => p.CreateSession(_connectionInfo, SocketFactoryMock.Object),
                                       Times.Once);
        }

        [TestMethod]
        public void ConnectOnSessionShouldBeInvokedOnce()
        {
            SessionMock.Verify(p => p.Connect(), Times.Once);
        }

        [TestMethod]
        public void IsConnectedOnSessionShouldBeInvokedOnce()
        {
            SessionMock.Verify(p => p.IsConnected, Times.Once);
        }

        [TestMethod]
        public void SendMessageOnSessionShouldBeInvokedThreeTimes()
        {
#pragma warning disable IDE0002 // Name can be simplified; "Ambiguous reference between Moq.Range and System.Range"
            SessionMock.Verify(p => p.TrySendMessage(It.IsAny<IgnoreMessage>()), Times.Between(2, 4, Moq.Range.Inclusive));
#pragma warning restore IDE0002
        }

        private class MyClient : BaseClient
        {
            public MyClient(ConnectionInfo connectionInfo, bool ownsConnectionInfo, IServiceFactory serviceFactory) : base(connectionInfo, ownsConnectionInfo, serviceFactory)
            {
            }
        }
    }
}
