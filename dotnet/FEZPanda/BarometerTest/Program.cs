using System;
using System.Threading;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using GHIElectronics.NETMF.FEZ;
using M3Space.Capsule.Drivers;
using System.IO.Ports;

namespace BarometerTest
{
    public class Program
    {
        public static void Main()
        {
            ushort alt;
            ushort p;
            short t;
            SerialPort barometerPort = new SerialPort("COM2", 9600, Parity.None, 8, StopBits.One);

            Barometer barometer = new Barometer(barometerPort);

            barometer.Initialize();

            while (true)
            {
                alt = barometer.GetAltitude();
                Thread.Sleep(300);
                p = barometer.GetPressure();
                Thread.Sleep(300);
                t = barometer.GetTemperature();
                Debug.Print("Alt " + alt + " P " + p + " T " + t);
                Thread.Sleep(5000);
            }
        }

    }
}
