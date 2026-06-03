using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Rocnikovka;

public partial class MainPage : ContentPage
{
    private Database database;
    private List<string> ListPlanu = new() { "Fullbody split A (Silový)", "Kardio vytrvalostní plán (Běh)" };

    public MainPage()
    {
        InitializeComponent();
        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "treninky.db3");
        database = new Database(dbPath);

        TypSportu.ItemsSource = new List<string> { "Posilovna", "Běh", "Kolo", "Kardio" };
        PickerPlanu.ItemsSource = ListPlanu;
        PickerPlanu.SelectedIndex = 0;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AktualizujVsechnyPickery();
        AktualizujCilePodleLevelu();
    }

    private void OnPlanChanged(object sender, EventArgs e)
    {
        AktualizujCilePodleLevelu();
    }

    private void AktualizujCilePodleLevelu()
    {
        var vybranyPlan = PickerPlanu.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(vybranyPlan)) return;

        int level = Preferences.Default.Get("TreninkLevel", 1);

        if (vybranyPlan.Contains("Silový"))
        {
            float vahaBench = 60f + ((level - 1) * 2.5f);
            float vahaDrep = 80f + ((level - 1) * 2.5f);

            LblPredepsanyPlanText.Text = $"[LEVEL {level}]\n• Benchpress (4 série × 8 opakování, Cíl: {vahaBench} kg)\n• Dřepy (4 série × 10 opakování, Cíl: {vahaDrep} kg)";
            TypSportu.SelectedItem = "Posilovna";
        }
        else if (vybranyPlan.Contains("Běh"))
        {
            float kmLes = 5.0f + ((level - 1) * 0.5f);
            float kmFartlek = 3.0f + ((level - 1) * 0.3f);

            LblPredepsanyPlanText.Text = $"[LEVEL {level}]\n• Běh v lese (Cíl: {kmLes:F1} km, Čas pod: 30 min)\n• Fartlek (Cíl: {kmFartlek:F1} km, Čas pod: 18 min)";
            TypSportu.SelectedItem = "Běh";
        }
    }

    private async void AktualizujVsechnyPickery()
    {
        try
        {
            var data = await database.GetTreninkyAsync();
            var vybranySport = TypSportu.SelectedItem?.ToString();

            if (!string.IsNullOrEmpty(vybranySport))
            {
                var filtrovaneCviky = data
                    .Where(x => x.Typ == vybranySport)
                    .Select(x => x.Nazev)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct()
                    .ToList();

                PickerUlozenychCviku.ItemsSource = filtrovaneCviky;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Chyba", ex.Message, "OK");
        }
    }

    private void OnSportChanged(object sender, EventArgs e)
    {
        var vybranySport = TypSportu.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(vybranySport)) return;

        SekceNazvuCviku.IsVisible = true;

        if (vybranySport == "Posilovna")
        {
            SilovaSekce.IsVisible = true;
            KardioSekce.IsVisible = false;
            Vzdalenost.Text = string.Empty;
            Cas.Text = string.Empty;
        }
        else
        {
            SilovaSekce.IsVisible = false;
            KardioSekce.IsVisible = true;
            Vaha.Text = string.Empty;
            Opakovani.Text = string.Empty;
            Serie.Text = string.Empty;
        }

        AktualizujVsechnyPickery();
    }

    private void OnUlozenyCvikChanged(object sender, EventArgs e)
    {
        if (PickerUlozenychCviku.SelectedItem != null)
        {
            NovyCvikEntry.Text = string.Empty;
            SpustAnalytickyModul(PickerUlozenychCviku.SelectedItem.ToString());
        }
    }

    private async void SpustAnalytickyModul(string nazevCvik)
    {
        if (string.IsNullOrEmpty(nazevCvik)) return;

        var data = await database.GetTreninkyAsync();
        var historieCviku = data.Where(x => x.Nazev == nazevCvik).OrderByDescending(x => x.Datum).ToList();

        if (historieCviku.Count == 0)
        {
            FrameDoporuceni.IsVisible = false;
            return;
        }

        FrameDoporuceni.IsVisible = true;
        var posledniTrenink = historieCviku.First();

        if (posledniTrenink.Typ == "Posilovna" && float.TryParse(posledniTrenink.Vaha, out float posledniVaha))
        {
            float doporucenaVaha = posledniVaha + 2.5f;
            LblDoporuceniText.Text = $"💡 Doporučení: Minule jsi zvládledl {posledniVaha} kg. Pokud splníš předepsané série, zkus dnes {doporucenaVaha} kg.";
        }
        else if (!string.IsNullOrEmpty(posledniTrenink.Vzdalenost) && float.TryParse(posledniTrenink.Cas, out float posledniCas) && float.TryParse(posledniTrenink.Vzdalenost, out float km) && km > 0)
        {
            float stareTempo = posledniCas / km;
            int cileMinuty = (int)stareTempo;
            int cileSekundy = (int)((stareTempo - cileMinuty) * 60);

            LblDoporuceniText.Text = $"💡 Analýza tempa: Minulé tempo bylo {cileMinuty}:{cileSekundy:D2} min/km. Dnes se drž pod tímto časem.";
        }
        else
        {
            FrameDoporuceni.IsVisible = false;
        }
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        string finalniNazev = PickerUlozenychCviku.SelectedItem?.ToString() ?? NovyCvikEntry.Text;

        if (TypSportu.SelectedItem == null || string.IsNullOrEmpty(finalniNazev))
        {
            await DisplayAlert("Upozornění", "Vyplňte sport a název aktivity.", "OK");
            return;
        }

        var t = new Trenink
        {
            Typ = TypSportu.SelectedItem.ToString(),
            Nazev = finalniNazev.Trim(),
            Vaha = Vaha.Text ?? "",
            Opakovani = Opakovani.Text ?? "",
            Serie = Serie.Text ?? "",
            Vzdalenost = Vzdalenost.Text ?? "",
            Cas = Cas.Text ?? "",
            Datum = DateTime.Now,
            PlanNazev = PickerPlanu.SelectedItem?.ToString() ?? "Bez plánu"
        };

        await database.SaveTreninkAsync(t);

        NovyCvikEntry.Text = string.Empty;
        PickerUlozenychCviku.SelectedItem = null;
        Vaha.Text = string.Empty;
        Opakovani.Text = string.Empty;
        Serie.Text = string.Empty;
        Vzdalenost.Text = string.Empty;
        Cas.Text = string.Empty;

        AktualizujVsechnyPickery();
        FrameDoporuceni.IsVisible = false;

        await DisplayAlert("Uloženo", $"Záznam zapsán do plánu: {t.PlanNazev}", "OK");
    }

    private async void OtevriHistorii(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new HistoriePage());
    }

    private async void OtevriAnalytiku(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AnalytikaPage());
    }

    private async void OtevriKontroluPlanu(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new KontrolaPlanuPage());
    }
}