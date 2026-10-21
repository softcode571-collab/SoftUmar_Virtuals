using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Application.Features.FiveSim.Activation.Command;

public class BuyActivationResult
{
    public Guid ActivationId { get; set; }
    public required string PhoneNumber { get; set; }
    public decimal Price { get; set; }
    public required string Status { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
