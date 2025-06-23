namespace ScreentimeLimiter.Models;

public interface IWindowHider {
    void HideWindow();
    void DisplayNotification(string title, string text);
}