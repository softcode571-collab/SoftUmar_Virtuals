using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace SoftUmar_Virtuals.Application.Features.FiveSim.Activation.Query


{
    public class ActivationStatusDTO
    {
        public Guid ActivationId { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Status { get; set; }
        public string? OtpCode { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
