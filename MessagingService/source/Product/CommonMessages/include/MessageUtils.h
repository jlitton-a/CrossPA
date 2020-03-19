#pragma once
#include "../stdafx.h"

//this is trying to disable warnings that are caused by the protobuf created file - it doesn't seem to completely work
#ifdef _WIN32
#pragma warning( push )
#pragma warning( disable: 4251 4100 4146)
#endif
#include "../CommonMessages.pb.h"
#ifdef _WIN32
#pragma warning( pop )
#endif

namespace Matrix
{
namespace MsgService
{
namespace CommonMessages
{
   /// <summary>
   /// Utility Class for handling common messages
   /// </summary>
	class MessageUtils
	{
      //****************************************
      // Static Methods
      //****************************************
   public:
      /// <summary>
      /// Helper function to determine MsgTypeID from a Header Message 
      /// </summary>
      /// <param name="data">Pointer to the binary message</param>
      /// <param name="len">Length of the message</param>
      /// <returns>MsgType if found or INVALID_MSG_TYPE if not</returns>
      static COMMONMESSAGES_API CommonMessages::MsgType GetMsgTypeID(const void* data, int len);

      /// <summary>
      /// Helper function to get a Header Message 
      /// </summary>
      /// <param name="reqHdr">data will be parsed into this</param>
      /// <param name="data">Pointer to the binary message</param>
      /// <param name="len">Length of the message</param>
      /// <returns>true if data was parsed into reqHdr</returns>
      static COMMONMESSAGES_API bool ParseHeader(CommonMessages::Header& reqHdr, const void* data, int len);

      /// <summary>
      /// Get a user-readable string for msgType 
      /// </summary>
      /// <param name="msgType">the MsgType to get</param>
      /// <returns>a user-readable string for msgTyper</returns>
      static COMMONMESSAGES_API std::string ToString(MsgType msgType);
   };
}
}
}