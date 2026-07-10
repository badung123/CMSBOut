using Bankout.API.Models.Enums;

namespace Bankout.API.DTOs;

public record CreateBankoutRequest(
    string RequestBankId,
    string UserName,
    string BankAccountName,
    string BankAccountNumber,
    double Amount,
    string Bank,
    int AgentId);

public record BankoutFilterRequest(
    string? UserName,
    string? RequestBankId,
    StatusActionEnum? Status,
    DateTime? FromDate,
    DateTime? ToDate,
    int Page = 1,
    int PageSize = 10);

public record BankoutListItemResponse(
    Guid Id,
    string UserName,
    string BankAccountName,
    string BankAccountNumber,
    double Amount,
    string Bank,
    string AgentName,
    string RequestBankId,
    DateTime CreatedDate,
    DateTime? BankDate,
    string? Log,
    StatusActionEnum Status);

public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public record AgentOptionResponse(int Id, string AgentName);
