using System.Net;

namespace PlaceKit.Tools
{
    public static class NetworkSettings
    {
        // Office runs the add-in with the defaults of .NET 4.0, which offer only SSL 3 and TLS 1.0, so requests to the
        // Claude and OpenAI APIs fail with "Could not create SSL/TLS secure channel". They need TLS 1.2.
        // SystemDefault, as in a .NET 4.7+ program, already lets Windows choose and is left alone.
        public static void UseModernTls()
        {
            if (ServicePointManager.SecurityProtocol != SecurityProtocolType.SystemDefault)
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
        }
    }
}
