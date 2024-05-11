using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharedLib
{
    public static class MiscellaneousExtensions
    {
        private static readonly int[] _byteOrder =
        new[] { 15, 14, 13, 12, 11, 10, 9, 8, 6, 7, 4, 5, 0, 1, 2, 3 };
        /// <summary>
        /// Increment GUID by 1 byte
        /// </summary>
        /// <param name="guid">GUID</param>
        /// <returns>Incremented GUID</returns>
        /// <remarks>In case of overflow - returns min value of guid</remarks>
        public static Guid Increment(this Guid guid)
        {
            var bytes = guid.ToByteArray();
            var canIncrement = _byteOrder.Any(i => ++bytes[i] != 0);
            return new Guid(canIncrement ? bytes : new byte[16]);
        }
        /// <summary>
        /// Decrement GUID by 1 byte
        /// </summary>
        /// <param name="guid">GUID</param>
        /// <returns>Decremented GUID</returns>
        /// <remarks>In case of underflow - returns max value of guid</remarks>
        public static Guid Decrement(this Guid guid)
        {
            var bytes = guid.ToByteArray();
            var canDecrement = _byteOrder.Any(i => --bytes[i] != byte.MaxValue); 
            return new Guid(canDecrement ? bytes : Enumerable.Repeat(byte.MaxValue, 16).ToArray());
        }
    }
}
