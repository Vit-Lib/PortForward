using System;
using Vit.Core.Util.Pool;

namespace Vit.Extensions
{
    public static partial class ArraySegmentByteExtensions
    {

        public static void ReturnToPool(this ArraySegment<byte> data)
        {
            DataPool.BytesReturn(data.Array);
        }




    }
}
