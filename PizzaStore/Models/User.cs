using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace PizzaStore.Models;

public record User(string Username, string Password)
{
    [Key]
    [Required]
    [MaxLength(255)]
    public string Username { get; init; } = Username;
    [MaxLength(255)]
    public string Password { get; init; } = Password;
}

internal class UserDb(DbContextOptions<UserDb> options) : DbContext(options)
{
    public DbSet<User> Users { get; init; } = null!;
}