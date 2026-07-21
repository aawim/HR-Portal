using HRM.Enum;

namespace HRM.DTOs.Security
{
       public static class AccessResult
    {
        public static AccessResultDto Allow(
              string message = "Access granted.")
        {
            return new AccessResultDto
            {
                Allowed = true,
                Code = AccessCode.Success,
                Message = message
            };
        }


        public static AccessResultDto Deny(
            AccessCode code,
            string message)
        {
            return new AccessResultDto
            {
                Allowed = false,
                Code = code,
                Message = message
            };
        }


        public static AccessResultDto Deny(
            AccessCode code,
            string message,
            params string[] details)
        {
            return new AccessResultDto
            {
                Allowed = false,
                Code = code,
                Message = message,
                Details = details.ToList()
            };
        }


    }
}
