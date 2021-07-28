using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Valour.MPS.Config
{
    public class VMPS_Config
    {
        /// <summary>
        /// The static instance of the current instance
        /// </summary>
        public static VMPS_Config Current;

        public VMPS_Config()
        {
            Current = this;
        }

        // Cross-server authorization

        [JsonPropertyName("auth_key")]
        public string Authorization_Key { get; set; }

        // Database properties

        [JsonPropertyName("db_address")]
        public string Database_Address { get; set; }

        [JsonPropertyName("db_user")]
        public string Database_User { get; set; }

        [JsonPropertyName("db_pass")]
        public string Database_Password { get; set; }

        [JsonPropertyName("db_name")]
        public string Database_Name { get; set; }
    }
}
