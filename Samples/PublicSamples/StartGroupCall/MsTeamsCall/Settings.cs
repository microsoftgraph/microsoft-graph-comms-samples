using System;

namespace CseSample
{
    public static class Settings
    {
        public static string CallBackEndpoint = Environment.GetEnvironmentVariable("CallBackUrl");
    }
}