using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using nekohub_core.Models;

namespace nekohub_core.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly nekohub_core.Data.IPostRepository _repository;

    public PostsController(nekohub_core.Data.IPostRepository repository)
    {
        _repository = repository;
    }

    // GET api/posts - 获取所有文章
    [HttpGet]
    public ActionResult<IEnumerable<Post>> GetAllPosts([FromQuery] bool? isPublished, [FromQuery] string? keyword, [FromQuery] string? sortBy, [FromQuery] string? sortOrder = "desc")
    {
        IEnumerable<Post> postsSnapshot = _repository.GetAllSnapshot();

        postsSnapshot = FilterPosts(postsSnapshot, isPublished, keyword);
        postsSnapshot = SortPosts(postsSnapshot, sortBy, sortOrder);

        var result = postsSnapshot.ToList();
        return Ok(result);
    }

    // GET api/posts/paged - 分页获取文章
    [HttpGet("paged")]
    public ActionResult<PagedResponse<Post>> GetPagedPosts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool? isPublished = null,
        [FromQuery] string? keyword = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = "desc")
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        IEnumerable<Post> postsSnapshot = _repository.GetAllSnapshot();

        postsSnapshot = FilterPosts(postsSnapshot, isPublished, keyword);
        postsSnapshot = SortPosts(postsSnapshot, sortBy, sortOrder);

        var total = postsSnapshot.Count();
        var items = postsSnapshot
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var response = new PagedResponse<Post>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return Ok(response);
    }

    // GET api/posts/{id} - 根据ID获取文章
    [HttpGet("{id}")]
    public ActionResult<Post> GetPost(int id)
    {
        var post = _repository.GetSnapshot(id);

        if (post == null)
        {
            return NotFound($"文章 ID {id} 未找到");
        }

        return Ok(post);
    }

    // POST api/posts - 创建新文章
    [HttpPost]
    public ActionResult<Post> CreatePost([FromBody] Post post)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        NormalizePost(post);

        var validationError = ValidateTrimmedContent(post);
        if (validationError is not null)
        {
            return validationError;
        }

        post.CreatedAt = DateTime.UtcNow;
        post.UpdatedAt = post.CreatedAt;

        var created = _repository.Add(post);

        return CreatedAtAction(nameof(GetPost), new { id = created.Id }, created);
    }

    // PUT api/posts/{id} - 更新文章
    [HttpPut("{id}")]
    public ActionResult<Post> UpdatePost(int id, [FromBody] Post updatedPost)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (updatedPost.Id != 0 && updatedPost.Id != id)
        {
            return BadRequest("请求正文中的文章 ID 与路径参数不一致");
        }

        NormalizePost(updatedPost);

        var validationError = ValidateTrimmedContent(updatedPost);
        if (validationError is not null)
        {
            return validationError;
        }

        var updated = _repository.Update(id, p =>
        {
            p.Title = updatedPost.Title;
            p.Content = updatedPost.Content;
            p.IsPublished = updatedPost.IsPublished;
            p.UpdatedAt = DateTime.UtcNow;
        });
        if (updated is null)
        {
            return NotFound($"文章 ID {id} 未找到");
        }
        return Ok(updated);
    }

    // PATCH api/posts/{id} - 部分更新文章
    [HttpPatch("{id}")]
    public ActionResult<Post> PatchPost(int id, [FromBody] PostPatchRequest changes)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        string? newTitle = null;
        string? newContent = null;

        if (changes.Title is not null)
        {
            newTitle = changes.Title.Trim();
            if (string.IsNullOrWhiteSpace(newTitle) || newTitle.Length > 200)
            {
                return ValidationFromDictionary(new Dictionary<string, string[]>
                {
                    [nameof(changes.Title)] = new[] { "文章标题不能为空且长度不能超过 200 个字符" }
                });
            }
        }

        if (changes.Content is not null)
        {
            newContent = changes.Content.Trim();
            if (string.IsNullOrWhiteSpace(newContent) || newContent.Length < 10)
            {
                return ValidationFromDictionary(new Dictionary<string, string[]>
                {
                    [nameof(changes.Content)] = new[] { "文章内容不能为空且至少需要 10 个字符" }
                });
            }
        }

        var updated = _repository.Update(id, p =>
        {
            if (newTitle is not null)
            {
                p.Title = newTitle;
            }
            if (newContent is not null)
            {
                p.Content = newContent;
            }
            if (changes.IsPublished.HasValue)
            {
                p.IsPublished = changes.IsPublished.Value;
            }
            p.UpdatedAt = DateTime.UtcNow;
        });

        if (updated is null)
        {
            return NotFound($"文章 ID {id} 未找到");
        }

        return Ok(updated);
    }

    // DELETE api/posts/{id} - 删除文章
    [HttpDelete("{id}")]
    public ActionResult DeletePost(int id)
    {
        var existing = _repository.GetSnapshot(id);
        if (existing == null)
        {
            return NotFound($"文章 ID {id} 未找到");
        }

        _repository.Delete(id);
        return Ok($"文章 '{existing.Title}' 已删除");
    }

    // POST api/posts/{id}/publish - 发布文章
    [HttpPost("{id}/publish")]
    public ActionResult<Post> PublishPost(int id)
    {
        var updated = _repository.Update(id, p =>
        {
            if (!p.IsPublished)
            {
                p.IsPublished = true;
                p.UpdatedAt = DateTime.UtcNow;
            }
        });
        if (updated is null)
        {
            return NotFound($"文章 ID {id} 未找到");
        }
        return Ok(updated);
    }

    // POST api/posts/{id}/unpublish - 取消发布文章
    [HttpPost("{id}/unpublish")]
    public ActionResult<Post> UnpublishPost(int id)
    {
        var updated = _repository.Update(id, p =>
        {
            if (p.IsPublished)
            {
                p.IsPublished = false;
                p.UpdatedAt = DateTime.UtcNow;
            }
        });
        if (updated is null)
        {
            return NotFound($"文章 ID {id} 未找到");
        }
        return Ok(updated);
    }

    // GET api/posts/stats - 获取文章统计
    [HttpGet("stats")]
    public ActionResult<PostStats> GetStats()
    {
        var (total, published, drafts) = _repository.GetStats();
        return Ok(new PostStats { Total = total, Published = published, Drafts = drafts });
    }

    private static IEnumerable<Post> FilterPosts(IEnumerable<Post> posts, bool? isPublished, string? keyword)
    {
        if (isPublished.HasValue)
        {
            posts = posts.Where(p => p.IsPublished == isPublished.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var trimmedKeyword = keyword.Trim();
            posts = posts.Where(p =>
                p.Title.Contains(trimmedKeyword, StringComparison.OrdinalIgnoreCase) ||
                p.Content.Contains(trimmedKeyword, StringComparison.OrdinalIgnoreCase));
        }

        return posts;
    }

    private static IEnumerable<Post> SortPosts(IEnumerable<Post> posts, string? sortBy, string? sortOrder)
    {
        var sortField = string.IsNullOrWhiteSpace(sortBy) ? "createdat" : sortBy.Trim().ToLowerInvariant();
        var descending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

        return sortField switch
        {
            "title" => descending
                ? posts.OrderByDescending(p => p.Title)
                : posts.OrderBy(p => p.Title),
            "updatedat" => descending
                ? posts.OrderByDescending(p => p.UpdatedAt)
                : posts.OrderBy(p => p.UpdatedAt),
            "createdat" or _ => descending
                ? posts.OrderByDescending(p => p.CreatedAt)
                : posts.OrderBy(p => p.CreatedAt)
        };
    }

    private ActionResult? ValidateTrimmedContent(Post post)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(post.Title))
        {
            errors[nameof(post.Title)] = new[] { "文章标题不能为空或全是空白字符" };
        }
        else if (post.Title.Length > 200)
        {
            errors[nameof(post.Title)] = new[] { "文章标题长度不能超过 200 个字符" };
        }

        if (string.IsNullOrWhiteSpace(post.Content))
        {
            errors[nameof(post.Content)] = new[] { "文章内容不能为空或全是空白字符" };
        }
        else if (post.Content.Length < 10)
        {
            errors[nameof(post.Content)] = new[] { "文章内容至少需要 10 个字符" };
        }

        return errors.Count > 0 ? ValidationFromDictionary(errors) : null;
    }

    private ActionResult ValidationFromDictionary(IDictionary<string, string[]> errors)
    {
        var modelState = new ModelStateDictionary();
        foreach (var kv in errors)
        {
            foreach (var message in kv.Value)
            {
                modelState.AddModelError(kv.Key, message);
            }
        }
        return ValidationProblem(modelState);
    }

    private static void NormalizePost(Post post)
    {
        post.Title = post.Title.Trim();
        post.Content = post.Content.Trim();
    }
}
