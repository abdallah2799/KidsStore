namespace WebUI.Models
{
    public class CreateVariantRequest
    {
        public int ProductId { get; set; }
        public string Color { get; set; } = string.Empty;
        public int Size { get; set; }
    }
}
