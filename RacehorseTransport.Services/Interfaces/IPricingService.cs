using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    public interface IPricingService
    {
        // Tính báo giá từ bảng pricing.price_items. Không ghi gì vào DB.
        Task<QuoteModel> CalculateQuoteAsync(QuoteInputModel input);

        // Chuyển các dòng báo giá sang / từ JSON lưu ở booking_requests.quote_breakdown
        string SerializeLines(IEnumerable<QuoteLineModel> lines);
        List<QuoteLineModel> ParseLines(string? quoteBreakdownJson);
    }
}
