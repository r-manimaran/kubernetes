using BlogsApi.Dtos;

namespace BlogsApi.Services;

public interface IPostService
{
    Task<IEnumerable<PostResponse>> GetAllAsync();
    Task<PostResponse> GetByIdAsync(int id);
    Task<PostResponse> CreateAsync(CreatePostRequest post);
    Task<PostResponse> UpdateAsync(UpdatePostRequest post);
    Task DeleteAsync(int id);
}
