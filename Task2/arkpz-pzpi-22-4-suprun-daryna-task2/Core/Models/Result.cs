namespace Core.Models
{
    public class Result
    {
        public bool IsSuccessful { get; set; }

        public Result(bool isSuccessful)
        {
            IsSuccessful = isSuccessful;
        }
    }

    public class Result<T> : Result where T : class
    {
        public T Data { get; }

        public Result(bool isSuccessful, T data = null) : base(isSuccessful)
        {
            IsSuccessful = isSuccessful;
            Data = data;
        }
    }
}
