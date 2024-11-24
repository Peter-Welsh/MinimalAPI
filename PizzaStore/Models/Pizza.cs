using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace PizzaStore.Models;

public record Pizza(string Name, string Description)
{
    public int Id { get; init; }
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = Name;
    [MaxLength(255)]
    public string Description { get; set; } = Description;
}

internal class PizzaDb(DbContextOptions<PizzaDb> options) : DbContext(options)
{
    public DbSet<Pizza> Pizzas { get; init; } = null!;
}