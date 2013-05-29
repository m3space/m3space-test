using System;
using System.Threading;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using GHIElectronics.NETMF.FEZ;
using System.IO.Ports;
using M3Space.Capsule.Drivers;

namespace GpsReceiverTest
{
    public class Program
    {
        public static void Main()
        {
            Debug.EnableGCMessages(true);  // set true for garbage collector output

            GpsReader gps = new GpsReader("COM4");
            gps.GpsDataReceived += OnGpsDataReceived;

            gps.Initialize();

            while (true)
            {
                gps.ReadNmeaData();
                Thread.Sleep(100);
            }
        }

        static void OnGpsDataReceived(GpsPoint gpsData)
        {
            Debug.Print("GPS Data: " + gpsData.Latitude + " " + gpsData.Longitude);
        }
    }
}
