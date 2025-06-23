using System.Collections.Generic;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScreentimeLimiter.Models;

namespace ScreentimeLimiter.ViewModels;

public partial class MainWindowViewModel : ViewModelBase {
    private static string AbsoluteTimeMessage => "Set exact time";
    private static string RelativeTimeMessage => "Set countdown time";
    private const string RegString = "^([0-9]{1,2})([mh])$"; // for hours, minutes parsing when entering warnTimes
    private readonly DataStorage _dataStorage;
    
    private uint[][]? _warnTimes;

    private uint _hours, _minutes;
    
    // tracking text
    [ObservableProperty] private string _hoursText = null!;
    [ObservableProperty] private string _minutesText = null!;
    [ObservableProperty] private string _warnTimesText = null!;
    [ObservableProperty] private string _confirmButtonMessage = null!;

    [ObservableProperty] private bool? _isExactTimeToggled;
    public bool IsWarnTimesEnabled => IsHoursValid && IsMinutesValid;
    public bool IsConfirmEnabled => IsHoursValid && IsMinutesValid && IsWarnTimesValid;
    private bool CanSetTime() => IsHoursValid && IsMinutesValid && IsWarnTimesValid;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConfirmEnabled))]
    [NotifyPropertyChangedFor(nameof(IsWarnTimesEnabled))]
    private bool _isHoursValid;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConfirmEnabled))]
    [NotifyPropertyChangedFor(nameof(IsWarnTimesEnabled))]
    private bool _isMinutesValid;
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(IsConfirmEnabled))]
    private bool _isWarnTimesValid;

    private readonly IWindowHider _windowHider;

    [RelayCommand(CanExecute = nameof(CanSetTime))]
    private void SetTime() {
        _windowHider.HideWindow();
        SaveToDisk();

        var shutTimer = new ShutdownTimer(_hours, _minutes, _warnTimes, IsExactTimeToggled);

        shutTimer.NotificationRequested += _windowHider.DisplayNotification;
        shutTimer.InitiateShutdown();
    }

    partial void OnHoursTextChanged(string value) {
        var parse = ParseUint(value);

        IsHoursValid = parse <= 24;
        if (IsHoursValid) _hours = parse!.Value;
    }

    partial void OnMinutesTextChanged(string value) {
        var parse = ParseUint(value);

        IsMinutesValid = parse <= 59;
        if (IsMinutesValid) _minutes = parse!.Value;
    }

    partial void OnWarnTimesTextChanged(string value) => IsWarnTimesValid = CheckWarnParse(value);

    public void SetConfirmButtonMessage() => 
        ConfirmButtonMessage = IsExactTimeToggled ?? false ? AbsoluteTimeMessage : RelativeTimeMessage;

    partial void OnIsExactTimeToggledChanged(bool? value) => SetConfirmButtonMessage();

    private static uint? ParseUint(string value) {
        return uint.TryParse(value, out var result) ? result : null;
    }

    private bool CheckWarnParse(string value) {
        if (string.IsNullOrEmpty(value)) {
            return false;
        }

        var warnTimesArray = value.Trim().ToLower().Split(",");
        var times = new List<uint[]>();
        
        foreach (var warn in warnTimesArray) {
            var match = Regex.Match(warn, RegString);
            if (match.Success) {
                var number = uint.Parse(match.Groups[1].Value);
                var unit = (uint)(match.Groups[2].Value == "h" ? 1 : 0);
                times.Add([number, unit]);
            }
            else {
                _warnTimes = null;
                return false;
            }
        }

        if (times.Count != 0) {
            _warnTimes = times.ToArray();
        }

        return true;
    }

    public MainWindowViewModel(IWindowHider windowHider) {
        _windowHider = windowHider;
        SetConfirmButtonMessage();
        _dataStorage = new DataStorage();
        ReadFromDisk();
    }

    private void ReadFromDisk() {
        var data = _dataStorage.Read();
        if (!data.HasValue) return;
        var dataVal = data.Value;

        HoursText = dataVal.Hours.ToString();
        MinutesText = dataVal.Minutes.ToString();
        WarnTimesText = dataVal.WarnTimes;
        IsExactTimeToggled = dataVal.IsChecked;
    }

    private void SaveToDisk() {
        var dataToSave = new DataStorage.DataPackage(_hours, _minutes, WarnTimesText,
            IsExactTimeToggled ?? false);
        _dataStorage.Save(dataToSave);
    }
}