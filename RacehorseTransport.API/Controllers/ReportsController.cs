using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RacehorseTransport.API.Mappers;
using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.API.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Produces("application/json")]
    [Authorize(Roles = UserRoles.LogisticsManager + "," + UserRoles.Admin)]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        /// <summary>
        /// Summary of the trips closed between 'from' and 'to' (both optional): revenue, operating cost, on-time rate, average KPI, and incidents by type.
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(ApiResponse<ReportSummaryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetSummary([FromQuery] ReportQueryRequest query)
        {
            var summary = await _reportService.GetSummaryAsync(query.From, query.To);
            return Ok(ApiResponse<ReportSummaryResponse>.SuccessResult(summary.ToResponse()));
        }
    }
}
