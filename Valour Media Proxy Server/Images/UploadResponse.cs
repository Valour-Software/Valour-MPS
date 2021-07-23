using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Valour.MPS.Images
{
    public class UploadResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }
    }
}
