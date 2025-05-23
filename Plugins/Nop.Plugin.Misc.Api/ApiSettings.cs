using Nop.Core.Configuration;

namespace Nop.Plugin.Misc.Api.Domain
{
    public class ApiSettings : ISettings
    {
        public ApiSettings()
        {
        }

        public bool EnableApi { get; set; } = true;

        public int TokenExpiryInDays { get; set; } = 0;
    }
}
