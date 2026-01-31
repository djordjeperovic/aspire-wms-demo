using AspireWms.Api.Shared.Infrastructure.Behaviors;
using FluentValidation;
using MediatR;

namespace AspireWms.Api.Shared.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers MediatR, FluentValidation, and pipeline behaviors.
    /// </summary>
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // Register MediatR with all handlers in assembly
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // Register all FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
