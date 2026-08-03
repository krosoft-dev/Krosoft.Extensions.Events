using Krosoft.Extensions.Core.Models;
using Krosoft.Extensions.Events.Identity.Interfaces;
using Krosoft.Extensions.Events.Identity.Services;
using Krosoft.Extensions.Events.Identity.Tests.Core;
using Krosoft.Extensions.Identity.Abstractions.Interfaces;
using Krosoft.Extensions.Jobs.Interfaces;
using Krosoft.Extensions.Testing;
using MediatR;

namespace Krosoft.Extensions.Events.Identity.Tests.Services;

[TestClass]
public class TokenEventServiceTests : BaseTest
{
    private Func<IMediator, Task>? _actionFireForget;
    private Mock<IFireForgetService> _fireForgetServiceMock = null!;
    private Mock<IMediator> _mediatorMock = null!;
    private KrosoftToken _krosoftToken = null!;
    private Mock<IKrosoftTokenBuilderService> _tokenBuilderServiceMock = null!;
    private ITokenEventService _tokenEventService = null!;

    [TestInitialize]
    public void SetUp()
    {
        _actionFireForget = null;
        _mediatorMock = new Mock<IMediator>();

        _krosoftToken = new KrosoftToken
        {
            Id = "42",
            Email = "kevin@krosoft.fr"
        };
        _tokenBuilderServiceMock = new Mock<IKrosoftTokenBuilderService>();
        _tokenBuilderServiceMock.Setup(x => x.Build())
                                .Returns(_krosoftToken);

        _fireForgetServiceMock = new Mock<IFireForgetService>();
        _fireForgetServiceMock.Setup(x => x.FireAsync(It.IsAny<Func<IMediator, Task>>()))
                              .Callback<Func<IMediator, Task>>(action => _actionFireForget = action);

        _tokenEventService = new TokenEventService(_fireForgetServiceMock.Object, _tokenBuilderServiceMock.Object);
    }

    [TestMethod]
    public void Publish_AvecFabriqueDeNotification_DelegueAuFireForgetService()
    {
        _tokenEventService.Publish(token => new SampleTokenNotification(token), CancellationToken.None);

        _fireForgetServiceMock.Verify(x => x.FireAsync(It.IsAny<Func<IMediator, Task>>()), Times.Once);
        _fireForgetServiceMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public void Publish_AvecFabriqueDeNotification_NeConstruitPasLeTokenDeManiereSynchrone()
    {
        _tokenEventService.Publish(token => new SampleTokenNotification(token), CancellationToken.None);

        // Le token est construit dans le scope du fire-and-forget, pas au moment de l'appel.
        _tokenBuilderServiceMock.Verify(x => x.Build(), Times.Never);
    }

    [TestMethod]
    public async Task Publish_ActionFireForgetExecutee_ConstruitLeTokenEtPublieLaNotification()
    {
        _tokenEventService.Publish(token => new SampleTokenNotification(token), CancellationToken.None);

        Check.That(_actionFireForget).IsNotNull();
        await _actionFireForget!(_mediatorMock.Object);

        _tokenBuilderServiceMock.Verify(x => x.Build(), Times.Once);
        _mediatorMock.Verify(x => x.Publish(It.Is<INotification>(n => ReferenceEquals(((SampleTokenNotification)n).Token, _krosoftToken)),
                                            CancellationToken.None),
                             Times.Once);
        _mediatorMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Publish_ActionFireForgetExecutee_PropageLeCancellationToken()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _tokenEventService.Publish(token => new SampleTokenNotification(token), cancellationToken);

        Check.That(_actionFireForget).IsNotNull();
        await _actionFireForget!(_mediatorMock.Object);

        _mediatorMock.Verify(x => x.Publish(It.IsAny<INotification>(),
                                            It.Is<CancellationToken>(t => t == cancellationToken)),
                             Times.Once);
    }

    [TestMethod]
    public async Task Publish_Notification_HeriteDuComportementDeEventService()
    {
        var notification = new SampleTokenNotification(new KrosoftToken());

        // Surcharge heritee de EventService : aucun token n'est construit.
        _tokenEventService.Publish(notification, CancellationToken.None);

        Check.That(_actionFireForget).IsNotNull();
        await _actionFireForget!(_mediatorMock.Object);

        _tokenBuilderServiceMock.Verify(x => x.Build(), Times.Never);
        _mediatorMock.Verify(x => x.Publish<INotification>(notification, CancellationToken.None), Times.Once);
    }

    [TestMethod]
    public void Publish_TokenBuilderEnErreur_AucuneNotificationPubliee()
    {
        _tokenBuilderServiceMock.Setup(x => x.Build())
                                .Throws(new InvalidOperationException("Aucun token disponible."));

        _tokenEventService.Publish(token => new SampleTokenNotification(token), CancellationToken.None);

        Check.That(_actionFireForget).IsNotNull();
        var action = _actionFireForget!;
        Check.ThatCode(async () => await action(_mediatorMock.Object))
             .Throws<InvalidOperationException>()
             .WithMessage("Aucun token disponible.");

        _mediatorMock.Verify(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
