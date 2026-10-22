namespace HRM.Services
{
    public class ToastService
    {
        public event Action<ToastMessage>? OnShow;

        public void Show(string message, ToastType type = ToastType.Info, string? title = null)
        {
           OnShow?.Invoke(new ToastMessage
            {
                Message = message,
                Title = string.IsNullOrWhiteSpace(title) ? type switch
                {
                    ToastType.Success => "Success",
                    ToastType.Error => "Action failed",
                    ToastType.Warning => "Attention needed",
                    _ => "Information"
                } : title.Trim(),
                Type = type
            });
        }


        public void Success(string message, string? title = null)
        {
            Show(message, ToastType.Success, title);
        }


        public void Error(string message, string? title = null)
        {
            Show(message, ToastType.Error, title);
        }


        public void Warning(string message, string? title = null)
        {
            Show(message, ToastType.Warning, title);
        }


        public void Info(string message, string? title = null)
        {
            Show(message, ToastType.Info, title);
        }
    }


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
}
