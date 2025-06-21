using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ScreentimeLimiter.Models;
using ScreentimeLimiter.ViewModels;

namespace ScreentimeLimiter.Views;

public partial class MainWindow : Window {
    private const string RegString = "^([0-9]{1,3})([mh])$";
    private uint _hours, _minutes;
    private uint[][]? _warnTimes;
    private readonly DataStorage _dataStorage;

    public MainWindow() {
        InitializeComponent();
        _dataStorage = new DataStorage();
        ReadFromDisk();
    }

    protected override void OnLoaded(RoutedEventArgs e) {
        base.OnLoaded(e);
        SetConfirmButtonMessage();
    }

    private void ReadFromDisk() {
        var data = _dataStorage.Read();
        if (!data.HasValue) return;
        var dataVal = data.Value;

        Hours.Text = dataVal.Hours.ToString();
        Minutes.Text = dataVal.Minutes.ToString();
        WarnTimes.Text = dataVal.WarnTimes;
        ToggleExactRelative.IsChecked = dataVal.IsChecked;
    }

    private void SaveToDisk() {
        var dataToSave = new DataStorage.DataPackage(_hours, _minutes, WarnTimes.Text!, ToggleExactRelative.IsChecked ?? false);
        _dataStorage.Save(dataToSave);
    }

    private void ExactTimeChangeBox_OnIsCheckedChanged(object? sender, RoutedEventArgs e) {
        SetConfirmButtonMessage();
    }

    private void SetConfirmButtonMessage() {
        if (DataContext is MainWindowViewModel viewModel) {
            viewModel.ConfirmButtonMessage =
                ToggleExactRelative.IsChecked ?? false ? viewModel.AbsoluteTimeMessage : viewModel.RelativeTimeMessage;
        }
    }

    private void HoursAutoCompleteBox_OnTextChanged(object? sender, TextChangedEventArgs e) {
        CheckParse();
    }
    
    private void MinutesAutoCompleteBox_OnTextChanged(object? sender, TextChangedEventArgs e) {
        CheckParse();
    }

    private void CheckParse() {
        if ((!uint.TryParse(Hours.Text, out var resultHours) || !(resultHours <= 24)) ||
            (!uint.TryParse(Minutes.Text, out var resultMinutes) || !(resultMinutes <= 59))) {
            Confirm.IsEnabled = false;
            WarnTimes.IsEnabled = false;
            return;
        }

        _hours = resultHours;
        _minutes = resultMinutes;
        WarnTimes.IsEnabled = true;

        CheckWarnParse();
    }

    private void CheckWarnParse() {
        var warnTimes = WarnTimes.Text;
        if (string.IsNullOrEmpty(warnTimes)) {
            Confirm.IsEnabled = true;
            return;
        }
        
        var warnTimesArray = warnTimes.Trim().ToLower().Split(",");
        var times = new List<uint[]>();
        
        foreach (var warn in warnTimesArray) {
            var match = Regex.Match(warn, RegString);
            if (match.Success) {
                var number = uint.Parse(match.Groups[1].Value);
                var unit = (uint)(match.Groups[2].Value == "h" ? 1 : 0);
                times.Add([number, unit]);
            }
            else {
                Confirm.IsEnabled = false;
                _warnTimes = null;
                return;
            }
        }

        if (times.Count != 0) {
            _warnTimes = times.ToArray();
        }

        Confirm.IsEnabled = true;
    }

    private void WarnTimes_OnTextChanged(object? sender, TextChangedEventArgs e) {
        CheckParse();
    }

    private void TimeSetConfirm_OnClick(object? sender, RoutedEventArgs e) {
        DisableAllButtons();
    }

    private void DisableAllButtons() {
        Hours.IsEnabled = false;
        Minutes.IsEnabled = false;
        WarnTimes.IsEnabled = false;
        ToggleExactRelative.IsEnabled = false;
        Confirm.IsEnabled = false;
        Hide();
        SaveToDisk();

        var shutTimer = new ShutdownTimer(_hours, _minutes, ToggleExactRelative.IsChecked, _warnTimes);

        shutTimer.NotificationRequested += DisplayNotification;
        shutTimer.InitiateShutdown();
    }

    private async void DisplayNotification(string title, string text) {
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
}