// --- Apply to your real AuthController ---
// Your Program.cs already defines the "fixed" limiter (10 requests/min).
// It just isn't attached to any endpoint yet. Add the [EnableRateLimiting]
// attribute at the controller or action level:

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace SoftUmar_Virtuals.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [EnableRateLimiting("fixed")] // applies to EVERY action in this controller —
                                  // register, login, refresh, password reset, etc.
    public class AuthController(IMediator mediator) : ControllerBase
    {
        // ... existing actions unchanged ...
    }
}

// If you'd rather rate-limit ONLY login (not every auth endpoint), remove the
// controller-level attribute and put it on just that one action instead:
//
//   [HttpPost("login")]
//   [EnableRateLimiting("fixed")]
//   public async Task<IActionResult> Login([FromBody] LoginDTO dto) { ... }
//
// Worth considering: 10 requests/min shared across ALL auth endpoints (current
// setup) means a user who fails login a few times could also get blocked from
// registering or resetting their password in the same window, since they're
// all counted against the same limiter. A per-endpoint or per-user-action
// limiter is more correct long-term, but the single "fixed" policy you already
// have is a reasonable starting point — just know that's the trade-off.