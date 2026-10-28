using PlaceKit.Models;

namespace PlaceKit.Interfaces
{
    public interface ISettingsService
    {
        string Location { get; }

        PlaceKitSettings Load();

        void Save(PlaceKitSettings settings);
    }
}
