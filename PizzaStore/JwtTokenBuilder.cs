using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace PizzaStore;

public static class JwtTokenBuilder
{
    public static string Build(IConfigurationRoot config)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes($"{config["JwtConfig:SecretKey"]}"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var issuer = config["JwtConfig:Issuer"];
        var audience = config["JwtConfig:Audience"];
        var lifetimeMinutes = DateTime.Now.AddMinutes(Convert.ToDouble(config["JwtConfig:LifetimeMinutes"]));
        var token = new JwtSecurityToken(issuer,
            audience,
            expires: lifetimeMinutes,
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}