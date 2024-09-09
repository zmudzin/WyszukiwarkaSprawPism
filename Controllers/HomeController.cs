using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using YourNamespace.Models;

namespace YourNamespace.Controllers
{
    public class HomeController : Controller
    {
        public async Task<ActionResult> Sprawy(string searchName, string searchSurname, string searchFirstName, DateTime? startDate, DateTime? endDate)
        {
            // Przekazanie wartości wyszukiwania do widoku
            ViewBag.SearchName = searchName;
            ViewBag.SearchSurname = searchSurname;
            ViewBag.SearchFirstName = searchFirstName;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            // Jeśli brak kryteriów wyszukiwania, nie wykonuj zapytania do bazy danych
            if (string.IsNullOrEmpty(searchName) && string.IsNullOrEmpty(searchSurname) &&
                string.IsNullOrEmpty(searchFirstName) && !startDate.HasValue && !endDate.HasValue)
            {
                return View(new List<SprawaModel>());
            }

            // Jeśli są kryteria, wykonaj zapytanie
            var model = await GetSprawyAsync(searchName, searchSurname, searchFirstName, startDate, endDate);
            return View(model);
        }

        public async Task<ActionResult> WyszukiwarkaPism(string searchDocumentName, string searchSenderName, string searchFirstName, string searchLastName, DateTime? startDate, DateTime? endDate)
        {
            // Przekazanie wartości wyszukiwania do widoku
            ViewBag.SearchDocumentName = searchDocumentName;
            ViewBag.SearchSenderName = searchSenderName;
            ViewBag.SearchFirstName = searchFirstName;
            ViewBag.SearchLastName = searchLastName;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            // Jeśli brak kryteriów wyszukiwania, nie wykonuj zapytania do bazy danych
            if (string.IsNullOrEmpty(searchDocumentName) && string.IsNullOrEmpty(searchSenderName) &&
                string.IsNullOrEmpty(searchFirstName) && string.IsNullOrEmpty(searchLastName) &&
                !startDate.HasValue && !endDate.HasValue)
            {
                return View(new List<PismoModel>());
            }

            // Jeśli są kryteria, wykonaj zapytanie
            var model = await GetPismaAsync(searchDocumentName, searchSenderName, searchFirstName, searchLastName, startDate, endDate);
            return View(model);
        }

        private async Task<List<PismoModel>> GetPismaAsync(string searchDocumentName, string searchSenderName, string searchFirstName, string searchLastName, DateTime? startDate, DateTime? endDate)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["MyDbContext"].ConnectionString;
            var queryBuilder = new StringBuilder(@"
        SELECT a.ID, a.Nazwa AS PismoNazwa, a.Data_Wplyniecia, 
               c.Nazwa AS NadawcaNazwa, c.Imie, c.Nazwisko,
               d.Imie AS WlascicielImie, d.Nazwisko AS WlascicielNazwisko
        FROM dbo.Pisma a 
        JOIN dbo.Dokument b ON a.ID = b.IdPisma
        JOIN dbo.Adresaci c ON b.MetaIdAdresata = c.Id
        JOIN dbo.Pracownicy d ON a.IdPracownikaWlasciciela = d.ID
        WHERE 1=1");

            // Dodajemy warunki do zapytania w zależności od wypełnionych pól
            if (!string.IsNullOrEmpty(searchDocumentName))
                queryBuilder.Append(" AND a.Nazwa LIKE @searchDocumentName");

            if (!string.IsNullOrEmpty(searchSenderName))
                queryBuilder.Append(" AND c.Nazwa LIKE @searchSenderName");

            if (!string.IsNullOrEmpty(searchFirstName))
                queryBuilder.Append(" AND c.Imie LIKE @searchFirstName");

            if (!string.IsNullOrEmpty(searchLastName))
                queryBuilder.Append(" AND c.Nazwisko LIKE @searchLastName");

            if (startDate.HasValue)
                queryBuilder.Append(" AND a.Data_Wplyniecia >= @startDate");

            if (endDate.HasValue)
                queryBuilder.Append(" AND a.Data_Wplyniecia <= @endDate");

            queryBuilder.Append(" GROUP BY a.ID, a.Nazwa, a.Data_Wplyniecia, c.Nazwa, c.Imie, c.Nazwisko, d.Imie, d.Nazwisko");

            var pisma = new List<PismoModel>();

            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(queryBuilder.ToString(), connection))
            {
                // Dodawanie parametrów do zapytania
                if (!string.IsNullOrEmpty(searchDocumentName))
                    command.Parameters.AddWithValue("@searchDocumentName", "%" + searchDocumentName + "%");

                if (!string.IsNullOrEmpty(searchSenderName))
                    command.Parameters.AddWithValue("@searchSenderName", "%" + searchSenderName + "%");

                if (!string.IsNullOrEmpty(searchFirstName))
                    command.Parameters.AddWithValue("@searchFirstName", "%" + searchFirstName + "%");

                if (!string.IsNullOrEmpty(searchLastName))
                    command.Parameters.AddWithValue("@searchLastName", "%" + searchLastName + "%");

                if (startDate.HasValue)
                    command.Parameters.AddWithValue("@startDate", startDate);

                if (endDate.HasValue)
                    command.Parameters.AddWithValue("@endDate", endDate);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var pismo = new PismoModel
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("ID")),
                            PismoNazwa = reader["PismoNazwa"].ToString(),
                            DataWplyniecia = reader.GetDateTime(reader.GetOrdinal("Data_Wplyniecia")),
                            NadawcaNazwa = reader["NadawcaNazwa"].ToString(),
                            Imie = reader["Imie"].ToString(),
                            Nazwisko = reader["Nazwisko"].ToString(),
                            WlascicielImie = reader["WlascicielImie"].ToString(),
                            WlascicielNazwisko = reader["WlascicielNazwisko"].ToString()
                        };
                        pisma.Add(pismo);
                    }
                }
            }

            return pisma;
        }

        private async Task<List<SprawaModel>> GetSprawyAsync(string searchName, string searchSurname, string searchFirstName, DateTime? startDate, DateTime? endDate)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["MyDbContext"].ConnectionString;
            var queryBuilder = new StringBuilder(@"
        SELECT a.Znak, a.Uwagi, a.DataRejestracji, a.DataZakonczenia, 
               d.Imie + ' ' + d.Nazwisko AS Prowadzący, 
               c.Imie + ' ' + c.Nazwisko AS 'Imię Nazwisko', 
               c.Nazwa AS 'Nazwa Podmiotu',
               b.IdPisma
        FROM dbo.SpisSpraw a
        JOIN dbo.Dokument b ON a.IdDokumentuWszczynajacego = b.Id
        JOIN dbo.Adresaci c ON b.MetaIdAdresata = c.Id
        JOIN dbo.Pracownicy d ON a.IdProwadzacy = d.Id
        WHERE 1=1");

            if (!string.IsNullOrEmpty(searchName))
                queryBuilder.Append(" AND c.Nazwa LIKE @searchName");

            if (!string.IsNullOrEmpty(searchSurname))
                queryBuilder.Append(" AND c.Nazwisko LIKE @searchSurname");

            if (!string.IsNullOrEmpty(searchFirstName))
                queryBuilder.Append(" AND c.Imie LIKE @searchFirstName");

            if (startDate.HasValue)
                queryBuilder.Append(" AND a.DataRejestracji >= @startDate");

            if (endDate.HasValue)
                queryBuilder.Append(" AND a.DataRejestracji <= @endDate");

            var sprawy = new List<SprawaModel>();

            try
            {
                using (var connection = new SqlConnection(connectionString))
                using (var command = new SqlCommand(queryBuilder.ToString(), connection))
                {
                    if (!string.IsNullOrEmpty(searchName))
                        command.Parameters.AddWithValue("@searchName", "%" + searchName + "%");

                    if (!string.IsNullOrEmpty(searchSurname))
                        command.Parameters.AddWithValue("@searchSurname", "%" + searchSurname + "%");

                    if (!string.IsNullOrEmpty(searchFirstName))
                        command.Parameters.AddWithValue("@searchFirstName", "%" + searchFirstName + "%");

                    if (startDate.HasValue)
                        command.Parameters.AddWithValue("@startDate", startDate);

                    if (endDate.HasValue)
                        command.Parameters.AddWithValue("@endDate", endDate);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var sprawa = new SprawaModel
                            {
                                Znak = reader["Znak"].ToString(),
                                Uwagi = reader["Uwagi"].ToString(),
                                DataRejestracji = reader.GetDateTime(reader.GetOrdinal("DataRejestracji")),
                                DataZakonczenia = reader.IsDBNull(reader.GetOrdinal("DataZakonczenia"))
                                    ? (DateTime?)null
                                    : reader.GetDateTime(reader.GetOrdinal("DataZakonczenia")),
                                Prowadzacy = reader["Prowadzący"].ToString(),
                                ImieNazwisko = reader["Imię Nazwisko"].ToString(),
                                NazwaPodmiotu = reader["Nazwa Podmiotu"].ToString(),
                                IdPisma = reader.GetInt32(reader.GetOrdinal("IdPisma"))
                            };
                            sprawy.Add(sprawa);
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                // Log the SQL exception details (e.g., using a logging framework)
                // LogException(sqlEx);

                // Optionally, you can re-throw the exception to handle it higher up
                throw new ApplicationException("Wystąpił błąd podczas wykonywania zapytania do bazy danych.", sqlEx);
            }
            catch (Exception ex)
            {
                // Log the general exception details
                // LogException(ex);

                // Optionally, you can re-throw the exception to handle it higher up
                throw new ApplicationException("Wystąpił błąd podczas przetwarzania danych.", ex);
            }

            return sprawy;
        }

    }
}