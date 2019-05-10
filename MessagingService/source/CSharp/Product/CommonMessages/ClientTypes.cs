using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.MsgService.CommonMessages
{
   /// <summary>
   /// Current client types for logging on and subscribing.
   /// </summary>
   ///<note>This enum is created here rather than in the proto file to allow future
   ///client types to connect and communicate without requiring upgrades to existing
   ///message services.  However, all client types should be included here to ensure
   ///there are no overlaps.
   ///This enum should match the c++ version in ClientTypes.h
   ///</note>
   public enum ClientTypes
   {
      Unknown = 0,
      MxDispatcher = 1,
      AssaAbloy = 2,
      Workstation = 3
   };
}
