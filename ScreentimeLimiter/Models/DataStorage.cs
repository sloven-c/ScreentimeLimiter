using System;
using System.IO;
using System.Text.Json;

namespace ScreentimeLimiter.Models;

public class DataStorage {
    private readonly string _path;

    /// <summary>
    /// Constructor for DataStorage class, initialises file path and creates directory
    /// </summary>
    public DataStorage() {
        _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ScreentimeLimiter", "data.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
    }

    /// <summary>
    /// struct that gets converted into json upon saving or vice versa upon loading
    /// </summary>
    /// <param name="hours">hour of shutdown</param>
    /// <param name="minutes">minutes of shutdown</param>
    /// <param name="warnTimes">duration of warning notifications sent ahead of shutdown</param>
    /// <param name="isChecked">whether we are inputting exact (true) or relative time (false)</param>
    public struct DataPackage(uint hours, uint minutes, string warnTimes, bool isChecked) {
        public uint Hours { get; init; } = hours;
        public uint Minutes { get; init; } = minutes;
        public string WarnTimes { get; init; }  = warnTimes;
        public bool IsChecked { get; init; }  = isChecked;
    }

    /// <summary>
    /// Reads the data and returns it within DataPackage? struct
    /// </summary>
    /// <returns>hour, minutes, warn times and if exact toggle button is checked</returns>
    public DataPackage? Read() {
        try {
            var content = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<DataPackage>(content);
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException or JsonException) {
            return null;
        }
    }
    
    /// <summary>
    /// Saves the data into the file
    /// </summary>
    /// <param name="data">DataPackage structure which will be saved in json format</param>
    public void Save(DataPackage data) {
        var jsonString = JsonSerializer.Serialize(data);
        File.WriteAllText(_path, $"{jsonString}\n");
    }
}