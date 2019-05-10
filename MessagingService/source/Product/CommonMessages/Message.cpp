
#include "Message.h"

using namespace Matrix::MsgService::CommonMessages;

int Message::CreateEmptyMsg()
{
   int totalSize = HDR_SIZE;
   google::protobuf::io::ArrayOutputStream aos(_buffer, (int)totalSize);
   // We create a new coded stream for each message.  Don't worry, this is fast.
   google::protobuf::io::CodedOutputStream output(&aos);
   // Write the size.
   output.WriteLittleEndian32((size_t)0);
   return totalSize;
}

int Message::PackMsg(const Header& msg)
{
   _msg = msg;
   size_t msgSize = msg.ByteSizeLong();
   int totalSize = msgSize + HDR_SIZE;
   google::protobuf::io::ArrayOutputStream aos(_buffer, (int)totalSize);
   // We create a new coded stream for each message.  Don't worry, this is fast.
   google::protobuf::io::CodedOutputStream output(&aos);
   // Write the size.
   output.WriteLittleEndian32(msgSize);
   if (msg.SerializeToCodedStream(&output))
      return totalSize;

   return -1;
}
int Message::Decode()
{
   google::protobuf::io::ArrayInputStream ais(_buffer, HDR_SIZE + MAX_MESSAGE_SIZE);
   google::protobuf::io::CodedInputStream coded_input(&ais);
   google::protobuf::uint32 size;
   if (coded_input.ReadLittleEndian32(&size))
   {
      if (size > 0)
      {
         // Tell the stream not to read beyond that size.
         google::protobuf::io::CodedInputStream::Limit limit = coded_input.PushLimit((size_t)size);

         // Parse the message.
         if (!_msg.MergeFromCodedStream(&coded_input)) return false;
         if (!coded_input.ConsumedEntireMessage()) return false;

         // Release the limit.
         coded_input.PopLimit(limit);
      }
      return (int)size;
   }
   return -1;
}
int Message::Decode(Header* pMsg)
{
   google::protobuf::io::ArrayInputStream ais(_buffer, HDR_SIZE + MAX_MESSAGE_SIZE);
   google::protobuf::io::CodedInputStream coded_input(&ais);
   google::protobuf::uint32 size;
   if (coded_input.ReadLittleEndian32(&size))
   {
      if (size > 0)
      {
         // Tell the stream not to read beyond that size.
         google::protobuf::io::CodedInputStream::Limit limit = coded_input.PushLimit((size_t)size);

         // Parse the message.
         if (!pMsg->MergeFromCodedStream(&coded_input)) return false;
         if (!coded_input.ConsumedEntireMessage()) return false;

         // Release the limit.
         coded_input.PopLimit(limit);
      }
      return (int)size;
   }
   return false;
}

bool Message::writeDelimitedTo(const google::protobuf::MessageLite& message,
   google::protobuf::io::ZeroCopyOutputStream* rawOutput)
{
   // We create a new coded stream for each message.  Don't worry, this is fast.
   google::protobuf::io::CodedOutputStream output(rawOutput);

   // Write the size.
   auto size = message.ByteSizeLong();
   output.WriteVarint32(size);

   uint8_t* buffer = output.GetDirectBufferForNBytesAndAdvance(size);
   if (buffer != NULL)
   {
      // Optimization:  The message fits in one buffer, so use the faster
      // direct-to-array serialization path.
      message.SerializeWithCachedSizesToArray(buffer);
   }

   else
   {
      // Slightly-slower path when the message is multiple buffers.
      message.SerializeWithCachedSizes(&output);
      if (output.HadError())
         return false;
   }

   return true;
}

bool Message::readDelimitedFrom(google::protobuf::io::ZeroCopyInputStream* rawInput, google::protobuf::MessageLite* message, bool* clean_eof)
{
   // We create a new coded stream for each message.  Don't worry, this is fast,
   // and it makes sure the 64MB total size limit is imposed per-message rather
   // than on the whole stream.  (See the CodedInputStream interface for more
   // info on this limit.)
   google::protobuf::io::CodedInputStream input(rawInput);
   const int start = input.CurrentPosition();
   if (clean_eof)
      *clean_eof = false;


   // Read the size.
   google::protobuf::uint32 size;
   if (!input.ReadVarint32(&size))
   {
      if (clean_eof)
         *clean_eof = input.CurrentPosition() == start;
      return false;
   }
   // Tell the stream not to read beyond that size.
   google::protobuf::io::CodedInputStream::Limit limit = input.PushLimit(size);

   // Parse the message.
   if (!message->MergeFromCodedStream(&input)) return false;
   if (!input.ConsumedEntireMessage()) return false;

   // Release the limit.
   input.PopLimit(limit);

   return true;
}