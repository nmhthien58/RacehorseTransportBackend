namespace RacehorseTransport.Repositories.Entities
{
    // clearance.document_types
    public class DocumentType
    {
        public int DocTypeId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsMandatory { get; set; } = true;
        public string ProvidedBy { get; set; } = "Customer"; // DocumentProviders
    }
}
