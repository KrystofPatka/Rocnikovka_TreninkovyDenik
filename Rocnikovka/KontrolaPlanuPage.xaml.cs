using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Rocnikovka;

public partial class KontrolaPlanuPage : ContentPage
{
    private Database database;
    private int aktualniLevel;

    public KontrolaPlanuPage()
    {
        InitializeComponent();
        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "treninky.db3");
        database = new Database(dbPath);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        aktualniLevel = Preferences.Default.Get("TreninkLevel", 1);
        LblAktualniLevel.Text = $"Aktuální úroveň plánu: Level {aktualniLevel}";

        VyhodnotDnesniPlany();
    }

    private async void VyhodnotDnesniPlany()
    {
        var data = await database.GetTreninkyAsync();
        var dnesniData = data.Where(x => x.Datum.Date == DateTime.Today).ToList();
        var reporty = new List<VyhodnoceniReport>();

        float cilovaVahaBench = 60f + ((aktualniLevel - 1) * 2.5f);
        float cilovaVahaDrep = 80f + ((aktualniLevel - 1) * 2.5f);
        float ciloveKmLes = 5.0f + ((aktualniLevel - 1) * 0.5f);
        float ciloveKmFartlek = 3.0f + ((aktualniLevel - 1) * 0.3f);

        bool cvicilDnesNeco = false;

        var bench = dnesniData.FirstOrDefault(x => x.Nazev.ToLower().Contains("bench"));
        if (bench != null)
        {
            cvicilDnesNeco = true;
            int.TryParse(bench.Serie, out int realneSerie);
            float.TryParse(bench.Vaha, out float realnaVaha);

            int procenta = (int)((realneSerie / 4.0f) * 50f + (Math.Min(realnaVaha, cilovaVahaBench) / cilovaVahaBench) * 50f);
            procenta = Math.Min(procenta, 100);

            reporty.Add(new VyhodnoceniReport
            {
                NazevAktivity = "Benchpress (Silový Plán)",
                PlanovanyCil = $"4 série / {cilovaVahaBench} kg",
                RealnyVykon = $"{realneSerie} série / {realnaVaha} kg",
                ProcentaSplneno = procenta
            });
        }

        var drep = dnesniData.FirstOrDefault(x => x.Nazev.ToLower().Contains("dřep") || x.Nazev.ToLower().Contains("drep"));
        if (drep != null)
        {
            cvicilDnesNeco = true;
            int.TryParse(drep.Serie, out int realneSerie);
            float.TryParse(drep.Vaha, out float realnaVaha);

            int procenta = (int)((realneSerie / 4.0f) * 50f + (Math.Min(realnaVaha, cilovaVahaDrep) / cilovaVahaDrep) * 50f);
            procenta = Math.Min(procenta, 100);

            reporty.Add(new VyhodnoceniReport
            {
                NazevAktivity = "Dřepy (Silový Plán)",
                PlanovanyCil = $"4 série / {cilovaVahaDrep} kg",
                RealnyVykon = $"{realneSerie} série / {realnaVaha} kg",
                ProcentaSplneno = procenta
            });
        }

        var behVLese = dnesniData.FirstOrDefault(x => x.Nazev.ToLower().Contains("les"));
        if (behVLese != null)
        {
            cvicilDnesNeco = true;
            float.TryParse(behVLese.Vzdalenost, out float realneKm);

            int procenta = (int)((realneKm / ciloveKmLes) * 100f);
            procenta = Math.Min(procenta, 100);

            reporty.Add(new VyhodnoceniReport
            {
                NazevAktivity = "Běh v lese (Kardio Plán)",
                PlanovanyCil = $"{ciloveKmLes:F1} km",
                RealnyVykon = $"{realneKm} km",
                ProcentaSplneno = procenta
            });
        }

        var fartlek = dnesniData.FirstOrDefault(x => x.Nazev.ToLower().Contains("fartlek"));
        if (fartlek != null)
        {
            cvicilDnesNeco = true;
            float.TryParse(fartlek.Vzdalenost, out float realneKm);

            int procenta = (int)((realneKm / ciloveKmFartlek) * 100f);
            procenta = Math.Min(procenta, 100);

            reporty.Add(new VyhodnoceniReport
            {
                NazevAktivity = "Fartlek (Kardio Plán)",
                PlanovanyCil = $"{ciloveKmFartlek:F1} km",
                RealnyVykon = $"{realneKm} km",
                ProcentaSplneno = procenta
            });
        }

        if (reporty.Count == 0)
        {
            reporty.Add(new VyhodnoceniReport
            {
                NazevAktivity = "Žádná dnešní data",
                PlanovanyCil = "Ulož trénink se zvoleným plánem",
                RealnyVykon = "0",
                ProcentaSplneno = 0
            });
        }

        SeznamVyhodnoceni.ItemsSource = reporty;

        if (cvicilDnesNeco && reporty.All(x => x.ProcentaSplneno == 100))
        {
            FrameProgres.IsVisible = true;
        }
        else
        {
            FrameProgres.IsVisible = false;
        }
    }

    private async void OnZvysitObtiznostClicked(object sender, EventArgs e)
    {
        try
        {
            // 1. Zvýšíme úroveň v paměti zařízení (Preferences)
            aktualniLevel++;
            Preferences.Default.Set("TreninkLevel", aktualniLevel);

            // 2. Načteme dnešní tréninky z databáze
            var vsechnaData = await database.GetTreninkyAsync();
            var dnesniTreninky = vsechnaData.Where(x => x.Datum.Date == DateTime.Today).ToList();

            // 3. Změníme jim datum na včerejšek, aby pro systém už nebyly "dnešní"
            foreach (var trenink in dnesniTreninky)
            {
                trenink.Datum = DateTime.Today.AddDays(-1);
                await database.SaveTreninkAsync(trenink);
            }

            // 4. NATVRDO VYČISTÍME ZOBRAZENÍ NA STRÁNCE
            // Tímto krokem okamžitě smažeme všechny dokončené záznamy z listu na obrazovce
            SeznamVyhodnoceni.ItemsSource = null;

            // 5. Informujeme uživatele vyskakovacím oknem
            await DisplayAlert("🔥 PROGRESSIVE OVERLOAD!",
                $"Skvělá práce! Tvůj plán byl povýšen na Level {aktualniLevel}.\n\nDnešní trénink byl úspěšně dokončen, přesunut do historie a tabulka byla vyčištěna pro novou úroveň!",
                "Přijmout výzvu");

            // 6. Aktualizujeme text s levelem v hlavičce a schováme tlačítko
            LblAktualniLevel.Text = $"Aktuální úroveň plánu: Level {aktualniLevel}";
            FrameProgres.IsVisible = false;

            // 7. Znovu zavoláme vyhodnocení, které teď legálně nenajde žádná dnešní data
            // a vypíše čistý stav: "Žádná dnešní data - Ulož trénink se zvoleným plánem"
            VyhodnotDnesniPlany();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Chyba", $"Nepodařilo se vyčistit stránku: {ex.Message}", "OK");
        }
    }
}

public class VyhodnoceniReport
{
    public string NazevAktivity { get; set; }
    public string PlanovanyCil { get; set; }
    public string RealnyVykon { get; set; }
    public int ProcentaSplneno { get; set; }
    public string BarvaIndikatoru => ProcentaSplneno >= 90 ? "#10B981" : (ProcentaSplneno >= 50 ? "#F59E0B" : "#EF4444");
}