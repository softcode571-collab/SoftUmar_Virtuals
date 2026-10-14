using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftUmar_Virtuals.Application.Features.Wallet.Query;
using SoftUmar_Virtuals.Application.Interfaces;

namespace SoftUmar_Virtuals.API.Controllers
{
    [ApiController]
    [Route("api/wallet")]
    [Authorize] 
    public class WalletController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase


    {
        [HttpGet]
        public async Task<IActionResult> GetWallet()
        {
            if (currentUser.UserId is not Guid userId)
                return Unauthorized();

            var result = await mediator.Send(new GetWalletQuery(userId));

            if (result.IsFailed)
                return NotFound(new { message = result.Errors.First().Message });

            return Ok(result.Value);
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (currentUser.UserId is not Guid userId)
                return Unauthorized();

            var result = await mediator.Send(new GetWalletTransactionsQuery(userId, page, pageSize));

            if (result.IsFailed)
                return NotFound(new { message = result.Errors.First().Message });

            return Ok(result.Value);
        }
    }
}