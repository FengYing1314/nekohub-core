using System.ComponentModel.DataAnnotations;

namespace nekohub_core.Models;

// 博客文章模型 - 定义文章的基本属性
public class Post
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MinLength(10)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsPublished { get; set; }
}
