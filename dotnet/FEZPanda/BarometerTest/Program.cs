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

            Barometer barometer = new Barometer("COM2");

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
