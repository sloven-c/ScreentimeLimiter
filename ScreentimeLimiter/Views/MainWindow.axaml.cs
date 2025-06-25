using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ScreentimeLimiter.Models;

namespace ScreentimeLimiter.Views;

public partial class MainWindow : Window, IWindowHider {
    /// <summary>
    /// How long should the window be above all other windows after the application starts up
    /// </summary>
    private const int TopMostTime = 10000;
    
    /// <summary>
    /// Implementation for <see cref="IWindowHider"/>: hiding of the window
    /// </summary>
    public void HideWindow() => Hide();
    
    /// <summary>
    /// Implementation for the <see cref="IWindowHider"/>: displaing of the notification
    /// </summary>
    /// <param name="title">notification title</param>
    /// <param name="text">notification text</param>
    public async void DisplayNotification(string title, string text) {
        try {
            await Dispatcher.UIThread.InvokeAsync(async () => {
                Topmost = true;

                var messageBox = MessageBoxManager.GetMessageBoxStandard(
                    title: title,
                    text: text,
                    @enum: ButtonEnum.Ok,
                    icon: MsBox.Avalonia.Enums.Icon.Warning
                );

                await messageBox.ShowWindowAsync();

                Topmost = false;
            });
        }
        catch (Exception e) {
            Console.WriteLine($"Failed to display notification: {e}");
        }
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public MainWindow() {
        InitializeComponent();
        Opened += OnWindowOpened;
    }

    /// <summary>
    /// Runs immediately when application window opens
    /// </summary>
    private void OnWindowOpened(object? sender, EventArgs e) {
        Topmost = true;
        Activate();
        Focus();

        Task.Delay(TopMostTime).ContinueWith(_ => {
            Dispatcher.UIThread.InvokeAsync(() => {
                Topmost = false;
            });
        });
    }
    
}