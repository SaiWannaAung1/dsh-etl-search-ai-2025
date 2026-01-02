namespace DshEtlSearch.Core.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        
        // Allow Value to be null (it will be null if IsSuccess is false)
        public T? Value { get; } 
        
        // Allow Error to be null (it will be null if IsSuccess is true)
        public string? Error { get; }

        private Result(bool isSuccess, T? value, string? error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        public static Result<T> Success(T value) 
        {
            // If value passed is null, we can decide to throw or allow it. 
            // Usually Success means we have a value.
            if (value == null) throw new ArgumentNullException(nameof(value));
            return new Result<T>(true, value, null);
        }

        public static Result<T> Failure(string error) 
        {
            // For failure, Value is 'default' (null for objects) and Error is required
            return new Result<T>(false, default, error);
        }
    }
    
    public class Result
    {
        public bool IsSuccess { get; }
        public string? Error { get; }

        private Result(bool isSuccess, string? error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success() => new Result(true, null);
        public static Result Failure(string error) => new Result(false, error);
    }
}