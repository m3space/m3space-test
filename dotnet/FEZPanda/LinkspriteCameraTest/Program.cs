using System;
using System.Threading;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using GHIElectronics.NETMF.FEZ;
using System.IO.Ports;
using GHIElectronics.NETMF.IO;
using Microsoft.SPOT.IO;
using System.IO;
using M3Space.Capsule.Drivers;

namespace LinkspriteCameraTest
{
    public class Program
    {
        static string imageFilename = "";
        static FileStream imageFileHandle = null;

        public static void Main()
        {
            Debug.EnableGCMessages(true);  // set true for garbage collector output

            // initialize SD card
            while (!PersistentStorage.DetectSDCard())
            {                
                Debug.Print("Waiting for SD card");
                Thread.Sleep(1000);
            }
            PersistentStorage sdStorage = new PersistentStorage("SD");
            sdStorage.MountFileSystem();
            string sdRootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;

            LinkspriteCamera camera = new LinkspriteCamera("COM3", 38400);
            camera.ImageChunkReceived += OnImageChunkReceived;

            camera.Initialize();
            Thread.Sleep(5000);     // wait for camera to initialize
            camera.FlushInput();

            //if (camera.SetBaudRate(115200))
            //{
            //    Debug.Print("Baudrate changed.");
            //}

            int imgNum = 1;

            while (true)
            {
                imageFilename = sdRootDirectory + @"\test" + imgNum + ".jpg";

                if (camera.CaptureImage())
                {
                    Debug.Print("Capture successful");
                    Thread.Sleep(15000);
                }
                else
                {
                    Debug.Print("Capture failed");
                    if (imageFileHandle != null)
                    {
                        imageFileHandle.Close();
                        imageFileHandle = null;
                    }
                    camera.Reset();
                    Debug.Print("Camera reset");
                    Thread.Sleep(5000);
                }

                imgNum++;
            }

        }

        static void OnImageChunkReceived(byte[] chunk, int chunkSize, bool complete)
        {
            if (imageFileHandle == null)
            {
                imageFileHandle = new FileStream(imageFilename, FileMode.OpenOrCreate);
                Debug.Print("Created new file " + imageFilename);
            }
            imageFileHandle.Position = imageFileHandle.Length;
            imageFileHandle.Write(chunk, 0, chunkSize);
            imageFileHandle.Flush();
            Debug.Print(chunkSize + " bytes written");
            if (complete && (imageFileHandle != null))
            {
                imageFileHandle.Close();
                imageFileHandle = null;
                Debug.Print("File closed");
            }
        }
    }
}
