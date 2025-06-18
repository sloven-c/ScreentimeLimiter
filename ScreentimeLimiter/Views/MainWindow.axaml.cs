using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using ScreentimeLimiter.ViewModels;

namespace ScreentimeLimiter.Views;

public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
    }

    private void ExactTimeChangeBox_OnIsCheckedChanged(object? sender, RoutedEventArgs e) {
        if (DataContext is MainWindowViewModel viewModel && sender is ToggleButton checkbox) {
            viewModel.ConfirmButtonMessage =
                (bool)checkbox.IsChecked! ? viewModel.AbsoluteTimeMessage : viewModel.RelativeTimeMessage;
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
            return;
        }

        CheckWarnParse();
    }

    private void CheckWarnParse() {
        var warnTimes = WarnTimes.Text;
        if (string.IsNullOrEmpty(warnTimes)) {
            Confirm.IsEnabled = true;
            return;
        }
        
        var warnTimesArray = warnTimes.Trim().ToLower().Split(",");

        const string regex = "^([0-9]{1,3})([mh])$";

        if (warnTimesArray.Select(warn => Regex.Match(warn, regex)).Any(match => !match.Success)) {
            Confirm.IsEnabled = false;
            return;
        }

        Confirm.IsEnabled = true;
    }

    private void WarnTimes_OnTextChanged(object? sender, TextChangedEventArgs e) {
        CheckParse();
    }

    private void TimeSetConfirm_OnClick(object? sender, RoutedEventArgs e) {
        Hours.IsEnabled = false;
        Minutes.IsEnabled = false;
        WarnTimes.IsEnabled = false;
        ToggleExactRelative.IsEnabled = false;
        Confirm.IsEnabled = false;
    }
}