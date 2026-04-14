using FluentValidation;
using MediatR;

namespace OrderingSystem.Application.Abstractions.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest
{
    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);

        var validationFailures = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = validationFailures
            .Where(f => !f.IsValid)
            .SelectMany(f => f.Errors)
            .ToList();

        if (errors.Count != 0)
        {
            throw new ValidationException(errors);
        }

        return await next();
    }
}