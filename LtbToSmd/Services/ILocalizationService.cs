using System.ComponentModel;
using System.Globalization;

namespace LtbToSmd.Services;

public interface ILocalizationService : INotifyPropertyChanged
{
    CultureInfo CurrentCulture { get; set; }
    string this[string key] { get; }
    string GetFormatted(string key, params object?[] args);
}
