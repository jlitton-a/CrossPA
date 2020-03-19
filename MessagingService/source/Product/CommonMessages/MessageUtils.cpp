
#include "MessageUtils.h"
#include "StringUtils.h"

using namespace Matrix::MsgService::CommonMessages;

MsgType MessageUtils::GetMsgTypeID(const void* data, int len)
{
   MsgType messageID = MsgType::INVALID_MSG_TYPE;

   Header reqHdr;
   if (reqHdr.ParseFromArray(data, len))
   {
      messageID = reqHdr.msgtypeid();
   }
   return messageID;
}

bool MessageUtils::ParseHeader(Header& reqHdr, const void* data, int len)
{
   if (reqHdr.ParseFromArray(data, len))
   {
      return true;
   }
   return false;
}
/// <summary>
/// Get a user-readable string for msgType 
/// </summary>
/// <param name="msgType">the MsgType to get</param>
/// <returns>a user-readable string for msgTyper</returns>
std::string MessageUtils::ToString(MsgType msgType)
{
   switch (msgType)
   {
      case MsgType::ACK:
         return "ACK";
      case MsgType::CUSTOM:
         return "CUSTOM";
      case MsgType::LOGOFF:
         return "LOGOFF";
      case MsgType::LOGON:
         return "LOGON";
      case MsgType::SUBSCRIBE:
         return "SUBSCRIBE";
      case MsgType::UNSUBSCRIBE:
         return "UNSUBSCRIBE";
      default:
         return Matrix::Common::StringUtils::Format("UNKNOWN (%d)", msgType);
   }
}

