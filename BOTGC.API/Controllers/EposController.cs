using BOTGC.API.Dto;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Controllers;

/// <summary>
/// EPOS-related endpoints for voucher-based benefits and reimbursements.
/// </summary>
[ApiController]
[Route("api/epos")]
[Produces("application/json")]
public class EposController(
    IOptions<AppSettings> settings,
    ILogger<EposController> logger,
    IMediator mediator) : Controller
{
    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly ILogger<EposController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    ///// <summary>
    ///// Gets the account summary, including allowance and usage.
    ///// </summary>
    ///// <param name="memberId">The member identifier.</param>
    //[HttpGet("accounts/{memberId:int}")]
    //[ProducesResponseType(typeof(AccountSummaryDto), StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status204NoContent)]
    //public async Task<IActionResult> GetAccount([FromRoute] int memberId)
    //{
    //    _logger.LogInformation("Fetching account summary for member {MemberId}...", memberId);

    //    try
    //    {
    //        var query = new GetAccountSummaryQuery(memberId);
    //        var result = await _mediator.Send(query, HttpContext.RequestAborted);

    //        if (result is null)
    //        {
    //            _logger.LogInformation("No account found for member {MemberId}.", memberId);
    //            return NoContent();
    //        }

    //        _logger.LogInformation("Successfully retrieved account summary for member {MemberId}.", memberId);
    //        return Ok(result);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error retrieving account summary for member {MemberId}.", memberId);
    //        return Problem("An error occurred while retrieving the account.", statusCode: StatusCodes.Status500InternalServerError);
    //    }
    //}

    ///// <summary>
    ///// Gets the current allowance balance for an account.
    ///// </summary>
    ///// <param name="memberId">The member identifier.</param>
    //[HttpGet("accounts/{memberId:int}/balance")]
    //[ProducesResponseType(typeof(AccountBalanceDto), StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status204NoContent)]
    //public async Task<IActionResult> GetAccountBalance([FromRoute] int memberId)
    //{
    //    _logger.LogInformation("Fetching balance for member {MemberId}...", memberId);

    //    try
    //    {
    //        var query = new GetAccountBalanceQuery(memberId);
    //        var result = await _mediator.Send(query, HttpContext.RequestAborted);

    //        if (result is null)
    //        {
    //            _logger.LogInformation("No balance found for member {MemberId}.", memberId);
    //            return NoContent();
    //        }

    //        _logger.LogInformation("Successfully retrieved balance for member {MemberId}.", memberId);
    //        return Ok(result);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error retrieving balance for member {MemberId}.", memberId);
    //        return Problem("An error occurred while retrieving the balance.", statusCode: StatusCodes.Status500InternalServerError);
    //    }
    //}

    ///// <summary>
    ///// Lists vouchers for a member account.
    ///// </summary>
    ///// <param name="memberId">The member identifier.</param>
    //[HttpGet("accounts/{memberId:int}/vouchers")]
    //[ProducesResponseType(typeof(List<AccountVoucherDto>), StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status204NoContent)]
    //public async Task<IActionResult> GetAccountVouchers([FromRoute] int memberId)
    //{
    //    _logger.LogInformation("Fetching vouchers for member {MemberId}...", memberId);

    //    try
    //    {
    //        var query = new GetAccountVouchersQuery(memberId);
    //        var vouchers = await _mediator.Send(query, HttpContext.RequestAborted);

    //        if (vouchers == null || vouchers.Count == 0)
    //        {
    //            _logger.LogInformation("No vouchers found for member {MemberId}.", memberId);
    //            return NoContent();
    //        }

    //        _logger.LogInformation("Successfully retrieved {Count} vouchers for member {MemberId}.", vouchers.Count, memberId);
    //        return Ok(vouchers);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error retrieving vouchers for member {MemberId}.", memberId);
    //        return Problem("An error occurred while retrieving vouchers.", statusCode: StatusCodes.Status500InternalServerError);
    //    }
    //}

    /// <summary>
    /// Gets current entitlements for a member, including what can be generated from the current balance.
    /// </summary>
    /// <param name="memberId">The member identifier.</param>
    [HttpGet("accounts/{memberId:int}/entitlements")]
    [ProducesResponseType(typeof(AccountEntitlementsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetAccountEntitlements([FromRoute] int memberId)
    {
        _logger.LogInformation("Fetching entitlements for member {MemberId}...", memberId);

        try
        {
            var query = new GetAccountEntitlementsQuery(memberId);
            var entitlements = await _mediator.Send(query, HttpContext.RequestAborted);

            if (entitlements is null)
            {
                _logger.LogInformation("No entitlements found for member {MemberId}.", memberId);
                return NoContent();
            }

            _logger.LogInformation("Successfully retrieved entitlements for member {MemberId}.", memberId);
            return Ok(entitlements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entitlements for member {MemberId}.", memberId);
            return Problem("An error occurred while retrieving entitlements.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    public sealed class EnsureEposAccountRequest
    {
        public int MemberId { get; init; }
        public int SubscriptionYear { get; init; }
        public decimal CreditAmount { get; init; }
    }

    [HttpPost("accounts")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> EnsureEPOSAccount([FromBody] EnsureEposAccountRequest request)
    {
        _logger.LogInformation("Ensuring EPOS account for member {MemberId}.", request.MemberId);

        var command = new UpdateBenefitsAccountCommand(request.MemberId, request.SubscriptionYear, request.CreditAmount);
        var result = await _mediator.Send(command, HttpContext.RequestAborted);

        return Ok(true);
    }

    ///// <summary>
    ///// Generates a voucher for the specified member (usually consuming allowance unless it is bonus).
    ///// </summary>
    ///// <param name="memberId">The member identifier.</param>
    ///// <param name="request">Voucher generation details.</param>
    //[HttpPost("accounts/{memberId:int}/vouchers")]
    //[ProducesResponseType(typeof(AccountVoucherDto), StatusCodes.Status201Created)]
    //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    //public async Task<IActionResult> GenerateVoucherForMember([FromRoute] int memberId, [FromBody] GenerateVoucherRequestDto request)
    //{
    //    _logger.LogInformation("Generating voucher for member {MemberId}.", memberId);

    //    if (request == null)
    //    {
    //        _logger.LogWarning("GenerateVoucherRequestDto is null for member {MemberId}.", memberId);
    //        return BadRequest(new ProblemDetails { Title = "Validation error", Detail = "Request body cannot be null." });
    //    }

    //    if (request.MemberId != memberId)
    //    {
    //        _logger.LogWarning("MemberId in route {RouteMemberId} does not match request.MemberId {BodyMemberId}.", memberId, request.MemberId);
    //        return BadRequest(new ProblemDetails { Title = "Validation error", Detail = "Route memberId does not match body MemberId." });
    //    }

    //    try
    //    {
    //        var command = new GenerateVoucherCommand(
    //            MemberId: memberId,
    //            ProductId: request.ProductId,
    //            IsBonus: request.IsBonus,
    //            AwardReason: request.AwardReason,
    //            ExpiresAtUtc: request.ExpiresAtUtc);

    //        var voucher = await _mediator.Send(command, HttpContext.RequestAborted);

    //        if (voucher is null)
    //        {
    //            _logger.LogWarning("Failed to generate voucher for member {MemberId}.", memberId);
    //            return BadRequest(new ProblemDetails { Title = "Voucher generation failed", Detail = "Unable to generate voucher with the supplied details or balance." });
    //        }

    //        _logger.LogInformation("Successfully generated voucher {VoucherId} for member {MemberId}.", voucher.VoucherId, memberId);
    //        return Created($"/api/epos/accounts/{memberId}/vouchers/{voucher.VoucherId}", voucher);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error generating voucher for member {MemberId}.", memberId);
    //        return Problem("An error occurred while generating the voucher.", statusCode: StatusCodes.Status500InternalServerError);
    //    }
    //}

    ///// <summary>
    ///// Awards one or more bonus vouchers to a member. These do not consume allowance.
    ///// </summary>
    ///// <param name="memberId">The member identifier.</param>
    ///// <param name="request">Award details and vouchers to create.</param>
    //[HttpPost("accounts/{memberId:int}/awards")]
    //[ProducesResponseType(typeof(AwardBonusVouchersResultDto), StatusCodes.Status201Created)]
    //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    //public async Task<IActionResult> AwardBonusVouchers([FromRoute] int memberId, [FromBody] AwardBonusVouchersRequestDto request)
    //{
    //    _logger.LogInformation("Awarding bonus vouchers to member {MemberId}.", memberId);

    //    if (request == null)
    //    {
    //        _logger.LogWarning("AwardBonusVouchersRequestDto is null for member {MemberId}.", memberId);
    //        return BadRequest(new ProblemDetails { Title = "Validation error", Detail = "Request body cannot be null." });
    //    }

    //    if (request.Items == null || request.Items.Count == 0)
    //    {
    //        _logger.LogWarning("No award items supplied for member {MemberId}.", memberId);
    //        return BadRequest(new ProblemDetails { Title = "Validation error", Detail = "No award items supplied." });
    //    }

    //    if (request.Items.Any(i => i.MemberId != memberId))
    //    {
    //        _logger.LogWarning("One or more award items have MemberId not equal to route memberId {MemberId}.", memberId);
    //        return BadRequest(new ProblemDetails { Title = "Validation error", Detail = "All award items must use the same MemberId as the route." });
    //    }

    //    try
    //    {
    //        var command = new AwardBonusVouchersCommand(request);
    //        var result = await _mediator.Send(command, HttpContext.RequestAborted);

    //        _logger.LogInformation("Successfully awarded {Created} bonus vouchers (requested {Requested}) for member {MemberId}.",
    //            result.TotalCreated, result.TotalRequested, memberId);

    //        return Created($"/api/epos/accounts/{memberId}/vouchers", result);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error awarding bonus vouchers to member {MemberId}.", memberId);
    //        return Problem("An error occurred while awarding bonus vouchers.", statusCode: StatusCodes.Status500InternalServerError);
    //    }
    //}

    ///// <summary>
    ///// Redeems a voucher by code, typically invoked after scanning a QR or barcode.
    ///// </summary>
    ///// <param name="request">Voucher redemption request.</param>
    //[HttpPost("vouchers/redeem")]
    //[ProducesResponseType(typeof(VoucherRedemptionResultDto), StatusCodes.Status200OK)]
    //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    //public async Task<IActionResult> RedeemVoucher([FromBody] RedeemVoucherCommand request)
    //{
    //    _logger.LogInformation("Redeeming voucher from request...");

    //    if (request == null)
    //    {
    //        _logger.LogWarning("RedeemVoucherCommand is null.");
    //        return BadRequest(new ProblemDetails { Title = "Validation error", Detail = "Request body cannot be null." });
    //    }

    //    try
    //    {
    //        var result = await _mediator.Send(request, HttpContext.RequestAborted);

    //        if (result is null)
    //        {
    //            _logger.LogWarning("Voucher redemption failed, voucher not found, invalid or already redeemed.");
    //            return NotFound(new ProblemDetails { Title = "Voucher not found", Detail = "The supplied voucher code could not be redeemed." });
    //        }

    //        _logger.LogInformation("Successfully redeemed voucher {VoucherId}.", result.VoucherId);
    //        return Ok(result);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error redeeming voucher.");
    //        return Problem("An error occurred while redeeming the voucher.", statusCode: StatusCodes.Status500InternalServerError);
    //    }
    //}

    ///// <summary>
    ///// Gets the list of benefit products (coaching, shop credits, promotions).
    ///// </summary>
    //[HttpGet("products")]
    //[ProducesResponseType(typeof(List<ProductDto>), StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status204NoContent)]
    //public async Task<IActionResult> GetProducts()
    //{
    //    _logger.LogInformation("Fetching EPOS products...");

    //    try
    //    {
    //        var query = new GetProductsQuery();
    //        var products = await _mediator.Send(query, HttpContext.RequestAborted);

    //        if (products == null || products.Count == 0)
    //        {
    //            _logger.LogInformation("No EPOS products found.");
    //            return NoContent();
    //        }

    //        _logger.LogInformation("Successfully retrieved {Count} EPOS products.", products.Count);
    //        return Ok(products);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error retrieving EPOS products.");
    //        return Problem("An error occurred while retrieving products.", statusCode: StatusCodes.Status500InternalServerError);
    //    }
    //}

    ///// <summary>
    ///// Gets a summary of amounts due to the pro shop for redeemed, uninvoiced vouchers.
    ///// </summary>
    ///// <param name="from">Inclusive start date (UTC).</param>
    ///// <param name="to">Inclusive end date (UTC).</param>
    //[HttpGet("pro-shop/invoices/summary")]
    //[ProducesResponseType(typeof(ProShopInvoiceSummaryDto), StatusCodes.Status200OK)]
    //public async Task<IActionResult> GetProShopInvoiceSummary([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    //{
    //    _logger.LogInformation("Fetching pro shop invoice summary...");

    //    try
    //    {
    //        DateTimeOffset? fromUtc = from.HasValue
    //            ? new DateTimeOffset(DateTime.SpecifyKind(from.Value, DateTimeKind.Utc))
    //            : null;

    //        DateTimeOffset? toUtc = to.HasValue
    //            ? new DateTimeOffset(DateTime.SpecifyKind(to.Value, DateTimeKind.Utc))
    //            : null;

    //        var query = new GetProShopInvoiceSummaryQuery(fromUtc, toUtc);
    //        var summary = await _mediator.Send(query, HttpContext.RequestAborted);

    //        _logger.LogInformation("Successfully retrieved pro shop invoice summary.");
    //        return Ok(summary);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error retrieving pro shop invoice summary.");
    //        return Problem("An error occurred while retrieving pro shop invoice summary.", statusCode: StatusCodes.Status500InternalServerError);
    //    }
    //}

    ///// <summary>
    ///// Gets details of a specific pro shop invoice.
    ///// </summary>
    ///// <param name="invoiceId">The invoice identifier.</param>
    //[HttpGet("pro-shop/invoices/{invoiceId:guid}")]
    //[ProducesResponseType(typeof(ProShopInvoiceDetailDto), StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status404NotFound)]
    //public async Task<IActionResult> GetProShopInvoice([FromRoute] Guid invoiceId)
    //{
    //    _logger.LogInformation("Fetching pro shop invoice {InvoiceId}.", invoiceId);

    //    try
    //    {
    //        var query = new GetProShopInvoiceDetailQuery(invoiceId);
    //        var invoice = await _mediator.Send(query, HttpContext.RequestAborted);

    //        if (invoice is null)
    //        {
    //            _logger.LogInformation("Pro shop invoice {InvoiceId} not found.", invoiceId);
    //            return NotFound(new { ok = false, message = "Invoice not found." });
    //        }

    //        _logger.LogInformation("Successfully retrieved pro shop invoice {InvoiceId}.", invoiceId);
    //        return Ok(invoice);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error retrieving pro shop invoice {InvoiceId}.", invoiceId);
    //        return Problem("An error occurred while retrieving the pro shop invoice.", statusCode: StatusCodes.Status500InternalServerError);
    //    }
    //}

    ///// <summary>
    ///// Gets line-level details for a specific pro shop invoice.
    ///// </summary>
    ///// <param name="invoiceId">The invoice identifier.</param>
    //[HttpGet("pro-shop/invoices/{invoiceId:guid}/lines")]
    //[ProducesResponseType(typeof(List<ProShopInvoiceLineDto>), StatusCodes.Status200OK)]
    //public async Task<IActionResult> GetProShopInvoiceLines([FromRoute] Guid invoiceId)
    //{
    //    _logger.LogInformation("Fetching pro shop invoice lines for invoice {InvoiceId}.", invoiceId);

    //    try
    //    {
    //        var query = new GetProShopInvoiceLinesQuery(invoiceId);
    //        var lines = await _mediator.Send(query, HttpContext.RequestAborted);

    //        _logger.LogInformation("Successfully retrieved {Count} invoice lines for invoice {InvoiceId}.", lines.Count, invoiceId);
    //        return Ok(lines);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error retrieving pro shop invoice lines for invoice {InvoiceId}.", invoiceId);
    //        return Problem("An error occurred while retrieving the pro shop invoice lines.", statusCode: StatusCodes.Status500InternalServerError);
    //    }
    //}

}
