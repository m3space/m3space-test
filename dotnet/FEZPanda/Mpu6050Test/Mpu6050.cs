using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using M3Space.Capsule.Util;
using System.Threading;

namespace M3Space.Capsule.Drivers
{
    /// <summary>
    /// MPU6050 Gyro/Accelerometer implementation.
    /// version 1.03
    /// </summary>
    public class Mpu6050 : I2CSlave
    {
        const byte MPU6050_ADDRESS_AD0_LOW = 0x68;      // device address
        const byte MPU6050_ADDRESS_AD0_HIGH = 0x69;

        const byte MPU6050_RA_GYRO_CONFIG = 0x1B;
        const byte MPU6050_RA_ACCEL_CONFIG = 0x1C;
        const byte MPU6050_RA_ACCEL_XOUT_H = 0x3B;      // first register of motion data
        const byte MPU6050_RA_PWR_MGMT_1 = 0x6B;

        //const byte MPU6050_PWR1_CLKSEL_MASK = 0x07;
        const byte MPU6050_CLOCK_PLL_XGYRO = 0x01;
        const byte MPU6050_TEMPSENSOR_DISABLE = 0x08;
        const byte MPU6050_RESET = 0x80;

        //const byte MPU6050_PWR1_SLEEP_MASK = 0x40;
        const byte MPU6050_SLEEP_DISABLE = 0x00;
        const byte MPU6050_SLEEP_ENABLE = 0x40;

        //const byte MPU6050_GCONFIG_FS_SEL_MASK = 0x18;
        const byte MPU6050_GYRO_FS_250 = 0x00;
        const byte MPU6050_GYRO_FS_500 = 0x08;
        const byte MPU6050_GYRO_FS_1000 = 0x10;
        const byte MPU6050_GYRO_FS_2000 = 0x18;

        //const byte MPU6050_ACONFIG_AFS_SEL_MASK = 0x18;
        const byte MPU6050_ACCEL_FS_2 = 0x00;
        const byte MPU6050_ACCEL_FS_4 = 0x08;
        const byte MPU6050_ACCEL_FS_8 = 0x10;
        const byte MPU6050_ACCEL_FS_16 = 0x18;


        private byte[] motionBuffer;

        /// <summary>
        /// Constructor.
        /// </summary>
        public Mpu6050() : base(MPU6050_ADDRESS_AD0_LOW)
        {
            motionBuffer = new byte[14];
        }
        
        /// <summary>
        /// Sets up the device.
        /// </summary>
        /// <returns>true if successful, false otherwise</returns>
        public bool Initialize()
        {
            // sleep and reset
            if (!WriteBytes(new byte[] { MPU6050_RA_PWR_MGMT_1, MPU6050_RESET }))
                return false;
            Thread.Sleep(25);

            // disable sleep, select clock and disable temp. sensor
            if (!WriteBytes(new byte[] { MPU6050_RA_PWR_MGMT_1, MPU6050_CLOCK_PLL_XGYRO | MPU6050_TEMPSENSOR_DISABLE }))
                return false;

            // configure gyro sensitivity
            if (!WriteBytes(new byte[] { MPU6050_RA_GYRO_CONFIG, MPU6050_GYRO_FS_2000 }))
                return false;

            // configure accelerometer sensitivity
            if (!WriteBytes(new byte[] { MPU6050_RA_ACCEL_CONFIG, MPU6050_ACCEL_FS_16 }))
                return false;

            return true;
        }

        /// <summary>
        /// Reads 6-axis motion data.
        /// </summary>
        /// <param name="data">the motion data to write to</param>
        /// <returns>true if successful, false if failed</returns>
        public bool GetMotionData(ref MotionData data)
        {
            if (ReadFromRegister(MPU6050_RA_ACCEL_XOUT_H, motionBuffer))
            {
                data.UtcTimestamp = DateTime.Now;
                data.Ax = BitConverter.ToInt16BigEndian(motionBuffer, 0);
                data.Ay = BitConverter.ToInt16BigEndian(motionBuffer, 2);
                data.Az = BitConverter.ToInt16BigEndian(motionBuffer, 4);
                data.Gx = BitConverter.ToInt16BigEndian(motionBuffer, 8);
                data.Gy = BitConverter.ToInt16BigEndian(motionBuffer, 10);
                data.Gz = BitConverter.ToInt16BigEndian(motionBuffer, 12);
                return true;
            }
            return false;
        }
    }
}
