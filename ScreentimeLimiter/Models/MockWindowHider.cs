namespace ScreentimeLimiter.Models;

public class MockWindowHider : IWindowHider {
    public void HideWindow() { }
    public void DisplayNotification(string title, string text) { }
}