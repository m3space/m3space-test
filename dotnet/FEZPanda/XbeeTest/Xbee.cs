using System;
using Microsoft.SPOT;
using System.IO.Ports;
using Microsoft.SPOT.Hardware;
using GHIElectronics.NETMF.FEZ;
using System.Threading;
using System.Text;
using M3Space.Capsule.Util;

namespace M3Space.Capsule.Drivers
{
    /// <summary>
    /// An XBee 868 Pro driver.
    /// version 1.01
    /// </summary>
    public class Xbee
    {
        const int RECEIVE_BUFFER_SIZE = 32;
        const int ATMODE_GUARD_TIME = 300;

        static readonly byte[] ENTER_ATMODE = new byte[] { 0x2B, 0x2B, 0x2B };                      // +++
        static readonly byte[] EXIT_ATMODE = new byte[] { 0x41, 0x54, 0x43, 0x4E, 0x0D };           // ATCN\r
        static readonly byte[] SET_TX_POWER = new byte[] { 0x41, 0x54, 0x50, 0x4C, 0x00, 0x0D };    // ATPLx\r  x=level
        static readonly byte[] GET_DUTYCYCLE = new byte[] { 0x41, 0x54, 0x44, 0x43, 0x0D };         // ATDC\r

        private SerialPort port;
        private byte[] rcvBuf;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="port">the serial port</param>
        public Xbee(SerialPort port)
        {
            this.port = port;
            rcvBuf = new byte[RECEIVE_BUFFER_SIZE];
        }

        /// <summary>
        /// Initializes the module.
        /// </summary>
        public void Initialize()
        {
            if (!port.IsOpen)
            {
                port.Open();
            }
            port.DiscardInBuffer();
        }

        /// <summary>
        /// Resets the XBee module.
        /// Uses FEZ Panda digital I/O.
        /// </summary>
        public void Reset()
        {
            TristatePort port = new TristatePort((Cpu.Pin)FEZ_Pin.Digital.Di7, false, false, Port.ResistorMode.PullUp);
            port.Active = true;  // set port as output
            port.Write(false);   // reset the xBee module 
            Thread.Sleep(1);     // wait (at least 100us)
            port.Active = false; // set port as input
        }

        /// <summary>
        /// Sets the transmission power level.
        /// </summary>
        /// <param name="level">0=1mW, 1=25mW, 2=100mW, 3=150mW, 4=300mW</param>
        public bool SetTransmitPower(byte level)
        {               
            if (EnterAtMode())
            {
                // now we are in at-command mode

                // set TX power (0=1mW, 1=23mW, 2=100mW, 3=158mW, 4=316mW)
                SET_TX_POWER[4] = level;
                port.DiscardInBuffer();
                port.Write(SET_TX_POWER, 0, 6);
                port.Flush();
                Thread.Sleep(150);

#if DEBUG
                Debug.Print("TX Power Level = " + level);
#endif
                ExitAtMode();

                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the duty cycle of the transmitter.
        /// </summary>
        /// <returns>the duty cycle in percent</returns>
        public uint GetDutyCycle()
        {
            uint dc = uint.MaxValue;
            if (EnterAtMode())
            {
                //get DutyCycle counter (0 to 0x64) 0x64 means 100% dutycycle is reached
                port.DiscardInBuffer();
                port.Write(GET_DUTYCYCLE, 0, 5);
                port.Flush();
                Thread.Sleep(150);

                int n = port.Read(rcvBuf, 0, port.BytesToRead);
                if (n > 1)
                {
                    string readString = new String(Encoding.UTF8.GetChars(rcvBuf), 0, n).TrimEnd('\r');    // subtract \r
                    dc = BitConverter.Hex2Dec(readString);
                }

#if DEBUG
                Debug.Print("DutyCycle = " + dc + "%");
#endif
                ExitAtMode();
            }
            return dc;
        }

        /// <summary>
        /// Transmits data.
        /// </summary>
        /// <param name="buf">the data buffer</param>
        /// <param name="offset">the buffer offset index</param>
        /// <param name="length">the number of bytes to send</param>
        public void Send(byte[] buf, int offset, int length)
        {
            port.Write(buf, offset, length);
            port.Flush();
        }

        /// <summary>
        /// Enters AT command mode.
        /// </summary>
        /// <returns>true if in AT mode, false otherwise</returns>
        public bool EnterAtMode()
        {
            Thread.Sleep(ATMODE_GUARD_TIME);
            port.DiscardInBuffer();
            port.Write(ENTER_ATMODE, 0, 3);
            port.Flush();
            Thread.Sleep(ATMODE_GUARD_TIME);
 
            int n = port.Read(rcvBuf, 0, port.BytesToRead);
            if (n >= 3)
            {
                string readString = new String(System.Text.Encoding.UTF8.GetChars(rcvBuf), 0, n).TrimEnd('\r');    // subtract \r
#if DEBUG
                Debug.Print("Enter ATmode = " + readString);
#endif
                return (readString.Equals("OK"));
            }

            return false;
        }

        /// <summary>
        /// Exits AT command mode.
        /// </summary>
        /// <returns></returns>
        public bool ExitAtMode()
        {
            port.DiscardInBuffer();
            port.Write(EXIT_ATMODE, 0, 5);
            port.Flush();
            Thread.Sleep(150);

            int n = port.Read(rcvBuf, 0, port.BytesToRead);
            if (n >= 3)
            {
                string readString = new String(Encoding.UTF8.GetChars(rcvBuf), 0, 3).TrimEnd('\r');    // subtract \r
#if DEBUG
                Debug.Print("Exit ATmode = " + readString + "\n");
#endif
                port.DiscardInBuffer();
                return (readString.Equals("OK"));
            }

            return false;
        }
    }
}
