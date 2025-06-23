using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ScreentimeLimiter.Models;
using ScreentimeLimiter.ViewModels;

namespace ScreentimeLimiter.Views;

public partial class MainWindow : Window, IWindowHider {
    private const int TopMostTime = 10000;
    
    public void HideWindow() => Hide();
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

    public MainWindow() {
        InitializeComponent();
        Opened += OnWindowOpened;
    }

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


    protected override void OnLoaded(RoutedEventArgs e) {
        base.OnLoaded(e);
        if (DataContext is MainWindowViewModel vm) {
            vm.SetConfirmButtonMessage();
        }
    }

    
}