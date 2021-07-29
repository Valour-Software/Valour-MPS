using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Valour.MPS.API
{
    public static class JsonExtensions
    {
        private static JsonSerializerOptions Options = new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = false
        };

        public static string SerializeJson(this object obj)
        {
            return JsonSerializer.Serialize(obj, Options);
        }

        public async static Task SerializeJsonAsync(this object obj, Stream stream)
        {
            await JsonSerializer.SerializeAsync(stream, obj, Options);
        }
    }
}
