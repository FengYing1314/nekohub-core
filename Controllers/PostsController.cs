using Microsoft.AspNetCore.Mvc;
using nekohub_core.Models;

namespace nekohub_core.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    // 临时使用内存数据，模拟数据库
    private static readonly List<Post> Posts = new()
    {
        new Post { Id = 1, Title = "第一篇博客", Content = "这是第一篇博客的内容", CreatedAt = DateTime.Now, IsPublished = true },
        new Post { Id = 2, Title = "第二篇博客", Content = "这是第二篇博客的内容", CreatedAt = DateTime.Now, IsPublished = false }
    };

    // GET api/posts - 获取所有文章
    [HttpGet]
    public ActionResult<IEnumerable<Post>> GetAllPosts()
    {
        return Ok(Posts);
    }

    // GET api/posts/{id} - 根据ID获取文章
    [HttpGet("{id}")]
    public ActionResult<Post> GetPost(int id)
    {
        var post = Posts.FirstOrDefault(p => p.Id == id);
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
        // 自动分配ID
        post.Id = Posts.Count > 0 ? Posts.Max(p => p.Id) + 1 : 1;
        post.CreatedAt = DateTime.Now;

        Posts.Add(post);
        return CreatedAtAction(nameof(GetPost), new { id = post.Id }, post);
    }

    // PUT api/posts/{id} - 更新文章
    [HttpPut("{id}")]
    public ActionResult<Post> UpdatePost(int id, [FromBody] Post updatedPost)
    {
        var post = Posts.FirstOrDefault(p => p.Id == id);
        if (post == null)
        {
            return NotFound($"文章 ID {id} 未找到");
        }

        post.Title = updatedPost.Title;
        post.Content = updatedPost.Content;
        post.IsPublished = updatedPost.IsPublished;

        return Ok(post);
    }

    // DELETE api/posts/{id} - 删除文章
    [HttpDelete("{id}")]
    public ActionResult DeletePost(int id)
    {
        var post = Posts.FirstOrDefault(p => p.Id == id);
        if (post == null)
        {
            return NotFound($"文章 ID {id} 未找到");
        }

        Posts.Remove(post);
        return Ok($"文章 '{post.Title}' 已删除");
    }
}
