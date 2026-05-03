using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace InteractHub.Api.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    // Việc "bắn" thông báo sẽ do các Service thực hiện.
}