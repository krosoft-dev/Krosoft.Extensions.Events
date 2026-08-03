using Krosoft.Extensions.Core.Models;
using Krosoft.Extensions.Events.Identity.Extensions;
using Krosoft.Extensions.Events.Identity.Interfaces;
using Krosoft.Extensions.Events.Identity.Services;
using Krosoft.Extensions.Events.Interfaces;
using Krosoft.Extensions.Identity.Abstractions.Interfaces;
using Krosoft.Extensions.Jobs.Interfaces;
using Krosoft.Extensions.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Krosoft.Extensions.Events.Identity.Tests.Extensions;

[TestClass]
public class ServiceCollectionExtensionsTests : BaseTest
{
    protected override void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        var tokenBuilderServiceMock = new Mock<IKrosoftTokenBuilderService>();
        tokenBuilderServiceMock.Setup(x => x.Build())
                               .Returns(new KrosoftToken());

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
                .AddTransient(_ => tokenBuilderServiceMock.Object)
                .AddTokenEvents();
    }

    [TestMethod]
    public void AddTokenEvents_TokenEventServiceResolvableDepuisLeConteneur()
    {
        using var serviceProvider = CreateServiceCollection();

        var tokenEventService = serviceProvider.GetRequiredService<ITokenEventService>();

        Check.That(tokenEventService).IsNotNull();
        Check.That(tokenEventService).IsInstanceOf<TokenEventService>();
    }

    [TestMethod]
    public void AddTokenEvents_EnregistreEgalementLesServicesDuPackageEvents()
    {
        using var serviceProvider = CreateServiceCollection();

        Check.That(serviceProvider.GetRequiredService<IEventService>()).IsNotNull();
        Check.That(serviceProvider.GetRequiredService<IFireForgetService>()).IsNotNull();
    }

    [TestMethod]
    public void AddTokenEvents_TokenEventServiceEnregistreEnTransient()
    {
        var services = new ServiceCollection();

        services.AddTokenEvents();

        var descriptors = services.Where(d => d.ServiceType == typeof(ITokenEventService)).ToList();
        Check.That(descriptors).HasSize(1);
        Check.That(descriptors[0].Lifetime).IsEqualTo(ServiceLifetime.Transient);
        Check.That(descriptors[0].ImplementationType).IsEqualTo(typeof(TokenEventService));
    }

    [TestMethod]
    public void AddTokenEvents_DeuxResolutions_RetournentDesInstancesDifferentes()
    {
        using var serviceProvider = CreateServiceCollection();

        var premiere = serviceProvider.GetRequiredService<ITokenEventService>();
        var seconde = serviceProvider.GetRequiredService<ITokenEventService>();

        Check.That(premiere).Not.IsSameReferenceAs(seconde);
    }

    [TestMethod]
    public void AddTokenEvents_RetourneLaMemeCollectionDeServices()
    {
        var services = new ServiceCollection();

        var resultat = services.AddTokenEvents();

        Check.That(resultat).IsSameReferenceAs(services);
    }
}
