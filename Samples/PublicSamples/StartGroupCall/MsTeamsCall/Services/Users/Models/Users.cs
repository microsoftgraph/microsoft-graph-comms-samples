using Microsoft.Graph;
using Newtonsoft.Json;

namespace CseSample.Services
{
    public class Users
    {
        [JsonProperty("value")]
        public User[] Value { get; set; }
    }
}
