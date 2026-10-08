namespace RacehorseTransport.API.ResponseModels
{
    public class DocumentTypeResponse
    {
        public int DocTypeId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsMandatory { get; set; }
    }
}
