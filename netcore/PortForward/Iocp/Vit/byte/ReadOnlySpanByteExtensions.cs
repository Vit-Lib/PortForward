using System;
 

namespace Vit.Extensions
{
    public static partial class ReadOnlySpanByteExtensions
    {

 


        #region ReadOnlySpanByte <--> Int32 

        public static Int32 ReadOnlySpanByteToInt32(this ReadOnlySpan<byte> data,int startIndex=0)
        {             
            return  BitConverter.ToInt32(data.ToArray(), startIndex);
        }


        public static ReadOnlySpan<byte> Int32ToReadOnlySpanByte(this Int32 data)
        {
            return BitConverter.GetBytes(data);
        }
        #endregion




    }
}
