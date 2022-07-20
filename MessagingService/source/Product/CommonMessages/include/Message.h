#pragma once

#include "../stdafx.h"

#ifdef _WIN32
#pragma warning( push )
#pragma warning( disable: 4251 4100 4146)
#endif
#include "../CommonMessages.pb.h"
#include <google/protobuf/io/coded_stream.h>
#include <google/protobuf/io/zero_copy_stream_impl.h>
#include <google/protobuf/io/zero_copy_stream_impl_lite.h>
#ifdef _WIN32
#pragma warning( pop )
#endif
#include <stdlib.h>

using namespace google::protobuf::io;

namespace Matrix
{
namespace MsgService
{
namespace CommonMessages
{
   const uint8_t HDR_SIZE = 4;
//   const int MAX_MESSAGE_SIZE = 16300;
   const int MAX_MESSAGE_SIZE = 1048576;
   /// <summary>
   /// Class for packing and unpacking messages into and out of a byte buffer 
   /// </summary>
   class Message
   {
   public:
      //TODO - use vector so we can accept any size message?
      typedef std::vector<uint8_t> DataBuffer;

      Message() { _buffer = (char*)malloc(HDR_SIZE + MAX_MESSAGE_SIZE); }
      ~Message() 
      { 
          if (_buffer != NULL)
          {
              free(_buffer);
              _buffer = NULL;
          }
      }
   private:
      Header _msg;
      char* _buffer;

   public:
      /// <summary>
      /// Gets the current Header msg
      /// TODO: 
      /// </summary>
      Header& GetMsg() { return _msg; }
      /// <summary>
      /// Class for packing and unpacking messages into and out of a byte buffer 
      /// </summary>
      char* GetBuffer() { return _buffer; }

      /// <summary>
      /// Fills the buffer retreived by GetBuffer() with a 0 size prefix
      /// </summary>
      /// <returns>Total size of the data in the buffer</returns>
      COMMONMESSAGES_API int CreateEmptyMsg();
      /// <summary>
      /// Converts msg into a size prefixed byte array and stores it in the buffer retreived by GetBuffer()
      /// </summary>
      /// <param name="msg">The message to convert to byte array</param>
      /// <returns>Total size of the data in the buffer</returns>
      COMMONMESSAGES_API int PackMsg(const Header& msg);
      /// <summary>
      /// Decodes a size prefixed byte array and parses it into Header retrieved by GetMsg()
      /// NOTE: If size is 0, GetMessage will not be modified and 0 will be returned
      /// </summary>
      /// <returns>Size that was found in the size prefix; -1 if an error occurred</returns>
      COMMONMESSAGES_API int Decode();
      /// <summary>
      /// Decodes a size prefixed byte array and parses it into pMsg.
      /// NOTE: If size is 0, pMsg will not be modified and 0 will be returned
      /// </summary>
      /// <param name="pMsg">The message into which the byte array will be parsed</param>
      /// <returns>Size that was found in the size prefix; -1 if an error occurred</returns>
      COMMONMESSAGES_API int Decode(Header* pMsg);


      COMMONMESSAGES_API static bool writeDelimitedTo(const google::protobuf::MessageLite& message,
         google::protobuf::io::ZeroCopyOutputStream* rawOutput);

      COMMONMESSAGES_API static bool readDelimitedFrom(google::protobuf::io::ZeroCopyInputStream* rawInput,
         google::protobuf::MessageLite* message,
         bool* clean_eof);
   };
}
}
}