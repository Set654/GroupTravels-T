namespace TripService.Api.Contracts;

public sealed class CreateExpenseRequest
{
    public int PaidByMemberId { get; set; }
    public string? Title { get; set; }
    public string? Category { get; set; }
    public decimal TotalAmount { get; set; }
    public List<ExpenseShareRequest>? Splits { get; set; }

    public string? SplitMode { get; set; }
    public List<int>? MemberIds { get; set; }
}
public sealed class ExpenseShareRequest
{
    public int MemberId { get; set; }
    public decimal ShareAmount { get; set; }
}
