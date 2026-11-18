namespace POSSystem.WPF.Business.Services.interfase
{
    public interface IDialogService
    {
        void ShowWarning(string message, string title = "تنبيه");
        void ShowError(string message, string title = "خطأ");
        void ShowMessage(string message, string title = "رسالة");
        bool ShowConfirmation(string message, string title = "تأكيد");
    }
}
