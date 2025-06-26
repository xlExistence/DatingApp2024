namespace API.Data;

using API.DataEntities;
using API.DTOs;
using API.Helpers;

public interface ILikesRepository
{
    public void AddLike(UserLike like);
    public Task<IEnumerable<int>> GetCurrentUserLikeIdsAsync(int currentUSerId);
    public Task<UserLike?> GetUserLikeAsync(int sourceUserId, int targerUserId);
    public Task<PagedList<MemberResponse>> GetUserLikesAsync(LikesParams likesParams);
    public void RemoveLike(UserLike userLike);
}
