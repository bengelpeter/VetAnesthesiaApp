using SQLite;

namespace VetAnesthesiaApp.Models;

public class Animal
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "";
    public string Species { get; set; } = "";
    public decimal? Weight { get; set; }

    public string? OwnerName { get; set; }
}