using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RacehorseTransport.API.Mappers;
using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.Exceptions;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.API.Controllers
{
    // BE-01 / R-11: bản đồ cho form tạo đơn, lập tuyến, hiển thị tuyến. Goong trong Việt Nam, Nominatim + OSRM ngoài Việt Nam.
    [ApiController]
    [Route("api/geo")]
    [Produces("application/json")]
    [Authorize]
    public class GeoController : ControllerBase
    {
        private readonly IGeoService _geoService;

        public GeoController(IGeoService geoService)
        {
            _geoService = geoService;
        }

        /// <summary>
        /// Address suggestions while typing. Optional country (2-letter ISO code) narrows the search; VN uses Goong.
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<List<GeoPlaceResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search([FromQuery] GeoSearchRequest request, CancellationToken cancellationToken)
        {
            var places = await _geoService.SearchAsync(request.Q, request.Country, cancellationToken);
            return Ok(ApiResponse<List<GeoPlaceResponse>>.SuccessResult(places.Select(p => p.ToResponse()).ToList()));
        }

        /// <summary>
        /// Address and country of a point (when the user clicks or drags a pin on the map).
        /// </summary>
        [HttpGet("reverse")]
        [ProducesResponseType(typeof(ApiResponse<GeoPlaceResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Reverse([FromQuery] GeoReverseRequest request, CancellationToken cancellationToken)
        {
            var place = await _geoService.ReverseAsync(request.Lat!.Value, request.Lng!.Value, cancellationToken)
                ?? throw new NotFoundException("No address was found at this location. Pick a point on land or search for the address.");
            return Ok(ApiResponse<GeoPlaceResponse>.SuccessResult(place.ToResponse()));
        }

        /// <summary>
        /// Road route through the points in order: distance and driving time of each leg, totals, and the line to draw.
        /// A leg whose provider fails is estimated from the straight-line distance (isEstimate = true).
        /// </summary>
        [HttpPost("route")]
        [ProducesResponseType(typeof(ApiResponse<GeoRouteResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Route([FromBody] GeoRouteRequest request, CancellationToken cancellationToken)
        {
            var route = await _geoService.RouteAsync(request.ToBusinessModel(), request.IncludeCountries, cancellationToken);
            return Ok(ApiResponse<GeoRouteResponse>.SuccessResult(route.ToResponse()));
        }
    }
}
