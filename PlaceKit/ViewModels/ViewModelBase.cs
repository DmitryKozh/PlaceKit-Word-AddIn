using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PlaceKit.ViewModels
{
    // PropertyChanged.Fody injects the notifications into the property setters.
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
