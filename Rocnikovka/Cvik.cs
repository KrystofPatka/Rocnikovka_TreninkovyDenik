using SQLite;

namespace Rocnikovka;

public class Cvik
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Nazev { get; set; } = "";
    public string Typ { get; set; } = "";
}