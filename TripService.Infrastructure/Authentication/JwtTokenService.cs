using Microsoft.Extensions.Options;
using System.Security.Claims;
using TripService.Infrastructure.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;


namespace TripService.Infrastructure.Authentication
{
    public sealed class JwtTokenService
    {
        private readonly JwtOptions _options;
        public JwtTokenService(IOptions<JwtOptions> options)
        {
            _options = options.Value;
        }
        private List<Claim> CreateClaims(int userId)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim("sub", userId.ToString())
            };
            return claims;
        }
        private SigningCredentials CreateSigningCredentials()
        {
            var keyBytes = Convert.FromBase64String(_options.SigningKeyBase64);

            if (keyBytes.Length < 32)
            {
                throw new InvalidOperationException("Ключ подписи должен содержать не менее 32 байт.");
            }
            var securityKey = new SymmetricSecurityKey(keyBytes);

            return new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        }
        public string GenerateToken(int userId)
        {
            var now = DateTime.UtcNow;

            JwtSecurityToken token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: CreateClaims(userId),
                notBefore: now,
                expires: now.AddMinutes(_options.ExpirationMinutes),
                signingCredentials : CreateSigningCredentials()
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

}
