namespace FleetCargo.Services;

/// <summary>Результат операції бізнес-логіки: успіх або текст помилки.</summary>
public class OperationResult
{
    public bool Success { get; private init; }
    public string Error { get; private init; } = "";
    public int Id { get; private init; }

    public static OperationResult Ok(int id = 0) => new() { Success = true, Id = id };

    public static OperationResult Fail(string error) => new() { Success = false, Error = error };
}
