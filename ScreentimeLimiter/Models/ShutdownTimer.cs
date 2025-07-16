using System;
using System.Diagnostics;
using System.Timers;

namespace ScreentimeLimiter.Models;

public class ShutdownTimer(uint hours, uint minutes, uint[][]? warnTimes, bool? type) {
    private const uint Midnight = 24 * 60; // midnight is at 24h
    private const uint RecalculationTime = 1; // we perform recalculation every minute
    private const uint RecalculationBuffer = 5; // less than how many minutes does it have to be for us to not perform a recalculation
    
    private readonly bool _type = type.HasValue && type.Value;
    private uint? _minutesToGo;
    private Timer? _mainTimer;

    /// <summary>
    /// Once timer for notification goes off this signals to VM to call DisplayNotification
    /// </summary>
    public event Action<string, string>? NotificationRequested;

    /// <summary>
    /// Gets called immediately after constructor - starts the shutdown timing calculation and execution
    /// </summary>
    public void InitiateShutdown() {
        PrepareCountdown();
        
        var timer = new Timer(TimeSpan.FromMinutes(RecalculationTime));
        timer.Elapsed += (sender, e) => PrepareCountdown();
        timer.AutoReset = true; // keep firing forever
        timer.Enabled = true;
    }

    /// <summary>
    /// Shutdown preparation (calculating minutes, initiating shutdown)
    /// </summary>
    private void PrepareCountdown() {
        if (!VerifyRecalculation()) return;
        
        // we save the old minutes value in order to prevent the countdown timings from recalculating if the buffer time is not exceeded
        CalculateMinutesToGo();
        CountdownMain();
    }

    /// <summary>
    /// Determine if re-calculations can happen
    /// </summary>
    /// <returns>boolean if recalculation can happen</returns>
    private bool VerifyRecalculation() {
        // if we do not have minutes left or timers initialised then (re)calculation must happen
        if (!_minutesToGo.HasValue || _mainTimer == null) return true;
        return TimeSpan.FromMilliseconds(_mainTimer.Interval) > TimeSpan.FromMinutes(RecalculationBuffer);
    }

    /// <summary>
    /// Calculates how many minutes left till the shutdown
    /// </summary>
    private void CalculateMinutesToGo() {
        _minutesToGo = 60 * hours + minutes; // this is for relative shutdown (aka shutdown in x hours x minutes)
        if (!_type) return;
        
        // we want the system to shutdown at exact time aka 22h
        // first we need to check if the time is on the same day (the given time is bigger than current time/date)
        var now = DateTime.Now;
        var curMinutes = (uint)(60 * now.Hour + now.Minute);
        if (_minutesToGo > curMinutes) {
            _minutesToGo -= curMinutes; // we gain the amount of minutes necessary to reach our desired time
        }
        else {
            var timeToMidnight = Midnight - curMinutes; // the time it will take us to reach midnight
            _minutesToGo += timeToMidnight;
        }
    }
    
    /// <summary>
    /// Starts countdown for the computer shutdown
    /// </summary>
    private void CountdownMain() {
        if (!_minutesToGo.HasValue) return;
        _mainTimer?.Dispose();
        
        _mainTimer = new Timer(TimeSpan.FromMinutes(_minutesToGo.Value));
        _mainTimer.Elapsed += (sender, e) => SystemShutdown();
        _mainTimer.AutoReset = false; // only fire once
        _mainTimer.Enabled = true;
    }

    /// <summary>
    /// Sends notification request to DisplayNotification and provides necessary parameters
    /// </summary>
    /// <param name="duration">in how much time the computer will shutdown</param>
    /// <param name="isHour">whether the duration is in hours (1) or minutes (0)</param>
    /// <param name="iterator">iterator which allows us to disable isFired data part in the <see cref="warnTimes"/> variable</param>
    private void SendShutdownNotification(uint duration, uint isHour, int iterator) {
        if (warnTimes != null)
            warnTimes[iterator][2] = 1; // the timer has fired already and can't fire again
        var unit = isHour == 1 ? "hours" : "minutes"; 
        
        const string title = "Shutdown Warning";
        var text = $"System will shutdown in {duration} {unit}";
        NotificationRequested?.Invoke(title, text);
    }

    /// <summary>
    /// Attemps to shutdown the computer (Windows/Linux)
    /// </summary>
    private static void SystemShutdown() {
        if (OperatingSystem.IsLinux()) {
            var psi = new ProcessStartInfo("systemctl", "poweroff") {
                CreateNoWindow = true,
                UseShellExecute = false
            };

            Process.Start(psi);
        } else if (OperatingSystem.IsWindows()) {
            var psi = new ProcessStartInfo("shutdown", "/s /t 0") {
                CreateNoWindow = true,
                UseShellExecute = false
            };

            Process.Start(psi);
        }
    }
}