using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Domain.Enum
{
    public enum ReservedAccountStatus
    {
        Pending = 0, // set at registration, before the Hangfire job runs
        Active = 1,  // Monnify account created successfully
        Failed = 2,  // job exhausted its retries — needs manual attention
    }
}
