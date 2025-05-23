using Newtonsoft.Json;

namespace Nop.Plugin.Misc.Api.Models.TokenModels
{
    public class TokenRequestModel
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
    }
}
