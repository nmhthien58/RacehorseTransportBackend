namespace RacehorseTransport.Services.BusinessModels
{
    public class DocumentTypeModel
    {
        public int DocTypeId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsMandatory { get; set; }
    }
}
