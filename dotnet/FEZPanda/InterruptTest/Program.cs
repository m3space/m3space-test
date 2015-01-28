using System;
using System.Threading;
using GHIElectronics.NETMF.FEZ;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT;

namespace InterruptTest
{
    /// <summary>
    /// Tests interrupt triggering.
    /// Uses input pin A0 for interrupt.
    /// Uses output pin Di8 to generate interrupts.
    /// Interrupt notification is printed on debug console and using onboard LED.
    /// </summary>
    public class Program
    {
        static OutputPort onboardLed = new OutputPort((Cpu.Pin)FEZ_Pin.Digital.LED, false);
        static OutputPort signalOut = new OutputPort((Cpu.Pin)FEZ_Pin.Digital.Di8, false);
        static InterruptPort interruptIn = new InterruptPort((Cpu.Pin)FEZ_Pin.Interrupt.An0, true, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeHigh);

        static int counter;

        public static void Main()
        {
            counter = 0;
            onboardLed.Write(false);
            signalOut.Write(false);
            interruptIn.OnInterrupt += new NativeEventHandler(OnInterruptDetected);
            interruptIn.EnableInterrupt();

            while (true)
            {
                Thread.Sleep(2000);
#if DEBUG
                Debug.Print("trig");
#endif
                signalOut.Write(true);
                Thread.Sleep(1);
                signalOut.Write(false);
            }
        }

        static void OnInterruptDetected(uint port, uint state, DateTime time)
        {
            counter++;
#if DEBUG
            Debug.Print(counter.ToString());
#endif
            onboardLed.Write(true);
            Thread.Sleep(100);
            onboardLed.Write(false);
        }
    }
}
