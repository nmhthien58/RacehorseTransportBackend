namespace RacehorseTransport.Repositories.Entities
{
    // common.countries (R-18): phạm vi dịch vụ. Nước có trong bảng mới chọn được làm nước đi / nước đến.
    // Có nhận đường bộ / đường bay hay không suy ra từ dòng cước FREIGHT đang bật trong pricing.price_items.
    public class Country
    {
        public string CountryCode { get; set; } = string.Empty;
        public string NameVi { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }

        public ICollection<Airport> Airports { get; set; } = new List<Airport>();
    }
}
