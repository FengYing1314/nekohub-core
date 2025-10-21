using System.ComponentModel.DataAnnotations;

namespace nekohub_core.Models;

public class PostPatchRequest
{
    [StringLength(200)]
    public string? Title { get; set; }

    [MinLength(10)]
    public string? Content { get; set; }

    public bool? IsPublished { get; set; }
}
