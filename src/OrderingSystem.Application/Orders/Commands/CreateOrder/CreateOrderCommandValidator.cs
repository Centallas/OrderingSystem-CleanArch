using FluentValidation;

namespace OrderingSystem.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandValidatorNew : AbstractValidator<CreateOrderCommand>
{
   public CreateOrderCommandValidatorNew()
    {
        RuleFor(v => v.CustomerName)
            .MaximumLength(200)
            .NotEmpty();

        // Remove the TotalAmount rule and add this:
        RuleFor(v => v.Items)
            .NotEmpty()
            .WithMessage("The order must contain at least one item.");

        // Optional: Validate individual items within the list
        RuleForEach(v => v.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Product).NotEmpty();
            item.RuleFor(x => x.Quantity).GreaterThan(0);
            item.RuleFor(x => x.Price).GreaterThan(0);
        });
    }
}