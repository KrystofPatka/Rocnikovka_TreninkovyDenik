using System.IO;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Rocnikovka;

public partial class HistoriePage : ContentPage
{
    private Database database;

    public HistoriePage()
    {
        InitializeComponent();

        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "treninky.db3");
        database = new Database(dbPath);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        NactiHistorii();
    }

    private async void NactiHistorii()
    {
        var data = await database.GetTreninkyAsync();
        // Seøadíme data podle data záznamu (od nejnovìjšího)
        GridHistorie.ItemsSource = data.OrderByDescending(x => x.Datum).ToList();
    }
}