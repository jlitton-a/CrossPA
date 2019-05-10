#pragma once

#include "SignalConnection.h"
#include "CommStatusDefs.h"

namespace Matrix
{
namespace MsgService
{
   namespace CommonMessages
   {
      class Header;
   }

namespace CommunicationUtils
{
   typedef Matrix::Common::Signal<bool> ConnectionChangeSignal;

   //callback should be of the form void HandleConnectChange(bool isConnected)
   typedef ConnectionChangeSignal::Callback ConnectionChangeCallback;
   //disconnect will happen automatically when object is destroyed, retain a pointer to this when you want to disconnect manually
   typedef ConnectionChangeSignal::Connection ConnectionChangeConnection;

   typedef Matrix::Common::Signal<SocketState, SocketState, DisconnectReason> SocketStateChangeSignal;

   //callback should be of the form void HandleSocketStateChange(CommunicationUtils::SocketState prevState, 
   //      CommunicationUtils::SocketState newState, CommunicationUtils::DisconnectReason reason)
   typedef SocketStateChangeSignal::Callback SocketStateChangeCallback;
   //disconnect will happen automatically when object is destroyed, retain a pointer to this when you want to disconnect manually
   typedef SocketStateChangeSignal::Connection SocketStateChangeConnection;

   typedef Matrix::Common::Signal<Matrix::MsgService::CommonMessages::Header*> MessageRxSignal;

   //callback should be of the form HandleMessageReceived(Matrix::MsgService::CommonMessages::Header*)
   typedef MessageRxSignal::Callback MessageRxCallback;
   //disconnect will happen automatically when object is destroyed, retain a pointer to this when you want to disconnect manually
   typedef MessageRxSignal::Connection MessageRxConnection;
}
}
}
