using System;
using System.Threading;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using GHIElectronics.NETMF.FEZ;
using M3Space.Capsule.Drivers;

namespace Mpu6050Test
{
    public class Program
    {
        public static void Main()
        {
            MotionData data = new MotionData();
            Mpu6050 mpu6050 = new Mpu6050();
            mpu6050.Initialize();

            while (true)
            {
                mpu6050.GetMotionData(ref data);
                Debug.Print(data.Ax + ' ' + data.Ay + ' ' + data.Az + "  " + data.Gx + ' ' + data.Gy + ' ' + data.Gz);
                Thread.Sleep(100);
            }
        }

    }
}
