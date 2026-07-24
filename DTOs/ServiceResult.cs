namespace HRM.DTOs
{
    public class ServiceResult
    {

        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public static ServiceResult Ok(string message = "")
        {
            return new ServiceResult
            {
                Success = true,
                Message = message
            };
        }

        public static ServiceResult Failed(string message)
        {
            return new ServiceResult
            {
                Success = false,
                Message = message
            };
        }

     

        //public bool Success { get; set; }
        //public string Message { get; set; }

        //public static ServiceResult Ok(string message = "")
        //{
        //    return new ServiceResult
        //    {
        //        Success = true,
        //        Message = message
        //    };
        //}

        public static ServiceResult Fail(string message)
        {
            return new ServiceResult
            {
                Success = false,
                Message = message
            };
        }


    }
}
