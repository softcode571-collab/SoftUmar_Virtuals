using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Application.Interfaces
{
    // STUB INTERFACE: this needs to exist and be registered in DI for
    // RegisterCommandHandler to compile and for the Hangfire job to resolve it.
    // The actual Monnify API call implementation is a separate piece —
    // ask me for it whenever you're ready and I'll fill Task CreateReservedAccount
    // with the real Create Reserved Account API call, response handling, and
    // the wallet-status update (Pending -> Active/Failed).
    public interface IMonnifyReservedAccountService
    {
        Task CreateReservedAccount(Guid userId);
    }
}
