namespace API.Helpers;

using System.Threading.Tasks;
using API.Data;
using API.Extensions;
using API.UnitOfWork;
using Microsoft.AspNetCore.Mvc.Filters;

public class LogUserActivity : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var resultContext = await next();

        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var userId = resultContext.HttpContext.User.GetUserId();
        var unitOfWork = resultContext.HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
        var user = await unitOfWork.UserRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return;
        }

        user.LastActive = DateTime.UtcNow;

        await unitOfWork.Complete();
    }
}
