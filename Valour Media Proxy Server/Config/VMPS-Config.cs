using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Valour.MPS.Config
{
    public class VMPS_Config
    {
        /// <summary>
        /// The static instance of the current instance
        /// </summary>
        public static VMPS_Config Current;

        // Cross-server authorization

        [JsonProperty("auth_key")]
        public string Authorization_Key { get; set; }

        // Database properties

        [JsonProperty("db_address")]
        public string Database_Address { get; set; }

        [JsonProperty("db_user")]
        public string Database_User { get; set; }

        [JsonProperty("db_pass")]
        public string Database_Password { get; set; }
    }
}
