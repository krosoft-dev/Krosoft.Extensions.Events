using MediatR;

namespace Krosoft.Extensions.Events.Tests.Core;

public record SampleNotification(string Libelle) : INotification;
