using System.Collections.Generic;
using Microsoft.Graph;

namespace CseSample.Utils
{
    public static class AuthUtil
    {
        public static List<HeaderOption> CreateRequestHeader(string accessToken)
        {
            return new List<HeaderOption>() { new HeaderOption("Authorization", $"Bearer {accessToken}") };
        }
    }
}