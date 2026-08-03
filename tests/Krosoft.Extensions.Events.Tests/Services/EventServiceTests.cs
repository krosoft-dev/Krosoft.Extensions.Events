using Krosoft.Extensions.Events.Interfaces;
using Krosoft.Extensions.Events.Services;
using Krosoft.Extensions.Events.Tests.Core;
using Krosoft.Extensions.Jobs.Interfaces;
using Krosoft.Extensions.Testing;
using MediatR;

namespace Krosoft.Extensions.Events.Tests.Services;

[TestClass]
public class EventServiceTests : BaseTest
{
    private Func<IMediator, Task>? _actionFireForget;
    private Mock<IFireForgetService> _fireForgetServiceMock = null!;
    private Mock<IMediator> _mediatorMock = null!;
    private IEventService _eventService = null!;

    [TestInitialize]
    public void SetUp()
    {
        _actionFireForget = null;
        _mediatorMock = new Mock<IMediator>();
        _fireForgetServiceMock = new Mock<IFireForgetService>();
        _fireForgetServiceMock.Setup(x => x.FireAsync(It.IsAny<Func<IMediator, Task>>()))
                              .Callback<Func<IMediator, Task>>(action => _actionFireForget = action);

        _eventService = new EventService(_fireForgetServiceMock.Object);
    }

    [TestMethod]
    public void Publish_Notification_DelegueAuFireForgetService()
    {
        _eventService.Publish(new SampleNotification("hello"), CancellationToken.None);

        _fireForgetServiceMock.Verify(x => x.FireAsync(It.IsAny<Func<IMediator, Task>>()), Times.Once);
        _fireForgetServiceMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public void Publish_Notification_NePublieRienDeManiereSynchrone()
    {
        _eventService.Publish(new SampleNotification("hello"), CancellationToken.None);

        // La publication est differee : elle n'a lieu qu'a l'execution de l'action fire-and-forget.
        _mediatorMock.Verify(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Publish_ActionFireForgetExecutee_PublieLaNotificationSurLeMediator()
    {
        var notification = new SampleNotification("hello");

        _eventService.Publish(notification, CancellationToken.None);

        Check.That(_actionFireForget).IsNotNull();
        await _actionFireForget!(_mediatorMock.Object);

        _mediatorMock.Verify(x => x.Publish<INotification>(notification, CancellationToken.None), Times.Once);
        _mediatorMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Publish_ActionFireForgetExecutee_PropageLeCancellationToken()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _eventService.Publish(new SampleNotification("hello"), cancellationToken);

        Check.That(_actionFireForget).IsNotNull();
        await _actionFireForget!(_mediatorMock.Object);

        _mediatorMock.Verify(x => x.Publish(It.IsAny<INotification>(),
                                            It.Is<CancellationToken>(t => t == cancellationToken)),
                             Times.Once);
    }

    [TestMethod]
    public async Task Publish_PlusieursNotifications_ChaqueNotificationEstPubliee()
    {
        var actions = new List<Func<IMediator, Task>>();
        _fireForgetServiceMock.Setup(x => x.FireAsync(It.IsAny<Func<IMediator, Task>>()))
                              .Callback<Func<IMediator, Task>>(action => actions.Add(action));

        var premiere = new SampleNotification("premiere");
        var seconde = new SampleNotification("seconde");

        _eventService.Publish(premiere, CancellationToken.None);
        _eventService.Publish(seconde, CancellationToken.None);

        Check.That(actions).HasSize(2);
        foreach (var action in actions)
        {
            await action(_mediatorMock.Object);
        }

        _mediatorMock.Verify(x => x.Publish<INotification>(premiere, CancellationToken.None), Times.Once);
        _mediatorMock.Verify(x => x.Publish<INotification>(seconde, CancellationToken.None), Times.Once);
    }

    [TestMethod]
    public void Publish_MediatorEnErreur_RemonteLExceptionDansLActionFireForget()
    {
        _mediatorMock.Setup(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new InvalidOperationException("Boom"));

        _eventService.Publish(new SampleNotification("hello"), CancellationToken.None);

        Check.That(_actionFireForget).IsNotNull();
        // L'exception n'est pas avalee par le service : c'est le fire-and-forget qui la loggue.
        Check.ThatCode(async () => await _actionFireForget!(_mediatorMock.Object))
             .Throws<InvalidOperationException>()
             .WithMessage("Boom");
    }
}
