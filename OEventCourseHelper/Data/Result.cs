namespace OEventCourseHelper.Data;

internal abstract record Result<TValue, TError>
    where TError : notnull;

internal sealed record Success<TValue, TError>(TValue Value) : Result<TValue, TError>
    where TError : notnull;

internal sealed record Failure<TValue, TError>(TError Error) : Result<TValue, TError>
    where TError : notnull;
