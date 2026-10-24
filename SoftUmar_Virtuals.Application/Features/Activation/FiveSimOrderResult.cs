using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Infrastructure.Services.FiveSim
{
    public class FiveSimOrderResult
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("operator")]
        public string? Operator { get; set; }

        [JsonPropertyName("product")]
        public string? Product { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; } // PENDING, RECEIVED, CANCELED, TIMEOUT, FINISHED, BANNED

        [JsonPropertyName("expires")]
        public DateTime? Expires { get; set; }

        [JsonPropertyName("sms")]
        public List<FiveSimSms>? Sms { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }
    }
}

