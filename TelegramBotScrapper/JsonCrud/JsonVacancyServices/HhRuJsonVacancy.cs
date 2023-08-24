namespace JsonCrud;

public sealed class HhRuJsonVacancy : JsonVacancy
{
    protected override string filePath { get; set; } =  vacDirectory + "hh_vacancies.json";

    public HhRuJsonVacancy() : base() {}
}