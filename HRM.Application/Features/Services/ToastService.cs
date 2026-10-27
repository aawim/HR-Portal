
namespace HRM.Services;


    public class ToastMessage
    {
        public string Title { get; set; } = "Information";
        public string Message { get; set; } = "";
        public ToastType Type { get; set; }
    }



    public enum ToastType
    {
        Success,
        Error,
        Warning,
        Info
    }
