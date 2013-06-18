using System;
using System.IO.Ports;
using System.Threading;
using Microsoft.SPOT;

namespace M3Space.Capsule.Drivers
{
    /// <summary>
    /// New LinkSprite serial camera implementation.
    /// version 2.09
    /// </summary>
    public class LinkspriteCamera
    {
        private const int CHUNK_SIZE = 128;
        private const int READ_RETRY = 2;

        public const byte Size_640x480 = 0x00;
        public const byte Size_320x240 = 0x11;
        public const byte Size_160x120 = 0x22;

        private static readonly byte[] RESET_COMMAND = new byte[] { 0x56, 0x00, 0x26, 0x00 };
        private static readonly byte[] RESET_OK_RESPONSE = new byte[] { 0x76, 0x00, 0x26, 0x00 };

        private static readonly byte[] SET_BAUDRATE_COMMAND = new byte[] { 0x56, 0x00, 0x24, 0x03, 0x01, 0x00, 0x00 };
        private static readonly byte[] SET_BAUDRATE_OK_RESPONSE = new byte[] { 0x76, 0x00, 0x24, 0x00, 0x00 };

        private static readonly byte[] SET_SIZE_COMMAND = new byte[] { 0x56, 0x00, 0x54, 0x01, 0x00 };
        private static readonly byte[] SET_SIZE_OK_RESPONSE = new byte[] { 0x76, 0x00, 0x54, 0x00, 0x00 };

        private static readonly byte[] SNAP_COMMAND = new byte[] { 0x56, 0x00, 0x36, 0x01, 0x00 };
        private static readonly byte[] SNAP_OK_RESPONSE = new byte[] { 0x76, 0x00, 0x36, 0x00, 0x00 };

        private static readonly byte[] STOP_COMMAND = new byte[] { 0x56, 0x00, 0x36, 0x01, 0x03 };
        private static readonly byte[] STOP_OK_RESPONSE = new byte[] { 0x76, 0x00, 0x36, 0x00, 0x00 };
        
        static readonly byte[] IMAGE_SIZE_COMMAND = new byte[] { 0x56, 0x00, 0x34, 0x01, 0x00 };
        static readonly byte[] IMAGE_SIZE_OK_RESPONSE = new byte[] { 0x76, 0x00, 0x34, 0x00, 0x04, 0x00, 0x00 };

        private static readonly byte[] GET_CHUNK_COMMAND = new byte[] { 0x56, 0x00, 0x32, 0x0C, 0x00, 0x0A, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x0A };
        private static readonly byte[] GET_CHUNK_OK_RESPONSE = new byte[] { 0x76, 0x00, 0x32, 0x00, 0x00 };



        private byte[] rcvBuf;
        private byte[] chunkBuf;
        private SerialPort port;

        /// <summary>
        /// A delegate for receiving image chunks.
        /// </summary>
        /// <param name="chunk">the chunk data</param>
        /// <param name="chunkSize">the chunk Size</param>
        /// <param name="complete">image is complete</param>
        public delegate void ImageChunkHandler(byte[] chunk, int chunkSize, bool complete);

        /// <summary>
        /// An event for image chunks.
        /// </summary>
        public event ImageChunkHandler ImageChunkReceived;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="portName">the serial port name</param>
        /// <param name="baudrate">the serial port baudrate to start with</param>
        public LinkspriteCamera(string portName, int baudrate)
        {
            port = new SerialPort(portName, baudrate, Parity.None, 8, StopBits.One);
            port.ReadTimeout = 200;

            rcvBuf = new byte[CHUNK_SIZE];
            chunkBuf = new byte[CHUNK_SIZE];
        }

        /// <summary>
        /// Initializes the camera.
        /// </summary>
        /// <returns>true if initialization successful, false otherwise</returns>
        public bool Initialize()
        {
            if (!port.IsOpen)
            {
                port.Open();
            }
            FlushInput();
            return true;
        }

        /// <summary>
        /// Resets the camera.
        /// After a reset, no command must be sent for 3 seconds.
        /// </summary>
        /// <returns>true if command successful, false otherwise</returns>
        public bool Reset()
        {
            SendCommand(RESET_COMMAND);
            Thread.Sleep(100);
            bool ok = ReceiveResponse(RESET_OK_RESPONSE);
            FlushInput();
            return ok;
        }

        /// <summary>
        /// Stops taking pictures.
        /// </summary>
        /// <returns>true if command successful, false otherwise</returns>
        public bool Stop()
        {
            SendCommand(STOP_COMMAND);
            Thread.Sleep(100);
            bool ok = ReceiveResponse(STOP_OK_RESPONSE);
            FlushInput();
            return ok;
        }

        /// <summary>
        /// Sets the baudrate.
        /// </summary>
        /// <param name="baudrate">the baudrate</param>
        /// <returns>true if command successful, false otherwise</returns>
        public bool SetBaudRate(int baudrate)
        {
            switch (baudrate)
            {
                case 9600:
                    SET_BAUDRATE_COMMAND[5] = 0xAE;
                    SET_BAUDRATE_COMMAND[6] = 0xC8;
                    break;

                case 19200:
                    SET_BAUDRATE_COMMAND[5] = 0x56;
                    SET_BAUDRATE_COMMAND[6] = 0xE4;
                    break;

                case 38400:
                    SET_BAUDRATE_COMMAND[5] = 0x2A;
                    SET_BAUDRATE_COMMAND[6] = 0xF2;
                    break;

                case 57600:
                    SET_BAUDRATE_COMMAND[5] = 0x1C;
                    SET_BAUDRATE_COMMAND[6] = 0x4C;
                    break;

                case 115200:
                    SET_BAUDRATE_COMMAND[5] = 0x0D;
                    SET_BAUDRATE_COMMAND[6] = 0xA6;
                    break;

                default:
                    return false;
            }

            SendCommand(SET_BAUDRATE_COMMAND);
            Thread.Sleep(100);
            bool ok = ReceiveResponse(SET_BAUDRATE_OK_RESPONSE);
            if (ok)
            {
                Thread.Sleep(5000);                
                this.port.Close();
                this.port.BaudRate = baudrate;
                this.port.Open();
                FlushInput();
            }
            return ok;
        }

        /// <summary>
        /// Sets the image size to be captured.
        /// Camera needs to be reset after setting the image size.
        /// </summary>
        /// <param name="size">a valid image size</param>
        /// <returns>true if successful, false otherwise</returns>
        public bool SetImageSize(byte size)
        {
            switch (size)
            {
                case Size_160x120:
                case Size_320x240:
                case Size_640x480:
                    SET_SIZE_COMMAND[4] = size;
                    SendCommand(SET_SIZE_COMMAND);
                    Thread.Sleep(100);
                    bool ok = ReceiveResponse(SET_SIZE_OK_RESPONSE);
                    FlushInput();
                    return ok;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Captures a new image.
        /// </summary>
        /// <returns>true if successful, false otherwise</returns>
        public bool CaptureImage()
        {
            // send command to capture an image.
            bool finished = false;
            SendCommand(SNAP_COMMAND);
            Thread.Sleep(100);
            if (ReceiveResponse(SNAP_OK_RESPONSE))
            {
                // get image size.
                SendCommand(IMAGE_SIZE_COMMAND);
                Thread.Sleep(100);
                if (ReceiveResponse(IMAGE_SIZE_OK_RESPONSE))
                {
                    int n = port.Read(rcvBuf, 0, 2);
                    if (n == 2)
                    {
                        int fileSize = (rcvBuf[0] << 8) | rcvBuf[1];
                        int startAddress = 0;
                        int bytesRead = 0;

                        // set chunk size
                        GET_CHUNK_COMMAND[12] = (byte)((CHUNK_SIZE >> 8) & 0xFF);
                        GET_CHUNK_COMMAND[13] = (byte)(CHUNK_SIZE & 0xFF);
                        
                        int retry = 0;
                        while (!finished)
                        {
                            // set data address
                            GET_CHUNK_COMMAND[8] = (byte)((startAddress >> 8) & 0xFF);
                            GET_CHUNK_COMMAND[9] = (byte)(startAddress & 0xFF);

                            // get chunk
                            SendCommand(GET_CHUNK_COMMAND);
                            Thread.Sleep(100);
                            if (ReceiveResponse(GET_CHUNK_OK_RESPONSE))
                            {
                                // get chunk data
                                if (ReadData(CHUNK_SIZE))
                                {
                                    Array.Copy(rcvBuf, chunkBuf, CHUNK_SIZE);

                                    // check trailer
                                    if (ReceiveResponse(GET_CHUNK_OK_RESPONSE))
                                    {
                                        bytesRead += CHUNK_SIZE;
                                        int chunkSize = CHUNK_SIZE;
                                        if (bytesRead >= fileSize)
                                        {
                                            chunkSize = FindEnd(chunkBuf);
                                            finished = true;
#if (DEBUG && VERBOSE)
                                            Debug.Print("JPEG image complete.");
#endif
                                        }
                                        if (chunkSize > 0)
                                        {
                                            OnImageChunkReceived(chunkBuf, chunkSize, finished);
                                            startAddress += CHUNK_SIZE;
                                            retry = 0;
                                        }
                                        else
                                        {
#if (DEBUG && VERBOSE)
                                            Debug.Print("JPEG end not found.");
#endif
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        FlushInput();
                                        retry++;
                                        if (retry > READ_RETRY)
                                            return false;
                                        Thread.Sleep(100);                                        
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
            FlushInput();
            return finished;
        }

        /// <summary>
        /// Sends a command.
        /// </summary>
        /// <param name="data">the command data</param>
        private void SendCommand(byte[] data)
        {
            port.Write(data, 0, data.Length);
            port.Flush();
        }

        /// <summary>
        /// Receives a command response.
        /// </summary>
        /// <param name="expectedResponse">the expected response</param>
        /// <returns>true if response ok, false otherwise</returns>
        private bool ReceiveResponse(byte[] expectedResponse)
        {
            if (ReadData(expectedResponse.Length))
            {
                return (ArrayStartsWith(rcvBuf, expectedResponse.Length, expectedResponse));
            }
            return false;
        }

        /// <summary>
        /// Reads data bytes into receive buffer.
        /// </summary>
        /// <param name="length">the number of bytes to read</param>
        /// <returns>true if bytes read, false if not enough data</returns>
        private bool ReadData(int length)
        {
            int bytesRead = 0;
            for (int i = 0; i <= READ_RETRY; i++)
            {
                int n = port.Read(rcvBuf, bytesRead, length - bytesRead);
                if (n > 0)
                {
                    bytesRead += n;
                }
                if (bytesRead == length)
                {
                    return true;
                }
                else if (i < READ_RETRY)
                {
                    Thread.Sleep(100);
                }
            }
            return false;
        }

        /// <summary>
        /// Discards remaining serial input.
        /// </summary>
        public void FlushInput()
        {
            if (port.BytesToRead > 0)
            {
                int n = 0;
                do
                {
                    n = port.Read(rcvBuf, 0, CHUNK_SIZE);
                }
                while (n == CHUNK_SIZE);
            }
        }

        /// <summary>
        /// Determines if an arrays starts with the same data as another array.
        /// </summary>
        /// <param name="array">the array to examine</param>
        /// <param name="arrayLength">the examined array length</param>
        /// <param name="contains">the expected content</param>
        /// <returns>true if array contains the data, false otherwise</returns>
        private bool ArrayStartsWith(byte[] array, int arrayLength, byte[] contains)
        {
            if (contains.Length <= arrayLength)
            {
                for (int i = 0; i < contains.Length; i++)
                {
                    if (contains[i] != rcvBuf[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Finds position of JPEG end marker.
        /// </summary>
        /// <param name="chunk">the chunk data</param>
        /// <returns>the position after the end marker, or -1 if not found</returns>
        private int FindEnd(byte[] chunk)
        {
            for (int i = 0; i < chunk.Length - 1; i++)
            {
                if ((chunk[i] == 0xFF) && (chunk[i + 1] == 0xD9))
                    return i + 1;
            }
            return -1;
        }

        /// <summary>
        /// Handle received image chunks.
        /// </summary>
        /// <param name="chunk">the image chunk data</param>
        /// <param name="chunkSize">the chunk size</param>
        /// <param name="complete">true if image is complete</param>
        private void OnImageChunkReceived(byte[] chunk, int chunkSize, bool complete)
        {
            if (ImageChunkReceived != null)
                ImageChunkReceived(chunk, chunkSize, complete);
        }
    }   
}
