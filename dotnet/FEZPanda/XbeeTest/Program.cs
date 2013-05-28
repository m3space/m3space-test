using System;
using System.Threading;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using GHIElectronics.NETMF.FEZ;
using System.IO.Ports;
using M3Space.Capsule.Drivers;

namespace XbeeTest
{
    public class Program
    {
        const int PacketSize = 32;
        const int Iterations = 12;
        const byte PowerLevel = 0;
        const uint ResetThreshold = 40;

        public static void Main()
        {
            Debug.EnableGCMessages(true);  // set true for garbage collector output

            byte[] testdata = new byte[PacketSize];
            for (int i = 0; i < PacketSize; i++)
            {
                testdata[i] = (byte)(64 + i);
            }

            // initialize serial port
            SerialPort xbeePort = new SerialPort("COM1", 38400, Parity.None, 8, StopBits.One);

            Xbee xbee = new Xbee(xbeePort);

            Thread.Sleep(2000);

            xbee.Initialize();

            if (xbee.SetTransmitPower(PowerLevel))
            {
                Debug.Print("Power level = " + PowerLevel);
            }
            else
            {
                Debug.Print("Power level not set");
                Thread.Sleep(500);
            }

            while (true)
            {
                for (int i = 0; i < Iterations; i++)
                {
                    xbee.Send(testdata, 0, PacketSize);
                    Thread.Sleep(50);
                }

                Debug.Print("Sent data");
                Thread.Sleep(1000);

                uint dc = xbee.GetDutyCycle();

                Debug.Print("Duty cycle = " + dc);

                if ((dc < uint.MaxValue) && (dc >= ResetThreshold))
                {
                    xbee.Reset();                    
                    Thread.Sleep(1000);
                    Debug.Print("XBee reset");
                }

                Thread.Sleep(10000);
            }
        }
    }
}
