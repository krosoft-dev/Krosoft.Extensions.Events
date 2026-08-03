using Krosoft.Extensions.Events.Extensions;
using Krosoft.Extensions.Events.Interfaces;
using Krosoft.Extensions.Events.Services;
using Krosoft.Extensions.Jobs.Interfaces;
using Krosoft.Extensions.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Krosoft.Extensions.Events.Tests.Extensions;

[TestClass]
public class ServiceCollectionExtensionsTests : BaseTest
{
    protected override void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
                .AddEvents();
    }

    [TestMethod]
    public void AddEvents_EventServiceResolvableDepuisLeConteneur()
    {
        using var serviceProvider = CreateServiceCollection();

        var eventService = serviceProvider.GetRequiredService<IEventService>();

        Check.That(eventService).IsNotNull();
        Check.That(eventService).IsInstanceOf<EventService>();
    }

    [TestMethod]
    public void AddEvents_FireForgetServiceEgalementEnregistre()
    {
        using var serviceProvider = CreateServiceCollection();

        var fireForgetService = serviceProvider.GetRequiredService<IFireForgetService>();

        Check.That(fireForgetService).IsNotNull();
    }

    [TestMethod]
    public void AddEvents_EventServiceEnregistreEnTransient()
    {
        var services = new ServiceCollection();

        services.AddEvents();

        var descriptors = services.Where(d => d.ServiceType == typeof(IEventService)).ToList();
        Check.That(descriptors).HasSize(1);
        Check.That(descriptors[0].Lifetime).IsEqualTo(ServiceLifetime.Transient);
        Check.That(descriptors[0].ImplementationType).IsEqualTo(typeof(EventService));
    }

    [TestMethod]
    public void AddEvents_DeuxResolutions_RetournentDesInstancesDifferentes()
    {
        using var serviceProvider = CreateServiceCollection();

        var premiere = serviceProvider.GetRequiredService<IEventService>();
        var seconde = serviceProvider.GetRequiredService<IEventService>();

        Check.That(premiere).Not.IsSameReferenceAs(seconde);
    }

    [TestMethod]
    public void AddEvents_RetourneLaMemeCollectionDeServices()
    {
        var services = new ServiceCollection();

        var resultat = services.AddEvents();

        Check.That(resultat).IsSameReferenceAs(services);
    }
}
