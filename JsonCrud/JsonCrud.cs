using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.IO;
using VacancyModel;

namespace JsonCrud;
/// <summary>
/// Аналог CRUD-а для JSON. 
/// Простые операции для добавления вакансий, удаления файла с вакансиями
/// и фильтрацией при чтении данных из файла.
/// </summary>
sealed class JsonVacancy
{
    private readonly string path = @"./JsonCrud/vacancies.json";
    private string json = String.Empty;

    private JsonSerializerOptions opts;

    public JsonVacancy()
    {
        opts = new ()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };
    }

    public async Task Add(Dictionary<string, Vacancy> recievedVac)
    {
        try
        {
            if (File.Exists(path))
            {
                FileInfo fi = new (path);

                if (fi.CreationTimeUtc.Date < DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(+5)).DateTime.AddDays(-3).Date) // удалять через три дня
                {
                    fi.Delete();

                    json = JsonSerializer.Serialize(recievedVac, options: opts);

                    using (StreamWriter sw = new (path))
                    {
                        await sw.WriteAsync(json);
                    };
                }
                else
                {
                    json = await File.ReadAllTextAsync(path);
                    var vacancies = JsonSerializer.Deserialize<Dictionary<string, Vacancy>>(json);

                    foreach (var vacancy in recievedVac)
                    {
                        if (vacancies.ContainsKey(vacancy.Key) == false)
                        {
                            vacancies.Add(vacancy.Key, vacancy.Value);
                        }
                    }

                    json = JsonSerializer.Serialize(vacancies, options: opts);

                    using (StreamWriter sw = new (path))
                    {
                        await sw.WriteAsync(json);
                    }
                }
            }
            else
            {
                json = JsonSerializer.Serialize(recievedVac, options: opts);

                using (StreamWriter sw = new (path))
                {
                    await sw.WriteAsync(json);
                };
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Возникла проблема проблема при работе с json-файлом: {e}");
        }
    }

    public async Task<IReadOnlyList<(string, string)>> GetVacancies(string city)
    {
        if (city is not null)
        {
            if (File.Exists(path))
            {
                List<(string, string)> vacByCity = new ();

                var Json = await File.ReadAllTextAsync(path);
                var vacancies = JsonSerializer.Deserialize<Dictionary<string, Vacancy>>(Json);

                foreach (var vacancy in vacancies)
                {
                    if (vacancy.Value.City.Contains(city))
                    {
                        vacByCity.Add((vacancy.Value.Url, vacancy.Value.Name));
                    }
                }

                return vacByCity;
            }
            
            return new List<(string, string)>();
        }

        return new List<(string, string)>();
    }        
}