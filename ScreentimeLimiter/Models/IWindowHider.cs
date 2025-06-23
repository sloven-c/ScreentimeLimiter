namespace ScreentimeLimiter.Models;

public interface IWindowHider {
    /// <summary>
    /// Hides window
    /// </summary>
    void HideWindow();
    
    /// <summary>
    /// Displays notification
    /// </summary>
    /// <param name="title">notification title</param>
    /// <param name="text">notification body</param>
    void DisplayNotification(string title, string text);
}