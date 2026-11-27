namespace TripService.Application.Expenses;

public sealed record ExpenseShare(int MemberId, decimal ShareAmount);
public sealed record CreateExpenseCommand(int PaidByMemberId, string? Title, string? Category,
    decimal TotalAmount, IReadOnlyList<ExpenseShare>? Splits, string? SplitMode,
    IReadOnlyList<int>? MemberIds);
public sealed record ExpenseItem(int ExpenseId, int TripId, int PaidByMemberId, string Title,
    string Category, decimal TotalAmount, DateTime CreatedAtUtc,
    IReadOnlyList<ExpenseShare> Splits);

public sealed record SettlementRecord(int FromMemberId, int ToMemberId, decimal Amount);
public sealed record BalanceSource(IReadOnlyList<MemberSnapshot> Members,
    IReadOnlyList<ExpenseSnapshot> Expenses, IReadOnlyList<SettlementRecord> Settlements);
public sealed record MemberSnapshot(int MemberId, string Name, string? TbankPaymentUrl);
public sealed record ExpenseSnapshot(int PaidByMemberId, decimal TotalAmount,
    IReadOnlyList<ExpenseShare> Splits);

public interface IExpenseRepository
{
    Task<int> CreateAsync(int tripId, int payerMemberId, string title, string category,
        decimal totalAmount, IReadOnlyList<ExpenseShare> splits, CancellationToken ct);
    Task<IReadOnlyList<ExpenseItem>> ListAsync(int tripId, CancellationToken ct);
    Task<BalanceSource> GetBalanceSourceAsync(int tripId, CancellationToken ct);
}

public sealed record OptimizedTransfer(int FromMemberId, string FromMemberName,
    int ToMemberId, string ToMemberName, decimal Amount, string? TargetTbankUrl);
public sealed record BalancesResponse(int TripId, IReadOnlyList<OptimizedTransfer> OptimizedTransfers);
