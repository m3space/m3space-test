namespace M3Space.Capsule.Util
{
    public static class BitConverter
    {
        private static string ConversionTable = "0123456789ABCDEF";

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
        
        public static byte[] GetBytes(ushort value)
        {
            return new byte[2] 
            {  
                (byte)(value & 0xFF),  
                (byte)((value >> 8) & 0xFF) 
            };
        }

        public static byte[] GetBytes(short value)
        {
            return new byte[2] 
            {  
                (byte)(value & 0xFF),  
                (byte)((value >> 8) & 0xFF) 
            };
        }

        public static unsafe byte[] GetBytes(float value)
        {
            uint val = *((uint*)&value);
            return GetBytes(val);
        }

        public static short ToInt16(byte[] value, int index)
        {
            return (short)(
                value[index] |
                value[index + 1] << 8);
        }

        public static short ToInt16BigEndian(byte[] value, int index)
        {
            return (short)(
                value[index + 1] |
                value[index] << 8);
        }

        public static uint ToUInt32(byte[] value, int index)
        {
            return (uint)(
                value[0 + index] << 0 |
                value[1 + index] << 8 |
                value[2 + index] << 16 |
                value[3 + index] << 24);
        }

        public static unsafe float ToSingle(byte[] value, int index)
        {
            uint i = ToUInt32(value, index);
            return *(((float*)&i));
        }

        public static uint Hex2Dec(string hexNumber)
        {
            // Always in upper case
            hexNumber = hexNumber.ToUpper();
            // Will contain the return value
            uint RetVal = 0;
            // Will increase
            uint Multiplier = 1;

            for (int Index = hexNumber.Length - 1; Index >= 0; --Index)
            {
                RetVal += (uint)(Multiplier * (ConversionTable.IndexOf(hexNumber[Index])));
                Multiplier = (uint)(Multiplier * ConversionTable.Length);
            }

            return RetVal;
        }
    }
}
