using System;
using System.Globalization;
using System.Net;

namespace DtronixMessageQueue
{
    /// <summary>
    /// Static helper utility class.
    /// </summary>
    internal static class Utilities
    {
        /// <summary>
        /// Creates a frame with the specified parameters.
        /// Fixes common issues with frame creation.
        /// </summary>
        /// <param name="bytes">Byte buffer to add to this frame.</param>
        /// <param name="type">Type of frame to create.</param>
        /// <param name="config">Socket configurations for the frame to use.</param>
        /// <returns>Configured frame.</returns>
        public static MqFrame CreateFrame(byte[] bytes, MqFrameType type, MqConfig config)
        {
            if (type == MqFrameType.Ping || type == MqFrameType.Empty || type == MqFrameType.EmptyLast)
            {
                bytes = null;
            }
            return new MqFrame(bytes, type, config);
        }

        /// <summary>
        /// Parses an IPv4 and IPv6 strings with port into an IpEndpoint.
        /// </summary>
        /// <param name="endPoint">Endpoint string to parse.</param>
        /// <returns>Configured IPEndPoint from the input string.</returns>
        /// <remarks>
        /// Created by Jens Granlund
        /// https://stackoverflow.com/a/2727880
        /// </remarks>
        // ReSharper disable once InconsistentNaming
        public static IPEndPoint CreateIPEndPoint(string endPoint)
        {
            string[] ep = endPoint.Split(':');
            if (ep.Length < 2)
                throw new FormatException("Invalid endpoint format");

            IPAddress ip;

            if (ep.Length > 2)
            {
                if (!IPAddress.TryParse(string.Join(":", ep, 0, ep.Length - 1), out ip))
                {
                    throw new FormatException("Invalid ip-adress");
                }
            }
            else
            {
                if (!IPAddress.TryParse(ep[0], out ip))
                {
                    throw new FormatException("Invalid ip-adress");
                }
            }
            int port;
            if (!int.TryParse(ep[ep.Length - 1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
            {
                throw new FormatException("Invalid port");
            }
            return new IPEndPoint(ip, port);
        }
    }
}