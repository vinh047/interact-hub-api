using InteractHub.Api.Data;
using InteractHub.Api.Entities;
using InteractHub.Api.Enums;
using InteractHub.Api.Events;
using InteractHub.Api.Hubs;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace InteractHub.Api.Handlers;

public class FriendRequestSentEventHandler(
    ApplicationDbContext context, 
    IHubContext<NotificationHub> hubContext) 
    : INotificationHandler<FriendRequestSentEvent>
{
    public async Task Handle(FriendRequestSentEvent notificationEvent, CancellationToken cancellationToken)
    {
        // 1. Ghi vào Database
        var notif = new Notification 
        {
            UserId = notificationEvent.TargetUserId,
            IssuerId = notificationEvent.RequesterId,
            Type = NotificationType.FriendRequest,
            Content = "Bạn có 1 lời mời kết bạn mới.",
            ReferenceId = notificationEvent.RequesterId,
            CreatedAt = DateTime.UtcNow
        };
        
        context.Notifications.Add(notif);
        await context.SaveChangesAsync(cancellationToken);

        // 2. Bắn SignalR realtime
        await hubContext.Clients.User(notificationEvent.TargetUserId.ToString())
            .SendAsync("ReceiveNotification", notif, cancellationToken);
    }
}