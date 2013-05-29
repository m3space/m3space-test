namespace M3Space.Capsule.Util
{
    /// <summary>
    /// Number conversion utilities.
    /// version 1.04
    /// </summary>
    public static class BitConverter
    {
        private static readonly string ConversionTable = "0123456789ABCDEF";

        /// <summary>
        /// Converts a 64bit integer to a byte array.
        /// Result is in little-endian format.
        /// </summary>
        /// <param name="value">a long</param>
        /// <returns>a byte array</returns>
        public static byte[] GetBytes(long value)
        {
            return new byte[8] 
            {  
                (byte)(value & 0xFF),  
                (byte)((value >> 8) & 0xFF),  
                (byte)((value >> 16) & 0xFF),  
                (byte)((value >> 24) & 0xFF),
                (byte)((value >> 32) & 0xFF),  
                (byte)((value >> 40) & 0xFF),  
                (byte)((value >> 48) & 0xFF),
                (byte)((value >> 56) & 0xFF)
            };
        }

        /// <summary>
        /// Converts a 32bit unsigned integer to a byte array.
        /// Result is in little-endian format.
        /// </summary>
        /// <param name="value">a uint</param>
        /// <returns>a byte array</returns>
        public static byte[] GetBytes(uint value)
        {
            return new byte[4] 
            {  
                (byte)(value & 0xFF),  
                (byte)((value >> 8) & 0xFF),  
                (byte)((value >> 16) & 0xFF),  
                (byte)((value >> 24) & 0xFF) 
            };
        }

        /// <summary>
        /// Converts a 16bit unsigned integer to a byte array.
        /// Result is in little-endian format.
        /// </summary>
        /// <param name="value">a ushort</param>
        /// <returns>a byte array</returns>
        public static byte[] GetBytes(ushort value)
        {
            return new byte[2] 
            {  
                (byte)(value & 0xFF),  
                (byte)((value >> 8) & 0xFF) 
            };
        }

        /// <summary>
        /// Converts a 16bit integer to a byte array.
        /// Result is in little-endian format.
        /// </summary>
        /// <param name="value">a short</param>
        /// <returns>a byte array</returns>
        public static byte[] GetBytes(short value)
        {
            return new byte[2] 
            {  
                (byte)(value & 0xFF),  
                (byte)((value >> 8) & 0xFF) 
            };
        }

        /// <summary>
        /// Converts a floating-point number to a byte array.
        /// Result is in little-endian format.
        /// </summary>
        /// <param name="value">a float</param>
        /// <returns>a byte array</returns>
        public static unsafe byte[] GetBytes(float value)
        {
            uint val = *((uint*)&value);
            return GetBytes(val);
        }

        /// <summary>
        /// Gets a 16bit signed integer from an array of bytes.
        /// Assumes little-endian format.
        /// </summary>
        /// <param name="value">the byte array</param>
        /// <param name="index">the starting index</param>
        /// <returns>a short</returns>
        public static short ToInt16(byte[] value, int index)
        {
            return (short)(
                value[index] |
                value[index + 1] << 8);
        }

        /// <summary>
        /// Gets a 16bit signed integer from an array of bytes.
        /// Assumes big-endian format.
        /// </summary>
        /// <param name="value">the byte array</param>
        /// <param name="index">the starting index</param>
        /// <returns>a short</returns>
        public static short ToInt16BigEndian(byte[] value, int index)
        {
            return (short)(
                value[index + 1] |
                value[index] << 8);
        }

        /// <summary>
        /// Gets a 32bit unsigned integer from an array of bytes.
        /// Assumes little-endian format.
        /// </summary>
        /// <param name="value">the byte array</param>
        /// <param name="index">the starting index</param>
        /// <returns>a uint</returns>
        public static uint ToUInt32(byte[] value, int index)
        {
            return (uint)(
                value[index] << 0 |
                value[index + 1] << 8 |
                value[index + 2] << 16 |
                value[index + 3] << 24);
        }

        /// <summary>
        /// Gets a 32bit floating-point number from an array of bytes.
        /// Assumes little-endian format.
        /// </summary>
        /// <param name="value">the byte array</param>
        /// <param name="index">the starting index</param>
        /// <returns>a float</returns>
        public static unsafe float ToSingle(byte[] value, int index)
        {
            uint i = ToUInt32(value, index);
            return *(((float*)&i));
        }

        /// <summary>
        /// Converts a hex string to a number.
        /// </summary>
        /// <param name="hexNumber">a string of hexadecimal numbers</param>
        /// <returns>an unsigned integer</returns>
        public static uint Hex2Dec(string hexNumber)
        {
            // Always in upper case
            hexNumber = hexNumber.ToUpper();
            uint retVal = 0;
            uint multiplier = 1;

            for (int i = hexNumber.Length - 1; i >= 0; i--)
            {
                int n = ConversionTable.IndexOf(hexNumber[i]);
                if (n >= 0)
                {
                    retVal += (uint)(multiplier * n);
                    multiplier *= 16;
                }
                else
                {
                    return uint.MaxValue;
                }
            }

            return retVal;
        }

        /// <summary>
        /// Converts a hex string to a number.
        /// </summary>
        /// <param name="hex">an array of ASCII characters with hexadecimal numbers</param>
        /// <returns>an unsigned integer</returns>
        public static uint Hex2Dec(byte[] hex, int start, int length)
        {
            uint retVal = 0;
            uint multiplier = 1;

            for (int i = start + length - 1; i >= start; i--)
            {
                int n = 0;
                if ((hex[i] >= 48) && (hex[i] <= 57))
                {
                    n = hex[i] - 48;
                }
                else if ((hex[i] >= 65) && (hex[i] <= 70))
                {
                    n = hex[i] - 55;
                }
                else if ((hex[i] >= 97) && (hex[i] <= 102))
                {
                    n = hex[i] - 87;
                }
                else
                {
                    return uint.MaxValue;
                }

                retVal += (uint)(multiplier * n);
                multiplier *= 16;
            }

            return retVal;
        }
    }
}
