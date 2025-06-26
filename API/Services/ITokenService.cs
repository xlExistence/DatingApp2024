namespace API.Services;
using API.DataEntities;

public interface ITokenService
{
    public Task<string> CreateToken(AppUser user);
}
