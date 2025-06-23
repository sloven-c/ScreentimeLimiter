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
    
    private uint _hours;
    private uint _minutes;
    private uint[][]? _warnTimes;
    
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

    /// <summary>
    /// Command that fires upon confirming the time
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSetTime))]
    private void SetTime() {
        _windowHider.HideWindow();
        SaveToDisk();

        var shutTimer = new ShutdownTimer(_hours, _minutes, _warnTimes, IsExactTimeToggled);

        shutTimer.NotificationRequested += _windowHider.DisplayNotification;
        shutTimer.InitiateShutdown();
    }

    /// <summary>
    /// Event that fires upon hours text change
    /// </summary>
    /// <param name="value">new value of hours textbox</param>
    partial void OnHoursTextChanged(string value) {
        var parse = ParseUint(value);

        IsHoursValid = parse <= 24;
        if (IsHoursValid) _hours = parse!.Value;
    }

    /// <summary>
    /// Event that fires upon minutes text change
    /// </summary>
    /// <param name="value">new value of minutes textbox</param>
    partial void OnMinutesTextChanged(string value) {
        var parse = ParseUint(value);

        IsMinutesValid = parse <= 59;
        if (IsMinutesValid) _minutes = parse!.Value;
    }

    /// <summary>
    /// Event that fires upon warning times text change
    /// </summary>
    /// <param name="value">new value of warn time textbox</param>
    partial void OnWarnTimesTextChanged(string value) => IsWarnTimesValid = CheckWarnParse(value);
    
    public void SetConfirmButtonMessage() => 
        // todo can we try to do this via property so we don't have to call this variable
        ConfirmButtonMessage = IsExactTimeToggled ?? false ? AbsoluteTimeMessage : RelativeTimeMessage;

    // todo this one should be called via property as mentioned above
    partial void OnIsExactTimeToggledChanged(bool? value) => SetConfirmButtonMessage();

    /// <summary>
    /// Attempts to parse into uint, primarily for hours and minutes
    /// </summary>
    /// <param name="value">string value to be parsed</param>
    /// <returns>uint if parsing succeeds, null otherwise</returns>
    private static uint? ParseUint(string value) {
        return uint.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    /// Attempts to parse warning times into an array of uint, alonside with 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="windowHider">interface IWindowHider for strictly UI methods that can't be called in VM</param>
    public MainWindowViewModel(IWindowHider windowHider) {
        _windowHider = windowHider;
        // todo remove
        SetConfirmButtonMessage();
        _dataStorage = new DataStorage();
        ReadFromDisk();
    }

    /// <summary>
    /// Calls method to read from disk and updates UI controls
    /// </summary>
    private void ReadFromDisk() {
        var data = _dataStorage.Read();
        if (!data.HasValue) return;
        var dataVal = data.Value;

        HoursText = dataVal.Hours.ToString();
        MinutesText = dataVal.Minutes.ToString();
        WarnTimesText = dataVal.WarnTimes;
        IsExactTimeToggled = dataVal.IsChecked;
    }

    /// <summary>
    /// Calls method to save values in UI controls onto disk
    /// </summary>
    private void SaveToDisk() {
        var dataToSave = new DataStorage.DataPackage(_hours, _minutes, WarnTimesText,
            IsExactTimeToggled ?? false);
        _dataStorage.Save(dataToSave);
    }
}