using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RacehorseTransport.API.Mappers;
using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.API.Controllers
{
    // R-18: phạm vi dịch vụ theo quốc gia. Danh sách cho form đặt chuyến ai cũng xem được;
    // phần cấu hình (nước, cước theo phương thức, sân bay) chỉ Manager / Admin.
    [ApiController]
    [Route("api/countries")]
    [Produces("application/json")]
    [Authorize(Roles = ManageRoles)]
    public class CountriesController : ControllerBase
    {
        private const string ManageRoles = UserRoles.LogisticsManager + "," + UserRoles.Admin;

        private readonly ICountryService _countryService;

        public CountriesController(ICountryService countryService)
        {
            _countryService = countryService;
        }

        /// <summary>
        /// Danh sách nước đang nhận cho form đặt chuyến: phương thức nào đặt được tới mỗi nước và sân bay đang mở.
        /// </summary>
        [HttpGet("catalog")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<List<CountryCatalogResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCatalog()
        {
            var catalog = await _countryService.GetCatalogAsync();
            return Ok(ApiResponse<List<CountryCatalogResponse>>.SuccessResult(
                catalog.Select(c => c.ToResponse()).ToList(), "Country catalog retrieved successfully"));
        }

        /// <summary>
        /// Tất cả quốc gia (kể cả đang tắt) kèm cước, số sân bay, số giấy tờ và cảnh báo thiếu thông tin.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<CountrySummaryResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAll()
        {
            var countries = await _countryService.GetCountriesAsync();
            return Ok(ApiResponse<List<CountrySummaryResponse>>.SuccessResult(
                countries.Select(c => c.ToResponse()).ToList(), "Countries retrieved successfully"));
        }

        /// <summary>
        /// Chi tiết một nước: cước đường bộ / đường bay, sân bay, giấy tờ yêu cầu, phí riêng của nước.
        /// </summary>
        [HttpGet("{countryCode:length(2)}")]
        [ProducesResponseType(typeof(ApiResponse<CountryDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByCode(string countryCode)
        {
            var country = await _countryService.GetCountryAsync(countryCode);
            return Ok(ApiResponse<CountryDetailResponse>.SuccessResult(country.ToResponse()));
        }

        /// <summary>
        /// Thêm một nước vào phạm vi dịch vụ (đang bật, chưa có cước: chỉ làm nước đi / quá cảnh cho tới khi nhập cước).
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CountryDetailResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateCountryRequest request)
        {
            var created = await _countryService.CreateCountryAsync(request.ToBusinessModel());
            return CreatedAtAction(nameof(GetByCode), new { countryCode = created.CountryCode },
                ApiResponse<CountryDetailResponse>.SuccessResult(created.ToResponse(), "Country created successfully"));
        }

        /// <summary>
        /// Sửa tên và thứ tự hiển thị của một nước.
        /// </summary>
        [HttpPut("{countryCode:length(2)}")]
        [ProducesResponseType(typeof(ApiResponse<CountryDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(string countryCode, [FromBody] UpdateCountryRequest request)
        {
            var updated = await _countryService.UpdateCountryAsync(countryCode, request.ToBusinessModel(countryCode));
            return Ok(ApiResponse<CountryDetailResponse>.SuccessResult(updated.ToResponse(), "Country updated successfully"));
        }

        /// <summary>
        /// Bật / tắt cả nước. Tắt thì khách không chọn được nước này làm nước đi hay nước đến; đơn cũ không bị ảnh hưởng.
        /// </summary>
        [HttpPatch("{countryCode:length(2)}/active")]
        [ProducesResponseType(typeof(ApiResponse<CountryDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetActive(string countryCode, [FromBody] SetCountryActiveRequest request)
        {
            var updated = await _countryService.SetActiveAsync(countryCode, request.IsActive!.Value);
            return Ok(ApiResponse<CountryDetailResponse>.SuccessResult(updated.ToResponse(), "Country status updated successfully"));
        }

        /// <summary>
        /// Đặt cước và bật / tắt một phương thức (Ground / Air) cho nước đến này. Chưa có dòng cước thì tạo mới.
        /// </summary>
        [HttpPut("{countryCode:length(2)}/freight/{transportMode}")]
        [ProducesResponseType(typeof(ApiResponse<CountryDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetFreight(string countryCode, string transportMode, [FromBody] SetFreightRequest request)
        {
            var updated = await _countryService.SetFreightAsync(countryCode, transportMode, request.Value!.Value, request.IsActive!.Value);
            return Ok(ApiResponse<CountryDetailResponse>.SuccessResult(updated.ToResponse(), "Freight rate saved successfully"));
        }

        /// <summary>
        /// Thêm sân bay cho một nước (đang mở).
        /// </summary>
        [HttpPost("{countryCode:length(2)}/airports")]
        [ProducesResponseType(typeof(ApiResponse<CountryDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddAirport(string countryCode, [FromBody] SaveAirportRequest request)
        {
            var updated = await _countryService.AddAirportAsync(countryCode, request.ToBusinessModel());
            return Ok(ApiResponse<CountryDetailResponse>.SuccessResult(updated.ToResponse(), "Airport added successfully"));
        }

        /// <summary>
        /// Sửa mã, tên, thành phố của một sân bay.
        /// </summary>
        [HttpPut("airports/{airportId:int}")]
        [ProducesResponseType(typeof(ApiResponse<CountryDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAirport(int airportId, [FromBody] SaveAirportRequest request)
        {
            var updated = await _countryService.UpdateAirportAsync(airportId, request.ToBusinessModel());
            return Ok(ApiResponse<CountryDetailResponse>.SuccessResult(updated.ToResponse(), "Airport updated successfully"));
        }

        /// <summary>
        /// Mở / đóng một sân bay. Nước không còn sân bay mở thì không đặt được đường bay.
        /// </summary>
        [HttpPatch("airports/{airportId:int}/active")]
        [ProducesResponseType(typeof(ApiResponse<CountryDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetAirportActive(int airportId, [FromBody] SetCountryActiveRequest request)
        {
            var updated = await _countryService.SetAirportActiveAsync(airportId, request.IsActive!.Value);
            return Ok(ApiResponse<CountryDetailResponse>.SuccessResult(updated.ToResponse(), "Airport status updated successfully"));
        }
    }
}
