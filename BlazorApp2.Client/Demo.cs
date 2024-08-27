using System.ComponentModel.DataAnnotations;

namespace BlazorApp2.Models;

public class ToDoItem
{
    public string? Title { get; set; }
    public bool IsDone { get; set; } = false;
}
