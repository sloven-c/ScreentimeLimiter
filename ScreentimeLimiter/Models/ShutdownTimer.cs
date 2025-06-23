using System;
using System.Diagnostics;
using System.Timers;

namespace ScreentimeLimiter.Models;

public class ShutdownTimer(uint hours, uint minutes, uint[][]? warnTimes, bool? type) {
    private readonly bool _type = type.HasValue && type.Value;
    private uint _minutesToGo;

    /// <summary>
    /// Once timer for notification goes off this signals to VM to call DisplayNotification
    /// </summary>
    public event Action<string, string>? NotificationRequested;

    /// <summary>
    /// Gets called immediately after constructor - starts the shutdown timing calculation and execution
    /// </summary>
    public void InitiateShutdown() {
        CalculateMinutesToGo();
        CountDownWarnings();
        CountdownMain();
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
            const uint midnight = 24 * 60;
            var timeToMidnight = midnight - curMinutes; // the time it will take us to reach midnight
            _minutesToGo += timeToMidnight;
        }
    }
    
    /// <summary>
    /// Starts countdown for the computer shutdown
    /// </summary>
    private void CountdownMain() {
        var timer = new Timer(TimeSpan.FromMinutes(_minutesToGo));
        timer.Elapsed += (sender, e) => SystemShutdown();
        timer.AutoReset = false; // only fire once
        timer.Start();
    }

    /// <summary>
    /// Starts countdowns for each and every warning
    /// </summary>
    private void CountDownWarnings() {
        if (warnTimes == null) return;
        foreach (var warn in warnTimes) {
            // [1] 1|0 hours | minutes
            var time = warn[0] * (warn[1]==1 ? 60u : 1u);
            // the warning time must not exceed the shutdown time
            // (if it does it would never reach us, therefore it'd be pointless to track it)
            if (time > _minutesToGo) continue;

            var timer = new Timer(TimeSpan.FromMinutes(_minutesToGo - time));
            timer.Elapsed += (sender, e) => SendShutdownNotification(warn[0], warn[1]);
            timer.AutoReset = false;
            timer.Start();
        }
    }

    /// <summary>
    /// Sends notification request to DisplayNotification and provides necessary parameters
    /// </summary>
    /// <param name="duration">in how much time the computer will shutdown</param>
    /// <param name="isHour">whether the duration is in hours (1) or minutes (0)</param>
    private void SendShutdownNotification(uint duration, uint isHour) {
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