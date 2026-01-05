using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Inventory.Infrastructure.Identity
{
    public class JwtTokenGenerator
    {
        private readonly IConfiguration _config;
        private readonly SymmetricSecurityKey _key;

        public JwtTokenGenerator(IConfiguration config)
        {
            _config = config;
            var keyBytes = Convert.FromBase64String(_config["Jwt:Key"]!);
            if (keyBytes.Length < 32)
                throw new InvalidOperationException(
                    "JWT key must be at least 256 bits (32 bytes)");
            _key = new SymmetricSecurityKey(keyBytes);
        }

        public string Generate(Guid userId, string email, Guid tenantId, string role)
        {
            var claims = new[]
            {
                // User identity
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Email, email),
                
                // Multi-tenancy
                new Claim("TenantId", tenantId.ToString()),
                
                // Authorization
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: new SigningCredentials(
                    _key,
                    SecurityAlgorithms.HmacSha256
                )
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}