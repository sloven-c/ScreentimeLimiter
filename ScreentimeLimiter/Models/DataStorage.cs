using System;
using System.IO;
using System.Text.Json;

namespace ScreentimeLimiter.Models;

public class DataStorage {
    private readonly string _path;

    public DataStorage() {
        _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ScreentimeLimiter", "data.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
    }

    public struct DataPackage(string hours, string minutes, string warnTimes, bool isChecked) {
        public string Hours = hours;
        public string Minutes = minutes;
        public string WarnTimes = warnTimes;
        public bool IsChecked = isChecked;
    }

    public DataPackage? Read() {
        var content = File.ReadAllText(_path);
        return JsonSerializer.Deserialize<DataPackage>(content);
    }

    public void Save(DataPackage data) {
        var jsonString = JsonSerializer.Serialize(data);
        File.WriteAllText(_path, jsonString);
    }
}