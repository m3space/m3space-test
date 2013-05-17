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
        const int CalibIterations = 50;
        const int CalibInterval = 100;

        public static void Main()
        {
            MotionData data = new MotionData();
            Mpu6050 mpu6050 = new Mpu6050();
            if (!mpu6050.Initialize())
            {
                Debug.Print("initialization failed");
                return;
            }

            int cAx = 0;
            int cAy = 0;
            int cAz = 0;
            int cGx = 0;
            int cGy = 0;
            int cGz = 0;

            for (int i = 0; i < CalibIterations; i++)
            {
                if (mpu6050.GetMotionData(ref data))
                {
                    cAx += data.Ax;
                    cAy += data.Ay;
                    cAz += data.Az;
                    cGx += data.Gx;
                    cGy += data.Gy;
                    cGz += data.Gz;
                    Thread.Sleep(CalibInterval);
                }
                else
                {
                    Debug.Print("calibration failed");
                    return;
                }
            }

            cAx = -cAx / CalibIterations;
            cAy = -cAy / CalibIterations;
            cAz = -cAz / CalibIterations + 2048;    // compensate for 1g in normal orientation
            cGx = -cGx / CalibIterations;
            cGy = -cGy / CalibIterations;
            cGz = -cGz / CalibIterations;
            Debug.Print("Calibration: A " + cAx + ' ' + cAy + ' ' + cAz + " G " + cGx + ' ' + cGy + ' ' + cGz);

            Thread.Sleep(30000);

            while (true)
            {
                if (mpu6050.GetMotionData(ref data))
                {
                    Debug.Print("A " + (cAx + data.Ax) + ' ' + (cAy + data.Ay) + ' ' + (cAz + data.Az) + " G " + (cGx + data.Gx) + ' ' + (cGy + data.Gy) + ' ' + (cGz + data.Gz));
                    Thread.Sleep(250);
                }
                else
                {
                    Debug.Print("motion data failed");
                    return;
                }
            }
        }

    }
}
