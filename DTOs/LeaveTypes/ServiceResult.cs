namespace HRM.DTOs.LeaveTypes
{
    public sealed class ServiceResult<T>
    {
        public bool Success { get; set; }

        public string Message { get; set; } = "";

        public T? Data { get; set; }

        public static ServiceResult<T> Successful(
            T data,
            string message = "")
        {
            return new()
            {
                Success = true,
                Data = data,
                Message = message
            };
        }

        public static ServiceResult<T> Failure(
            string message)
        {
            return new()
            {
                Success = false,
                Message = message
            };
        }
    }
}
