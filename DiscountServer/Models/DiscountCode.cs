namespace DiscountServer.Models
{
    public class DiscountCode
    {
        public string Code { get; set; } = string.Empty;
        public bool IsUsed { get; set; } = false;
    }
}
