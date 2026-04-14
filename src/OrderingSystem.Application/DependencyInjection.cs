using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OrderingSystem.Application.Abstractions.Behaviors;

namespace OrderingSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);

            // Add this line to register the behavior
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        return services;
    }
}