using BlogsApi.Data;
using BlogsApi.Dtos;
using BlogsApi.Mappings;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BlogsApi.Services;

public class PostService : IPostService
{
    private readonly AppDbContext _dbContext;
    private readonly IValidator<UpdatePostRequest> _validator;
    private readonly IValidator<CreatePostRequest> _createPostRequestValidator;
    private readonly ILogger<PostService> _logger;

    private readonly PostMapper _postMapper = new();
    public PostService(AppDbContext dbContext,
        IValidator<UpdatePostRequest> validator,
        IValidator<CreatePostRequest> createPostRequestValidator,
        

        ILogger<PostService> logger)
    {
        _dbContext = dbContext;
        _validator = validator;
        _createPostRequestValidator = createPostRequestValidator;      
        _logger = logger;
    }
    public async Task<PostResponse> CreateAsync(CreatePostRequest post)
    {
        if (post == null)
        {
            _logger.LogError("Post is null");
            throw new ArgumentNullException(nameof(post));
        }
        var validationResult = _createPostRequestValidator.Validate(post);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(x => x.ErrorMessage);
            throw new FluentValidation.ValidationException(string.Join(", ", errors));
        }

        // Using Mapperly to map CreatePostRequest to Post
        var newPost = _postMapper.ToPost(post);
        await _dbContext.Posts.AddAsync(newPost);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Post created successfully");

        // Map the post to PostResponse
        var postResponse = _postMapper.ToPostResponse(newPost);
        return postResponse;
    }

    public Task DeleteAsync(int id)
    {
        var post = _dbContext.Posts.FirstOrDefault(p => p.Id == id);
        if (post is null)
        {
            _logger.LogError("Post not found");
            throw new ApplicationException("Post not found");
        }
        _dbContext.Posts.Remove(post);
        _dbContext.SaveChanges();
        _logger.LogInformation("Post deleted");
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<PostResponse>> GetAllAsync()
    {
        var posts = await _dbContext.Posts.AsNoTracking()
                                      .Include(x => x.Category)
                                      .ToListAsync();

        // Map the response to the PostResponse using Mapperly
        var response = posts.Select(p => _postMapper.ToPostResponse(p));

        return response;
    }

    public async Task<PostResponse> GetByIdAsync(int id)
    {
        var post = await _dbContext.Posts
                        .Include(c => c.Category)
                        .FirstOrDefaultAsync(p => p.Id == id);
        if (post is null)
        {
            _logger.LogError("Post not found");
            throw new ApplicationException("Post not found");
        }
        // Map the post to PostResponse using Mapperly
        var response = _postMapper.ToPostResponse(post);
        return response;
    }

    public async Task<PostResponse> UpdateAsync(UpdatePostRequest post)
    {
        if (post == null)
        {
            _logger.LogError("Post is null");
            throw new ArgumentNullException(nameof(post));
        }

        var validationResult = _validator.Validate(post);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(x => x.ErrorMessage);
            throw new FluentValidation.ValidationException(string.Join(", ", errors));
        }

        //update the existing post
        var existingPost = await _dbContext.Posts.FirstOrDefaultAsync(p => p.Id == post.Id);
        if (existingPost is null)
        {
            _logger.LogError("Post not found");
            throw new ApplicationException("Post not found");
        }
        // use Mapperly to map the post to the existing post
        _postMapper.UpdatePost(post, existingPost);
        existingPost.PublishedOn = DateTime.UtcNow;


        _dbContext.Posts.Update(existingPost);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Post updated");

        var response = _postMapper.ToPostResponse(existingPost);
        return response;
    }
}
