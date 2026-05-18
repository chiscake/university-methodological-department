namespace UniversityMethodologicalDepartment.Bus.Services.Errors;

/// <summary>
/// Типизированная модель распознанной бизнес-ошибки БД: стабильный код + готовое к показу
/// пользователю сообщение + сохранённая исходная информация Postgres для диагностики.
/// </summary>
/// <param name="Code">Стабильный код из <see cref="DatabaseBusinessErrorCodes"/>.</param>
/// <param name="Message">Сообщение для пользователя (на русском языке).</param>
/// <param name="Raw">Исходные поля Postgres-ошибки, если были доступны при распознавании.</param>
public sealed record DatabaseBusinessError(string Code, string Message, PostgresErrorInfo? Raw);
