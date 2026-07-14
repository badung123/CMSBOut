namespace Bankout.API.Models.Enums;

public enum StatusActionEnum
{
    WaitAccept = 1,
    WaitBank = 2,
    Success = 3,
    ErrorRequestBank = 4,
    ErrorBank = 5
}
