namespace MinimalRazor.Models;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime? DateCreated { get; set; }
    public DateTime? DateUpdated { get; set; }
    public string Detail { get; set; } = "";
    public int UserId { get; set; }
    public User? User { get; set; }
}