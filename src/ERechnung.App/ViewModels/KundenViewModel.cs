using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using ERechnung.Core.Models;
using ERechnung.Core.Services;

namespace ERechnung.App.ViewModels;

public class KundenViewModel : INotifyPropertyChanged
{
    private readonly IRepository<Kunde> _kundeRepo;

    public ObservableCollection<Kunde> KundenListe { get; } = new();
    public string Suchbegriff { get; set; } = "";

    public KundenViewModel(IRepository<Kunde> kundeRepo)
    {
        _kundeRepo = kundeRepo;
        Task.Run(LadeKundenAsync);
    }

    private async Task LadeKundenAsync()
    {
        var kunden = await _kundeRepo.GetAllAsync();
        Application.Current.Dispatcher.Invoke(() =>
        {
            KundenListe.Clear();
            foreach (var k in kunden)
                KundenListe.Add(k);
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}