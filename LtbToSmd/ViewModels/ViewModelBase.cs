using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace LtbToSmd.ViewModels
{
    public partial class ViewModelBase : ObservableObject
    {
        protected ViewModelBase()
        {
            ErrorMessages = new ObservableCollection<string>();
        }

        [ObservableProperty]
        private ObservableCollection<string>? _errorMessages;
    }
}
