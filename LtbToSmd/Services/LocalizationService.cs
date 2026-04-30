using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace LtbToSmd.Services;

public class LocalizationService : ILocalizationService
{
    private ConcurrentDictionary<string, string> _translations = new();
    private CultureInfo _currentCulture;

    public event PropertyChangedEventHandler? PropertyChanged;

    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        set
        {
            if (value.Name == _currentCulture.Name) return;
            _currentCulture = value;
            LoadLanguage(value);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentCulture)));
        }
    }

    public LocalizationService()
    {
        var culture = CultureInfo.CurrentCulture.Name.StartsWith("zh") ? "zh-CN" : "en-US";
        _currentCulture = new CultureInfo(culture);
        LoadLanguage(_currentCulture);
    }

    public string this[string key] => _translations.TryGetValue(key, out var value) ? value : $"[{key}]";

    public string GetFormatted(string key, params object?[] args)
    {
        var template = this[key];
        return args.Length > 0 ? string.Format(template, args) : template;
    }

    private void LoadLanguage(CultureInfo culture)
    {
        try
        {
            var basePath = AppContext.BaseDirectory;
            var filePath = Path.Combine(basePath, "Resources", $"lang.{culture.Name}.json");

            if (!File.Exists(filePath))
            {
                // Fallback to en-US
                filePath = Path.Combine(basePath, "Resources", "lang.en-US.json");
                if (!File.Exists(filePath))
                {
                    _translations = new ConcurrentDictionary<string, string>();
                    return;
                }
            }

            var json = File.ReadAllText(filePath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            _translations = dict is not null
                ? new ConcurrentDictionary<string, string>(dict)
                : new ConcurrentDictionary<string, string>();
        }
        catch
        {
            _translations = new ConcurrentDictionary<string, string>();
        }
    }
}
