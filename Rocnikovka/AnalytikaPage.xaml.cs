using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microcharts;
using SkiaSharp;

namespace Rocnikovka;

public partial class AnalytikaPage : ContentPage
{
    private Database database;

    public AnalytikaPage()
    {
        InitializeComponent();
        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "treninky.db3");
        database = new Database(dbPath);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        NactiSeznamCvikou();
    }

    private async void NactiSeznamCvikou()
    {
        var data = await database.GetTreninkyAsync();
        var unikatniCviky = data.Select(x => x.Nazev).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
        PickerCviky.ItemsSource = unikatniCviky;
    }

    private void OnCvikChanged(object sender, EventArgs e)
    {
        GenerujDvojityGraf();
    }

    private async void GenerujDvojityGraf()
    {
        var vybranyCvik = PickerCviky.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(vybranyCvik)) return;

        SekceGrafu.IsVisible = true;
        LblNazevAktivity.Text = $"Analytické trendy: {vybranyCvik}";

        var data = await database.GetTreninkyAsync();
        var historieCviku = data.Where(x => x.Nazev == vybranyCvik).OrderBy(x => x.Datum).ToList();

        if (historieCviku.Count == 0) return;

        var typSportu = historieCviku.First().Typ;

        var entriesGraf1 = new List<ChartEntry>();
        var entriesGraf2 = new List<ChartEntry>();

        if (typSportu == "Posilovna")
        {
            LblGraf1Titul.Text = "📈 Vývoj maximální zvednuté zátěže (kg)";
            LblGraf2Titul.Text = "📊 Celkový objem práce (Váha × Opakování)";

            foreach (var x in historieCviku)
            {
                if (float.TryParse(x.Vaha, out float vaha) && float.TryParse(x.Opakovani, out float opakovani))
                {
                    string datumStr = x.Datum.ToString("d.M.");

                    entriesGraf1.Add(new ChartEntry(vaha)
                    {
                        Label = datumStr,
                        ValueLabel = $"{vaha} kg",
                        Color = SKColor.Parse("#8B5CF6")
                    });

                    float objem = vaha * opakovani;
                    entriesGraf2.Add(new ChartEntry(objem)
                    {
                        Label = datumStr,
                        ValueLabel = $"{objem} kg",
                        Color = SKColor.Parse("#3B82F6")
                    });
                }
            }
        }
        else // Běh, Kolo, Kardio
        {
            LblGraf1Titul.Text = "🏃 Vývoj překonané vzdálenosti (km)";

            // Pro běh je titul TEMPO, pro kolo necháme rychlost, protože na kole se min/km neměří
            if (typSportu == "Běh")
                LblGraf2Titul.Text = "⏱️ Analýza průměrného tempa běhu (min / km) - NIŽŠÍ JE LEPŠÍ";
            else
                LblGraf2Titul.Text = "⏱️ Analýza průměrné rychlosti (km / hod) - VYŠŠÍ JE LEPŠÍ";

            foreach (var x in historieCviku)
            {
                if (float.TryParse(x.Vzdalenost, out float km) && float.TryParse(x.Cas, out float minuty) && km > 0)
                {
                    string datumStr = x.Datum.ToString("d.M.");

                    // Graf 1: Kilometry
                    entriesGraf1.Add(new ChartEntry(km)
                    {
                        Label = datumStr,
                        ValueLabel = $"{km} km",
                        Color = SKColor.Parse("#10B981")
                    });

                    // Graf 2: Výpočty podle typu sportu
                    if (typSportu == "Běh")
                    {
                        // Výpočet běžeckého tempa: minuty / kilometry (např. 25 min / 5 km = 5.0 min/km)
                        float tempoMinPerKm = minuty / km;

                        entriesGraf2.Add(new ChartEntry(tempoMinPerKm)
                        {
                            Label = datumStr,
                            ValueLabel = $"{tempoMinPerKm:F1} m/km",
                            Color = SKColor.Parse("#E11D48") // Červená pro běžecké tempo
                        });
                    }
                    else // Kolo, Kardio
                    {
                        float rychlost = km / (minuty / 60f);
                        entriesGraf2.Add(new ChartEntry(rychlost)
                        {
                            Label = datumStr,
                            ValueLabel = $"{rychlost:F1} km/h",
                            Color = SKColor.Parse("#F59E0B")
                        });
                    }
                }
            }
        }

        // Vykreslení
        if (entriesGraf1.Count > 0)
        {
            Graf1.Chart = new LineChart
            {
                Entries = entriesGraf1,
                LabelTextSize = 18,
                LineMode = LineMode.Spline,
                PointSize = 8
            };
        }
        else Graf1.Chart = null;

        if (entriesGraf2.Count > 0)
        {
            Graf2.Chart = new LineChart
            {
                Entries = entriesGraf2,
                LabelTextSize = 18,
                LineMode = LineMode.Spline,
                PointSize = 8
            };
        }
        else Graf2.Chart = null;
    }
}