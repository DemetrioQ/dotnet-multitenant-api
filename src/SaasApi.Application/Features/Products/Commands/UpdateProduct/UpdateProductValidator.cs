using FluentValidation;

namespace SaasApi.Application.Features.Products.Commands.UpdateProduct
{
    public class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
    {
        public UpdateProductValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
            RuleFor(x => x.Price).GreaterThan(0);
            RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);

            RuleFor(x => x.Slug)
                .MaximumLength(200)
                .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
                .WithMessage("Slug must be lowercase alphanumeric and may contain hyphens (e.g. 'my-product').")
                .When(x => !string.IsNullOrWhiteSpace(x.Slug));

            RuleFor(x => x.ImageUrl).MaximumLength(500);
            RuleFor(x => x.Sku).MaximumLength(100);
        }
    }
}
