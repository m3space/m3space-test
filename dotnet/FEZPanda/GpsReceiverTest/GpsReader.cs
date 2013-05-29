using System;
using System.IO.Ports;
using Microsoft.SPOT;

namespace M3Space.Capsule.Drivers
{
    /// <summary>
    /// NMEA GPS Reader.
    /// version 2.02
    /// </summary>
    public class GpsReader
    {
        private const int RECEIVE_BUFFER_SIZE = 100;
        private const int NMEA_LINE_SIZE = 80;

        private byte[] rcvBuf;
        private char[] lineBuf;
        private int iLine;
        private SerialPort port;
        private GpsPoint cachedGpsData;

        public delegate void GpsDataHandler(GpsPoint gpsPoint);

        /// <summary>
        /// GPS data received event.
        /// </summary>
        public event GpsDataHandler GpsDataReceived;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="portName">the serial port name</param>
        public GpsReader(string portName)
        {
            port = new SerialPort(portName);
            port.BaudRate = 38400;
            port.DataBits = 8;
            port.Parity = Parity.None;
            port.StopBits = StopBits.One;
            port.ReadTimeout = 200;
            rcvBuf = new byte[RECEIVE_BUFFER_SIZE];
            lineBuf = new char[NMEA_LINE_SIZE];
            iLine = 0;
            cachedGpsData = new GpsPoint();
        }

        /// <summary>
        /// Initializes the GPS reader.
        /// </summary>
        public void Initialize()
        {
            if (!port.IsOpen)
            {
                port.Open();
            }
        }

        /// <summary>
        /// Reads NMEA data from the serial port.
        /// </summary>
        public void ReadNmeaData()
        {
            int n = port.Read(rcvBuf, 0, RECEIVE_BUFFER_SIZE);
            for (int i = 0; i < n; i++)
            {
                switch (rcvBuf[i])
                {
                    case 0x0D:
                        // \r (ignore)
                        break;

                    case 0x0A:
                        // \n (marks end of line)
                        string line = new String(lineBuf, 0, iLine);
#if DEBUG
                        Debug.Print(line);
#endif
                        ParseLine(line);
                        iLine = 0;
                        break;

                    default:
                        // add to line buffer
                        if (iLine < NMEA_LINE_SIZE)
                        {
                            lineBuf[iLine++] = (char)rcvBuf[i];
                        }
                        else
                        {
                            iLine = 0;
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Parses a line of text.
        /// </summary>
        /// <param name="line">the text</param>
        private void ParseLine(String line)
        {
            if (CheckSentence(line))
            {
                string[] parts = line.Split(',');
                if (parts.Length > 0)
                {
                    if (parts[0].Equals("$GPGGA"))
                    {
                        // GGA - Global positioning system fix data, time, position and fix related data for a GPS receiver. 
                        // $GPGGA,191410,4735.5634,N,00739.3538,E,1,04,4.4,351.5,M,48.0,M,,*45
                        try
                        {
                            if ((parts.Length != 15) || parts[6].Equals("0"))
                            {
#if DEBUG
                                Debug.Print("Length " + parts.Length + " " + parts[6]);
#endif
                                return;
                            }

                            cachedGpsData.Satellites = Byte.Parse(parts[7]);            // satellites
                            cachedGpsData.Altitude = (ushort)Double.Parse(parts[9]);    // altitude
                            OnGpsData();
                        }
                        catch (Exception e)
                        {
#if DEBUG
                            Debug.Print(e.Message);
#endif
                        }
                        
                    }
                    else if (parts[0].Equals("$GPRMC"))
                    {
                        // RMC - Recommended minimum navigation information
                        // $GPRMC,191410,A,4735.5634,N,00739.3538,E,0.0,0.0,181102,0.4,E,A*19
                        try
                        {
                            if ((parts.Length != 13) || !parts[2].Equals("A"))
                            {
#if DEBUG
                                Debug.Print("Length " + parts.Length + " " + parts[2]);
#endif
                                return;
                            }

                            if ((parts[9].Length == 6) && (parts[1].Length == 10))
                            {
                                string date = parts[9]; // UTC Date DDMMYY
                                string time = parts[1]; // HHMMSS.XXX
                                int year = 2000 + int.Parse(date.Substring(4, 2));
                                int month = int.Parse(date.Substring(2, 2));
                                int day = int.Parse(date.Substring(0, 2));
                                int hour = int.Parse(time.Substring(0, 2));
                                int minute = int.Parse(time.Substring(2, 2));
                                int second = int.Parse(time.Substring(4, 2));
                                int milliseconds = int.Parse(time.Substring(7, 3));
                                cachedGpsData.UtcTimestamp = new DateTime(year, month, day, hour, minute, second, milliseconds);
                            }

                            if (parts[3].Length == 9)
                            {
                                string lat = parts[3];  // HHMM.MMMM
                                float latHours = (float)Double.Parse(lat.Substring(0, 2));
                                float latMins = (float)Double.Parse(lat.Substring(2));
                                float latitude = latHours + latMins / 60.0f;
                                if (parts[4].Equals("S"))       // N or S
                                {
                                    latitude = -latitude;
                                }
                                cachedGpsData.Latitude = latitude;
                            }

                            if (parts[5].Length == 10)
                            {
                                string lng = parts[5];  // HHHMM.M
                                float lngHours = (float)Double.Parse(lng.Substring(0, 3));
                                float lngMins = (float)Double.Parse(lng.Substring(3));
                                float longitude = lngHours + lngMins / 60.0f;
                                if (parts[6].Equals("W"))
                                {
                                    longitude = -longitude;
                                }
                                cachedGpsData.Longitude = longitude;
                            }

                            cachedGpsData.HorizontalSpeed = (float)Double.Parse(parts[7]) * 0.51444f; // knots to m/s
                            cachedGpsData.Heading = (ushort)double.Parse(parts[8]);
                            OnGpsData();
                        }
                        catch (Exception e)
                        {
#if DEBUG
                            Debug.Print(e.Message);
#endif
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validates an NMEA sentence.
        /// </summary>
        /// <param name="strSentence">the NMEA sentence</param>
        /// <returns>true if valid, false if invalid</returns>
        private bool CheckSentence(string strSentence)
        {
            int iStart = strSentence.IndexOf('$');
            int iEnd = strSentence.IndexOf('*');
            if ((iStart != 0) || (iEnd < 0) || (strSentence.Length != iEnd + 3))
                return false;

            // validate checksum
            byte result = 0;
            for (int i = iStart + 1; i < iEnd; i++)
                result ^= (byte)strSentence[i];

            int cs = Convert.ToInt32(strSentence.Substring(iEnd + 1, 2), 16);
#if DEBUG
            Debug.Print("Checksum " + result + " " + cs);
#endif
            return (result == cs);
        }

        /// <summary>
        /// Handles new GPS data.
        /// </summary>
        private void OnGpsData()
        {
            if (GpsDataReceived != null)
                GpsDataReceived(cachedGpsData);
        }
    }
}
