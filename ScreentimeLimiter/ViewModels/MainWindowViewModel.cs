using System.Collections.Generic;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScreentimeLimiter.Models;

namespace ScreentimeLimiter.ViewModels;

/// <summary>
/// Main window VM class
/// </summary>
public partial class MainWindowViewModel : ViewModelBase {
    /// <summary>
    /// for hours, minutes parsing when entering warnTimes
    /// </summary>
    private const string RegString = "^([0-9]{1,2})([mh])$";
    private readonly DataStorage _dataStorage;
    
    // parsed input values
    private uint _hours;
    private uint _minutes;
    private uint[][]? _warnTimes;
    
    // tracking text
    [ObservableProperty] private string _hoursText = null!;
    [ObservableProperty] private string _minutesText = null!;
    [ObservableProperty] private string _warnTimesText = null!;
    public string ConfirmButtonMessage => IsExactTimeToggled ?? false ? "Set exact time" : "Set countdown time";
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(ConfirmButtonMessage))]
    private bool? _isExactTimeToggled;
    
    public bool IsWarnTimesEnabled => IsHoursValid && IsMinutesValid;
    public bool IsConfirmEnabled => IsHoursValid && IsMinutesValid && IsWarnTimesValid;
    private bool CanSetTime() => IsConfirmEnabled;
    
    // is input valid
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConfirmEnabled))]
    [NotifyPropertyChangedFor(nameof(IsWarnTimesEnabled))]
    private bool _isHoursValid;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConfirmEnabled))]
    [NotifyPropertyChangedFor(nameof(IsWarnTimesEnabled))]
    private bool _isMinutesValid;

    public bool IsWarnTimesValid => CheckWarnParse(WarnTimesText);
    
    /// <summary>
    /// interface to allow us using some UI exclusive commands that we cannot do in VM by default
    /// </summary>
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
    /// Upon warn times change we re-evaluate if warn times is valid and consequently if confirm is enabled
    /// </summary>
    partial void OnWarnTimesTextChanged(string value) {
        OnPropertyChanged(nameof(IsWarnTimesValid));
        OnPropertyChanged(nameof(IsConfirmEnabled));
    }

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
        _dataStorage = new DataStorage();
        ReadFromDisk();
    }
    
    /// <summary>
    /// Only to be used by XAML for design time preview working correctly
    /// </summary>
    public MainWindowViewModel() : this(new MockWindowHider()) {}

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