using System;

namespace guideXOS.OS {
    internal static class Session {
        // Base URL of guideXOS.Web service. Adjust if your web server runs on a different port.
        public static string ServiceBaseUrl = "http://localhost:5068/";
        // Current authenticated login token (GUID string). Empty if not logged in.
        public static string LoginToken = string.Empty;
    }
}
