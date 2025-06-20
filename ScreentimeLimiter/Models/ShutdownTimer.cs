using System;
using System.Diagnostics;
using System.Timers;

namespace ScreentimeLimiter.Models;

public class ShutdownTimer {
    private readonly uint _hours;
    private readonly uint _minutes;
    private readonly bool _type;
    private readonly uint[][]? _warnTimes;
    private uint _minutesToGo;

    public event Action<string, string>? NotificationRequested;

    public ShutdownTimer(uint hours, uint minutes, bool? type, uint[][]? warnTimes) {
        _hours = hours;
        _minutes = minutes;
        _type = type.HasValue && type.Value;
        _warnTimes = warnTimes;
    }

    public void InitiateShutdown() {
        CalculateMinutesToGo();
        CountDownWarnings();
        CountdownMain();
    }

    private void CalculateMinutesToGo() {
        _minutesToGo = 60 * _hours + _minutes; // this is for relative shutdown (aka shutdown in x hours x minutes)
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
    
    private void CountdownMain() {
        var timer = new Timer(TimeSpan.FromMinutes(_minutesToGo));
        timer.Elapsed += (sender, e) => SystemShutdown();
        timer.AutoReset = false; // only fire once
        timer.Start();
    }

    private void CountDownWarnings() {
        if (_warnTimes == null) return;
        foreach (var warn in _warnTimes) {
            // [1] 1|0 hours | minutes
            var time = warn[0] * (warn[1]==1 ? 60u : 1u);
            // the warning time must not exceed the shutdown time
            // (if it does it would never reach us, therefore it'd be pointless to track it)
            if (time > _minutesToGo) continue;

            var timer = new Timer(TimeSpan.FromMinutes(_minutesToGo - time));
            timer.Elapsed += (sender, e) => SendShutdownNotification(warn[0], warn[1]);
            timer.AutoReset = false;
        }
    }

    private void SendShutdownNotification(uint duration, uint isHour) {
        var unit = isHour == 1 ? "hours" : "minutes"; 
        
        const string title = "Shutdown Warning";
        var text = $"System will shutdown in {duration} {unit}";
        NotificationRequested?.Invoke(title, text);
    }

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