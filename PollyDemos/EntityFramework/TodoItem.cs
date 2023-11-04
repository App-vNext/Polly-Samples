using System.ComponentModel.DataAnnotations;

namespace PollyDemos.EntityFramework;

public class TodoItem
{
    public int Id { get; set; }

    [Required]
    public string? Text { get; set; }
}
