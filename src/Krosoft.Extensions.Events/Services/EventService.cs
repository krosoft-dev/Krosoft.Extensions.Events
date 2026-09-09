using Krosoft.Extensions.Events.Interfaces;
using Krosoft.Extensions.Jobs.Interfaces;
using MediatR;

namespace Krosoft.Extensions.Events.Services;

public class EventService : IEventService
{
    private readonly IFireForgetService _fireForgetService;

    public EventService(IFireForgetService fireForgetService)
    {
        _fireForgetService = fireForgetService;
    }

    public void Publish(INotification notification, CancellationToken cancellationToken)
    {
        // Fire-and-forget : le travail survit a la requete appelante. On ne propage donc pas
        // son CancellationToken, sinon il est annule des la fin de la requete (OperationCanceledException).
        _fireForgetService.FireAsync<IMediator>(async mediator => { await mediator.Publish(notification, CancellationToken.None); });
    }
}