using TripService.Api.Extensions;
using TripService.Application.Trips;
using TripService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPersistence(builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddScoped<TripQueryService>();
builder.Services.AddScoped<CreateTripService>();
builder.Services.AddScoped<TripMemberService>();

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();