using InteractHub.Api.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace InteractHub.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public abstract class BaseController : ControllerBase
{
    // Tạo một Property dùng chung cho tất cả Controller con
    protected Guid CurrentUserId
    {
        get
        {
            var userId = User.GetUserId(); 
            
            if (userId == Guid.Empty)
            {
                throw new UnauthorizedAccessException("User identity not found in token.");
            }
            
            return userId;
        }
    }
}