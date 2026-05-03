using MediatR;

namespace InteractHub.Api.Events;

// INotification là interface của MediatR đánh dấu đây là một Event
public record FriendRequestSentEvent(Guid RequesterId, Guid TargetUserId) : INotification;