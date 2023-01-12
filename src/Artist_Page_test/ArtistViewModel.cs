using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Eum.Connections.Spotify.JsonConverters;
using Eum.Connections.Spotify.Models.Artists;
using Refit;

namespace Artist_Page_test
{
    public class ArtistViewModel : INotifyPropertyChanged
    {
        public async void OnNavigatedTo()
        {
            var fullBaseDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "buble.json");
            await using var fs = File.OpenRead(fullBaseDirectory);
            var artist = await JsonSerializer.DeserializeAsync<MercuryArtist>(fs, DefaultOptions.Default);

        }
        public void OnNavigatedFrom()
        {
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
