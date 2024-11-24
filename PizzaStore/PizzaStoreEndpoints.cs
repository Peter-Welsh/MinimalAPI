using Microsoft.EntityFrameworkCore;
using PizzaStore.Models;

namespace PizzaStore;

public abstract class PizzaStoreEndpoints
{
    public static void Map(WebApplication app, IConfigurationRoot config)
    {
        MapAuthenticationEndpoints(app, config);
        
        MapUserManagementEndpoints(app);

        MapPizzaEndpoints(app);
    }

    private static void MapPizzaEndpoints(WebApplication app)
    {
        app.MapGet("/pizza/{id:int}", async (PizzaDb db, int id) => 
                await db.Pizzas.FindAsync(id) is { } pizza
                    ? Results.Ok(pizza)
                    : Results.NotFound())
            .Produces<Pizza>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        app.MapGet("/pizzas", async (PizzaDb db) => await db.Pizzas.ToListAsync())
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        app.MapPost("/pizza", async (PizzaDb db, Pizza pizza) =>
            {
                if (pizza.Id != 0) return Results.BadRequest("Explicit IDs are not allowed. Remove the ID from the request body and try again.");
                await db.Pizzas.AddAsync(pizza);
                await db.SaveChangesAsync();
                return Results.Created($"/pizza/{pizza.Id}", pizza);
            })
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        app.MapPut("/pizza/{id:int}", async (PizzaDb db, Pizza updatedPizza, int id) =>
            {
                var pizza = await db.Pizzas.FindAsync(id);
                if (pizza is null) return Results.NotFound();
                pizza.Name = updatedPizza.Name;
                pizza.Description = updatedPizza.Description;
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        app.MapDelete("/pizza/{id:int}", async (PizzaDb db, int id) =>
            {
                var pizza = await db.Pizzas.FindAsync(id);
                if (pizza is null) return Results.NotFound();
                db.Pizzas.Remove(pizza);
                await db.SaveChangesAsync();
                return Results.Ok();
            })
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }

    private static void MapAuthenticationEndpoints(WebApplication app, IConfigurationRoot config)
    {
        app.MapPost("/login", async (UserDb db, User user) =>
            {
                if (app.Environment.IsDevelopment() && user is { Username: "admin", Password: "admin" })
                    return JwtTokenBuilder.Build(config);
                return await db.Users.FindAsync(user.Username) is null
                    ? string.Empty
                    : JwtTokenBuilder.Build(config);
            })
            .Produces<string>()
            .WithTags("Authentication")
            .AllowAnonymous();
    }

    private static void MapUserManagementEndpoints(WebApplication app)
    {
        app.MapPost("/user", async (UserDb db, User user) =>
            {
                if (await db.Users.AnyAsync(u => u.Username == user.Username))
                    return Results.Conflict("This username is taken.");
                await db.Users.AddAsync(user);
                await db.SaveChangesAsync();
                return Results.Created();
            })
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status409Conflict)
            .WithTags("User Management")
            .RequireAuthorization();
        
        app.MapGet("/user/{username}", async (UserDb db, string username) => 
                await db.Users.FindAsync(username) is { } user
                    ? Results.Ok(user)
                    : Results.NotFound())
            .Produces<User>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .WithTags("User Management")
            .RequireAuthorization();

        app.MapDelete("/user/{username}", async (UserDb db, string username) =>
            {
                var user = await db.Users.FindAsync(username);
                if (user is null) return Results.NotFound();
                db.Users.Remove(user);
                await db.SaveChangesAsync();
                return Results.Ok();
            })
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .WithTags("User Management")
            .RequireAuthorization();
    }
}