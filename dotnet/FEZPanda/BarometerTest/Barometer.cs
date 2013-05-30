using System;
using Microsoft.SPOT;
using System.IO.Ports;
using System.Threading;

namespace M3Space.Capsule.Drivers
{
    /// <summary>
    /// Sure Electronics Barometer Module with MS5561 Sensor.
    /// version 1.04
    /// </summary>
    public class Barometer
    {
        private SerialPort port;

        private static readonly byte[] COMMAND_START = new byte[] { 0x24, 0x73, 0x75, 0x72, 0x65, 0x20 };   // $sure<SP>
        private static readonly byte[] COMMAND_END = new byte[] { 0x0D, 0x0A };                             // <CR><LF>
        private static readonly byte[] GETPRESSURE = new byte[] { 0x70 };                                   // p
        private static readonly byte[] GETTEMPERATURE = new byte[] { 0x74, 0x2D, 0x63 };                    // t-c
        private static readonly byte[] GETALTITUDE = new byte[] { 0x68 };                                   // h
        private static readonly byte PRESSURE_SIZE = 26;    // number of bytes of the pressure packet (including \r\n)
        private static readonly byte TEMPERATURE_SIZE = 31; // number of bytes of the temperature packet (including \r\n)
        private static readonly byte ALTITUDE_SIZE = 21;    // number of bytes of the altitude packet (including \r\n)

        private byte[] readBuffer;
        private const int ReadBufferSize = 32;
        private const int ReadRetry = 3;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="portName">the serial port name</param>
        public Barometer(string portName)
        {
            port = new SerialPort(portName);
            port.BaudRate = 9600;
            port.DataBits = 8;
            port.StopBits = StopBits.One;
            port.Parity = Parity.None;
            port.ReadTimeout = 250;
            readBuffer = new byte[ReadBufferSize];
        }

        /// <summary>
        /// Initializes the port.
        /// </summary>
        public void Initialize()
        {
            if (!port.IsOpen)
            {
                port.Open();
            }
        }

        /// <summary>
        /// Gets the air pressure in bar.
        /// </summary>
        /// <returns>the air pressure</returns>
        public ushort GetPressure()
        {
            FlushInput();
            port.Write(COMMAND_START, 0, 6);
            port.Write(GETPRESSURE, 0, 1);
            port.Write(COMMAND_END, 0, 2);
            port.Flush();
            if (ReadData(PRESSURE_SIZE))
            {
                // returns integer part only. float parsing not available.
                string str = new String(System.Text.Encoding.UTF8.GetChars(readBuffer), 13, 4);
                try
                {
                    return ushort.Parse(str);
                }
                catch (Exception)
                {
#if DEBUG
                    Debug.Print("Parse Pressure value failed.");
#endif
                    return ushort.MaxValue;
                }
            }

            return ushort.MaxValue;
        }

        /// <summary>
        /// Gets the temperature in celsius.
        /// </summary>
        /// <returns>the temperature</returns>
        public short GetTemperature()
        {
            FlushInput();
            port.Write(COMMAND_START, 0, 6);
            port.Write(GETTEMPERATURE, 0, 3);
            port.Write(COMMAND_END, 0, 2);
            port.Flush();
            if (ReadData(TEMPERATURE_SIZE))
            {
                // returns integer part only. float parsing not available.
                string str = new String(System.Text.Encoding.UTF8.GetChars(readBuffer), 15, 4);
                try
                {
                    return short.Parse(str);
                }
                catch (Exception)
                {
#if DEBUG
                    Debug.Print("Parse Temperature value failed.");
#endif
                    return short.MinValue;
                } 
            }

            return short.MinValue;
        }

        /// <summary>
        /// Gets the current altitude in meters above sea level.
        /// </summary>
        /// <returns>the altitude</returns>
        public ushort GetAltitude()
        {
            FlushInput();
            port.Write(COMMAND_START, 0, 6);
            port.Write(GETALTITUDE, 0, 1);
            port.Write(COMMAND_END, 0, 2);
            port.Flush();
            if (ReadData(ALTITUDE_SIZE))
            {
                string str = new String(System.Text.Encoding.UTF8.GetChars(readBuffer), 7, 5);
                try
                {
                    return ushort.Parse(str);
                }
                catch (Exception)
                {
#if DEBUG
                    Debug.Print("Parse Altitude value failed.");
#endif
                    return ushort.MaxValue;
                }
            }

            return ushort.MaxValue;
        }

        /// <summary>
        /// Discards remaining serial input.
        /// </summary>
        private void FlushInput()
        {
            if (port.BytesToRead > 0)
            {
                int n = 0;
                do
                {
                    n = port.Read(readBuffer, 0, ReadBufferSize);
                }
                while (n == ReadBufferSize);
            }
        }

        /// <summary>
        /// Reads data bytes into receive buffer.
        /// </summary>
        /// <param name="length">the number of bytes to read</param>
        /// <returns>true if bytes read, false if not enough data</returns>
        private bool ReadData(int length)
        {
            int bytesRead = 0;
            for (int i = 0; i <= ReadRetry; i++)
            {
                int n = port.Read(readBuffer, bytesRead, length - bytesRead);
                if (n > 0)
                {
                    bytesRead += n;
                }
                if (bytesRead == length)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
