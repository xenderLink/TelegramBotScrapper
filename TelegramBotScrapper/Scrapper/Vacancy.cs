using System.Collections.Generic;

namespace VacancyModel;

public record Vacancy
{
    public string Name { get; init; }
    public string Url { get; init; }
    public string City { get; init; }

    public Vacancy(string name, string url, string city) => (Name, Url, City) = (name, url, city);
}