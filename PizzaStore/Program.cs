using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PizzaStore;
using PizzaStore.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<PizzaDb>(options => options.UseInMemoryDatabase("pizzas"));
builder.Services.AddDbContext<UserDb>(options => options.UseInMemoryDatabase("users"));
builder.Services.AddAuthorization();
builder.Services.AddSwaggerGen(ConfigureSwaggerGenOptions);
var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
builder.Services.AddAuthentication(ConfigureAuthenticationOptions()).AddJwtBearer(ConfigureJwtOptions(config));
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PizzaStore API V1");
    });
}

PizzaStoreEndpoints.Map(app, config);

app.UseAuthorization();
app.Run();
return;

void ConfigureSwaggerGenOptions(SwaggerGenOptions swaggerGenOptions)
{
    swaggerGenOptions.SwaggerDoc("v1", new OpenApiInfo { Title = "PizzaStore API", Description = "Making the Pizzas you love", Version = "v1" });
    swaggerGenOptions.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });

    swaggerGenOptions.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
}

Action<AuthenticationOptions> ConfigureAuthenticationOptions()
{
    return option =>
    {
        option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    };
}

Action<JwtBearerOptions> ConfigureJwtOptions(IConfigurationRoot configurationRoot)
{
    return jwtOption =>
    {
        var key = configurationRoot.GetValue<string>("JwtConfig:SecretKey");
        var keyBytes = Encoding.ASCII.GetBytes($"{key}");
        jwtOption.SaveToken = true;
        jwtOption.TokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateLifetime = true,
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidIssuer = configurationRoot["JwtConfig:Issuer"],
            ValidAudience = configurationRoot["JwtConfig:Audience"]
        };
    };
}