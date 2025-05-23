using System.Collections.Generic;

namespace Nop.Plugin.Misc.Api.Models.BaseModels
{
    public partial class BaseResponseModel
    {
        public BaseResponseModel()
        {
            Errors = new List<string>();
        }
        public int StatusCode { get; set; }

        public string Message { get; set; }

        public List<string> Errors { get; set; }

        public object Data { get; set; }

    }
}