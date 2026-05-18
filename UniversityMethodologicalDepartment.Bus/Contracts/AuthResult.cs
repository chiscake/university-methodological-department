namespace UniversityMethodologicalDepartment.App.Bus.Contracts;

/// <summary>
/// Результат операции входа: успех с данными пользователя или ошибка с сообщением.
/// </summary>
public sealed class AuthResult
{
    private AuthResult(bool isSuccess, string? errorMessage, UserInfo? user)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        User = user;
    }

    /// <summary>Признак успешного входа.</summary>
    public bool IsSuccess { get; }

    /// <summary>Сообщение об ошибке при неуспешном входе.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Данные пользователя при успешном входе.</summary>
    public UserInfo? User { get; }

    /// <summary>Создаёт результат успешного входа.</summary>
    public static AuthResult Success(UserInfo user) => new(true, null, user);

    /// <summary>Создаёт результат неуспешного входа с сообщением об ошибке.</summary>
    public static AuthResult Failure(string message) => new(false, message, null);
}
