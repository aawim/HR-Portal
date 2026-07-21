namespace HRM.Services
{
    public class ToastService
    {
        public event Action<ToastMessage>? OnShow;

        public void Show(string message, ToastType type = ToastType.Info)
        {
           OnShow?.Invoke(new ToastMessage
            {
                Message = message,
                Type = type
            });
        }


        public void Success(string message)
        {
            Show(message, ToastType.Success);
        }


        public void Error(string message)
        {
            Show(message, ToastType.Error);
        }


        public void Warning(string message)
        {
            Show(message, ToastType.Warning);
        }


        public void Info(string message)
        {
            Show(message, ToastType.Info);
        }
    }


    public class ToastMessage
    {
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
}