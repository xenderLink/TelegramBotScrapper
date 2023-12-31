using System.Text.Encodings.Web;
using System.Text.Json;

namespace JsonCrud;
/// <summary>
/// Аналог CRUD-а для JSON. 
/// Простые операции для добавления вакансий, удаления файла с вакансиями
/// и фильтрацией при чтении данных из файла.
/// </summary>
public record Vacancy(string Name, string Url, string City); // модель для вакансии. Рид-онли.

public abstract class JsonVacancy
{
    protected abstract string filePath { get; set; }
    protected static string vacDirectory => Path.Combine(Directory.GetCurrentDirectory()).ToString() + "/JsonCrud/";
    protected string json = string.Empty;

    private JsonSerializerOptions opts;

    protected JsonVacancy()
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
            if (File.Exists(filePath))
            {
                FileInfo fi = new (filePath);

                if (fi.CreationTimeUtc.Date < DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(+5)).DateTime.AddDays(-3).Date) // удалять через три дня
                {
                    fi.Delete();

                    json = JsonSerializer.Serialize(recievedVac, options: opts);

                    using (StreamWriter sw = new (filePath))
                    {
                        await sw.WriteAsync(json);
                    };
                }
                else
                {
                    json = await File.ReadAllTextAsync(filePath);
                    var vacancies = JsonSerializer.Deserialize<Dictionary<string, Vacancy>>(json);

                    foreach (var vacancy in recievedVac)
                    {
                        if (vacancies.ContainsKey(vacancy.Key) is false)
                            vacancies.Add(vacancy.Key, vacancy.Value);                
                    }

                    json = JsonSerializer.Serialize(vacancies, options: opts);

                    using (StreamWriter sw = new (filePath))
                    {
                        await sw.WriteAsync(json);
                    }
                }
            }
            else
            {
                json = JsonSerializer.Serialize(recievedVac, options: opts);

                using (StreamWriter sw = new (filePath))
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
        if (!string.IsNullOrEmpty(city))
        {
            if (File.Exists(filePath))
            {
                List<(string, string)> vacByCity = new ();

                var Json = await File.ReadAllTextAsync(filePath);
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