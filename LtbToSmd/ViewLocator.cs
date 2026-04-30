using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using LtbToSmd.Services;
using LtbToSmd.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LtbToSmd
{
    public class ViewLocator : IDataTemplate
    {

        public Control? Build(object? param)
        {
            if (param is null)
                return null;

            var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
            var type = Type.GetType(name);

            if (type != null)
            {
                return (Control)Activator.CreateInstance(type)!;
            }

            var localization = App.Current?.Services?.GetRequiredService<ILocalizationService>();
            var msg = localization?.GetFormatted("viewlocator.not_found", name) ?? "Not Found: " + name;
            return new TextBlock { Text = msg };
        }

        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}
