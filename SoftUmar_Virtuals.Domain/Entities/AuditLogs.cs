using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftUmar_Virtuals.Domain.Entities
{
    public class AuditLogs
    {
        public Guid Id { get; set; }

        public string Level { get; set; } = default!; // INFO, WARNING, ERROR

        public string Message { get; set; } = default!;

        public string Method { get; set; } = default!;

        public string? TraceId { get; set; }

        public string? UserId { get; set; }

        public string? Data { get; set; } // JSON (request/response/etc)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
