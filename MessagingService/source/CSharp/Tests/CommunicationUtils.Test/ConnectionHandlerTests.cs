using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Matrix.MsgService.CommunicationUtils.Test
{
   [TestClass]
   public class ConnectionHandlerTest
   {
      [TestMethod]
      public void Constructor()
      {
         //Setup
         string hostName = "myhost";
         int port = 1234;

         //Test
         var underTest = new CommunicationUtils.ConnectionHandler(hostName, port);

         //Checks
         Assert.AreEqual(hostName, underTest.HostName);
         Assert.AreEqual(port, underTest.Port);
         Assert.AreEqual(false, underTest.IsConnected);
      }

      [TestMethod]
      public void ConnectNoHostnameError()
      {
         //Setup
         string hostName = "";
         int port = 1234;
         var underTest = new CommunicationUtils.ConnectionHandler(hostName, port);

         //Test
         Exception ex;
         bool success = underTest.Connect(out ex);

         //Checks
         Assert.IsFalse(success);
         Assert.IsNotNull(ex);
      }
      [TestMethod]
      public void ConnectNoConnectionError()
      {
         //Setup
         string hostName = "MyHost";
         int port = 1234;
         var underTest = new CommunicationUtils.ConnectionHandler(hostName, port);

         //Test
         Exception ex;
         bool success = underTest.Connect(out ex);

         //Checks
         Assert.IsFalse(success);
         Assert.IsNotNull(ex);
      }
      [TestMethod]
      public void DisconnectNotConnected()
      {
         //Setup
         string hostName = "MyHost";
         int port = 1234;
         var underTest = new CommunicationUtils.ConnectionHandler(hostName, port);

         //Test
         underTest.Disconnect();

         //Checks
      }
      [TestMethod]
      public void BeginReadingNotConnected()
      {
         //Setup
         string hostName = "MyHost";
         int port = 1234;
         var underTest = new CommunicationUtils.ConnectionHandler(hostName, port);

         //Test
         underTest.BeginReading();

         //Checks
      }
   }
}
