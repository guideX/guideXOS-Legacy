using System.Runtime.InteropServices;

namespace guideXOS
{
    internal static class SSE
    {
        [DllImport("*")]
        public static extern void enable_sse();
    }
}