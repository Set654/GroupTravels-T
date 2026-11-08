namespace TripService.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Email { get; set; }
    public required string Phone { get; set; }
    public string? TbankPaymentUrl { get; set; }
    public bool IsDarkTheme { get; set; } = false;

}
