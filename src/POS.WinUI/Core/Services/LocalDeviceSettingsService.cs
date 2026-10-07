using System;
using System.IO;
using System.Text.Json;

namespace POS.WinUI.Core.Services;

public class DeviceSettingsData
{
    public string? StoreId { get; set; }
    public string? StoreName { get; set; }
    public string? RegisterId { get; set; }
    public string? RegisterName { get; set; }
    public DateTime? LastUpdated { get; set; }
}

public sealed class LocalDeviceSettingsService
{
    private readonly string _settingsFilePath;
    private DeviceSettingsData _settings = new();

    public LocalDeviceSettingsService()
    {
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OraPOS");

        if (!Directory.Exists(appDataDir))
        {
            Directory.CreateDirectory(appDataDir);
        }

        _settingsFilePath = Path.Combine(appDataDir, "device_settings.json");
        LoadSettings();
    }

    public string? SavedStoreId => _settings.StoreId;
    public string? SavedStoreName => _settings.StoreName;
    public string? SavedRegisterId => _settings.RegisterId;
    public string? SavedRegisterName => _settings.RegisterName;

    public void SaveDeviceBinding(string storeId, string? storeName, string registerId, string? registerName)
    {
        _settings.StoreId = storeId;
        _settings.StoreName = storeName;
        _settings.RegisterId = registerId;
        _settings.RegisterName = registerName;
        _settings.LastUpdated = DateTime.UtcNow;

        PersistSettings();
    }

    public void SaveRegisterBinding(string registerId, string? registerName)
    {
        _settings.RegisterId = registerId;
        _settings.RegisterName = registerName;
        _settings.LastUpdated = DateTime.UtcNow;

        PersistSettings();
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<DeviceSettingsData>(json);
                if (loaded != null)
                {
                    _settings = loaded;
                }
            }
        }
        catch
        {
            _settings = new DeviceSettingsData();
        }
    }

    private void PersistSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch
        {
            // Tránh crash nếu không ghi được file
        }
    }
}
