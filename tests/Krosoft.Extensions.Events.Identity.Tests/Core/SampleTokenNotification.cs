using Krosoft.Extensions.Core.Models;
using MediatR;

namespace Krosoft.Extensions.Events.Identity.Tests.Core;

public record SampleTokenNotification(KrosoftToken Token) : INotification;
