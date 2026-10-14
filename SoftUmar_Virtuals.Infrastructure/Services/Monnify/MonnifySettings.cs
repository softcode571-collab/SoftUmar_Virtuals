using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Infrastructure.Services.Monnify
{
    
    public class MonnifySettings
    {
        public required string BaseUrl { get; set; }
        public required string ApiKey { get; set; }
        public required string SecretKey { get; set; }
        public required string ContractCode { get; set; }
    }
}
