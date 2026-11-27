using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using TripService.Infrastructure.Authentication;
using TripService.Infrastructure.Options;


namespace TripService.Infrastructure.Tests.Authentication
{
    public class JwtTokenServiceTests
    {
        [Fact]
        public void GenerateToken_ShouldContainCorrectUserId()
        {
            var options = new JwtOptions
            {
                Issuer = "TTravel.Tests",
                Audience = "TTravel.Tests.Api",
                SigningKeyBase64 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                ExpirationMinutes = 60
            };

            var service = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(options));

            var token = service.GenerateToken(15);

            var handler = new JwtSecurityTokenHandler();
            var parsedToken = handler.ReadJwtToken(token);

            Assert.Equal("15", parsedToken.Subject);
        }
    }
}
