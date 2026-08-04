namespace ERechnung.App.Services;

public interface IUserDialogService
{
    bool Confirm(string message, string title);
    void ShowInfo(string message, string title);
    void ShowError(string message, string title);
}
