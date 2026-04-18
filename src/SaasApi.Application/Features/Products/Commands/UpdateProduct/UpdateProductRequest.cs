namespace SaasApi.Application.Features.Products.Commands.UpdateProduct
{
    public class UpdateProductRequest
    {
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string? Slug { get; set; }
        public string? ImageUrl { get; set; }
        public string? Sku { get; set; }
    }
}
