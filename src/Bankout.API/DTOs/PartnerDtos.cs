namespace Bankout.API.DTOs;

public record PartnerBankItem(string BankNo, string BankName, string ShortBankName);

public record PartnerPayOutResponse(int Status, string Message);

public record PayoutCallbackRequest(
    int Status,
    string Type,
    string? RequestId,
    string TransId,
    double Amount,
    string Date,
    string Message,
    string Signature);

public record CallbackResponse(string ErrorCode, string ErrorDescription);
