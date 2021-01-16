 
using System;

namespace Vit.Extensions
{
    public static partial class ArraySegmentByteExtensions
    {
        public static readonly ArraySegment<byte> Null = new ArraySegment<byte>(new byte[0],0,0);
         


 

 


        #region ArraySegmentByte <--> Int32
 
        public static ArraySegment<byte> Int32ToArraySegmentByte(this Int32 data)
        {
            return BitConverter.GetBytes(data).BytesToArraySegmentByte();
        }
        #endregion

 

    }
}
