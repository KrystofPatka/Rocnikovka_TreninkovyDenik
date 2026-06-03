using SQLite;
using System;

namespace Rocnikovka;

public class Trenink
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Typ { get; set; } // Posilovna, Běh...
    public string Nazev { get; set; } // Benchpress, Fartlek...
    public string Vaha { get; set; }
    public string Opakovani { get; set; }
    public string Serie { get; set; } // <--- TENTO ŘÁDEK PŘIDEJ / UPRAV
    public string Vzdalenost { get; set; }
    public string Cas { get; set; }
    public DateTime Datum { get; set; }
    public string PlanNazev { get; set; }
}