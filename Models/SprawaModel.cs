using System;

namespace YourNamespace.Models
{
    public class SprawaModel
    {
        public string Znak { get; set; }
        public string Uwagi { get; set; }
        public DateTime DataRejestracji { get; set; }
        public DateTime? DataZakonczenia { get; set; }
        public string Prowadzacy { get; set; }
        public string ImieNazwisko { get; set; }
        public string NazwaPodmiotu { get; set; }
        public int IdPisma { get; set; }
    }

    public class PismoModel
    {
        public int Id { get; set; }
        public string PismoNazwa { get; set; }
        public DateTime DataWplyniecia { get; set; }
        public string NadawcaNazwa { get; set; }
        public string Imie { get; set; }
        public string Nazwisko { get; set; }
    }
}