namespace RacehorseTransport.Repositories.Entities
{
    // common.system_settings: cấu hình Admin / Manager sửa được trên giao diện (khóa - giá trị dạng chuỗi)
    public class SystemSetting
    {
        public string SettingKey { get; set; } = string.Empty;
        public string SettingValue { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? UpdatedByUserId { get; set; }
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
