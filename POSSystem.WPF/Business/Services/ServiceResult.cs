namespace POSSystem.WPF.Business.Services;

public record ServiceResult<T>
{
    public T? Value { get; init; }
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? Title { get; init; }
    public Exception? Exception { get; init; }

    public static ServiceResult<T> Ok(T value, string? message = null, string? title = null)
        => new() { Success = true, Value = value, Message = message, Title = title };

    public static ServiceResult<T> Fail(string message, string? title = null, Exception? ex = null)
        => new() { Success = false, Message = message, Title = title, Exception = ex };

    public static ServiceResult<T> Fail(Exception ex, string? title = null)
        => new() { Success = false, Message = ex.Message, Title = title, Exception = ex };
}

public record ServiceResult
{
    private static readonly ServiceResult CachedOk = new() { Success = true };

    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? Title { get; init; }
    public Exception? Exception { get; init; }

    public static ServiceResult Ok(string? message = null, string? title = null)
        => message == null && title == null ? CachedOk : new() { Success = true, Message = message, Title = title };

    public static ServiceResult Fail(string message, string? title = null, Exception? ex = null)
        => new() { Success = false, Message = message, Title = title, Exception = ex };

    public static ServiceResult Fail(Exception ex, string? title = null)
        => new() { Success = false, Message = ex.Message, Title = title, Exception = ex };
}