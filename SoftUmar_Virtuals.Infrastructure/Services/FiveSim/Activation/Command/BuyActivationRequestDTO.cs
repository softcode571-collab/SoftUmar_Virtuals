using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Infrastructure.Services.FiveSim.Activation.Command
{
    public class BuyActivationRequestDTO
    {
        public required string Country { get; set; }
        public required string Product { get; set; }
        public string Operator { get; set; } = "any";
    }
}
