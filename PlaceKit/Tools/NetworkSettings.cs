using System.Net;

namespace PlaceKit.Tools
{
    public static class NetworkSettings
    {
        // Office runs add-ins with .NET 4.0 defaults (SSL 3 and TLS 1.0), and the AI APIs need TLS 1.2.
        // SystemDefault already lets Windows pick, so leave it alone.
        public static void UseModernTls()
        {
            if (ServicePointManager.SecurityProtocol != SecurityProtocolType.SystemDefault)
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
        }
    }
}
