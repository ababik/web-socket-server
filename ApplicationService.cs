using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class ApplicationService
{
    public static ApplicationService Create(IServiceProvider provider)
    {
        var logger = provider.GetService<ILogger<ApplicationService>>();
        return new ApplicationService(logger);
    }

    private ILogger Logger { get; }

    public ApplicationService(ILogger logger)
    {
        Logger = logger;
    }
}