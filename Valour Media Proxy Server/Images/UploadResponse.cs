﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Valour.MPS.Images
{
    public class UploadResponse
    {
        [JsonPropertyName("location")]
        public string Url { get; set; }
    }
}
