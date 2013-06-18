using System;

namespace M3Space.Capsule.Drivers
{
    public struct GpsPoint
    {
        public DateTime UtcTimestamp;   // 
        public float Latitude;          // [°]
        public float Longitude;         // [°]
        public float HorizontalSpeed;   // [m/s]
        public float VerticalSpeed;     // [m/s]
        public float Heading;           // [°]
        public float Altitude;          // [m]
        public byte Satellites;         // [#]
    }
}
