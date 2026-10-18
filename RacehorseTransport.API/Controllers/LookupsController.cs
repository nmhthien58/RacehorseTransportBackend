using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Repositories.Constants;

namespace RacehorseTransport.API.Controllers
{
    [ApiController]
    [Route("api/lookups")]
    [Produces("application/json")]
    [Authorize]
    public class LookupsController : ControllerBase
    {
        /// <summary>
        /// Lấy toàn bộ giá trị danh mục dùng cho các danh sách chọn của giao diện người dùng.
        /// </summary>
        [HttpGet("enums")]
        [ProducesResponseType(typeof(ApiResponse<EnumLookupsResponse>), StatusCodes.Status200OK)]
        public IActionResult GetEnums()
        {
            var response = new EnumLookupsResponse
            {
                UserRoles = UserRoles.All.ToArray(),
                HorseGenders = HorseGenders.All.ToArray(),
                BookingStatuses = BookingStatuses.All.ToArray(),
                BookingHorseStatuses = BookingHorseStatuses.All.ToArray(),
                TransportModes = TransportModes.All.ToArray(),
                StallClasses = StallClasses.All.ToArray(),
                DossierStatuses = DossierStatuses.All.ToArray(),
                DocumentStatuses = DocumentStatuses.All.ToArray(),
                RequirementDirections = RequirementDirections.All.ToArray(),
                AssetTypes = AssetTypes.All.ToArray(),
                AssetStatuses = AssetStatuses.All.ToArray(),
                TripStatuses = TripStatuses.All.ToArray(),
                OnTimeStatuses = new[]
                {
                    OnTimeStatuses.OnTime,
                    OnTimeStatuses.DelayedForceMajeure,
                    OnTimeStatuses.DelayedOperationalFault
                },
                CrewRoles = CrewRoles.All.ToArray(),
                CheckpointTypes = CheckpointTypes.All.ToArray(),
                CheckpointStatuses = CheckpointStatuses.All.ToArray(),
                TripHorseStatuses = TripHorseStatuses.All.ToArray(),
                FeedStatuses = FeedStatuses.All.ToArray(),
                StressLevels = StressLevels.All.ToArray(),
                IncidentTypes = IncidentTypes.All.ToArray(),
                IncidentSeverities = IncidentSeverities.All.ToArray(),
                IncidentStatuses = IncidentStatuses.All.ToArray(),
                HandoverConditions = HandoverConditions.All.ToArray(),
                PriceCategories = PriceCategories.All.ToArray(),
                PriceCalcTypes = PriceCalcTypes.All.ToArray(),
                PaymentTypes = PaymentTypes.All.ToArray(),
                PaymentStatuses = PaymentStatuses.All.ToArray(),
                PaymentProviders = PaymentProviders.All.ToArray()
            };

            return Ok(ApiResponse<EnumLookupsResponse>.SuccessResult(response, "Enum lookups retrieved successfully"));
        }
    }
}
