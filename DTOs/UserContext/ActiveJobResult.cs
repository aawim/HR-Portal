namespace HRM.DTOs.UserContext
{
    public class ActiveJobResult
    {
        public bool Success { get; init; }

        public string Message { get; init; } =
            string.Empty;

        public ActiveJobDto? ActiveJob { get; init; }

        public static ActiveJobResult Successful(
            ActiveJobDto activeJob,
            string message = "Active job loaded successfully.")
        {
            return new ActiveJobResult
            {
                Success = true,
                Message = message,
                ActiveJob = activeJob
            };
        }

        public static ActiveJobResult Failure(
            string message)
        {
            return new ActiveJobResult
            {
                Success = false,
                Message = message,
                ActiveJob = null
            };
        }
    }
}
