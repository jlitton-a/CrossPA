#pragma once

#include <stdlib.h>
#include "../stdafx.h"

namespace Matrix
{
namespace MsgService
{
namespace CommunicationUtils
{
   /// <summary>
   /// The socket state
   /// </summary>
   enum class SocketState
   {
      /// <summary>
      /// The socket is disconnected; we are not trying to connect.
      /// </summary>
      Disconnected,
      /// <summary>
      /// The socket is attempting to connect.
      /// </summary>
      Connecting,
      /// <summary>
      /// The socket is connected.
      /// </summary>
      Connected,
      /// <summary>
      /// The socket is attempting to disconnect.
      /// </summary>
      Disconnecting,
      /// <summary>
      /// The socket is disconnected. An attempt will be made to retry connection
      /// </summary>
      RetryConnect,
   };
   /// <summary>
   /// The reason why the socket is disconnected
   /// </summary>
   enum class DisconnectReason
   {
      /// <summary>
      /// Server is connected or starting to connect
      /// </summary>
      None,
      /// <summary>
      /// It was manually disconnected
      /// </summary>
      Manual,
      /// <summary>
      /// Initial attempt to connect failed
      /// </summary>
      CouldNotConnect,
      /// <summary>
      /// Server connection was lost
      /// </summary>
      ServerDisconnected,
      /// <summary>
      /// Server is connected but it is not responding
      /// </summary>
      ServerNotResponding,
      /// <summary>
      /// An exception occurred during read/write
      /// </summary>
      Exception,
   };

}
}
}