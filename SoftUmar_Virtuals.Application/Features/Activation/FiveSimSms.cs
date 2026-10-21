using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Infrastructure.Services.FiveSim
{
    public class FiveSimSms
    {
        [JsonPropertyName("sender")]
        public string? Sender { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; } // the actual OTP, already extracted by 5SIM
    }
}
