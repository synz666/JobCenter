using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseStaticFiles();

string dataFile = "data.json";
DataStore store;
if (File.Exists(dataFile))
{
    var json = File.ReadAllText(dataFile);
    store = JsonSerializer.Deserialize<DataStore>(json) ?? new DataStore();
}
else
{
    store = new DataStore();
}

app.MapGet("/", async context =>
{
    await context.Response.WriteAsync(HtmlPage());
});

app.MapGet("/seekers", async context =>
{
    await context.Response.WriteAsync(HtmlSeekers(store));
});

app.MapGet("/vacancies", async context =>
{
    await context.Response.WriteAsync(HtmlVacancies(store));
});

app.MapPost("/addseeker", async context =>
{
    var form = await context.Request.ReadFormAsync();
    var name = form["name"];
    var age = int.TryParse(form["age"], out var a) ? a : 0;
    var skills = form["skills"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    store.JobSeekers.Add(new JobSeeker { Id = Guid.NewGuid().ToString(), Name = name, Age = age, Skills = skills });
    Save();
    context.Response.Redirect("/seekers");
});

app.MapPost("/addvacancy", async context =>
{
    var form = await context.Request.ReadFormAsync();
    var title = form["title"];
    var desc = form["desc"];
    var skills = form["skills"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    var count = int.TryParse(form["count"], out var c) ? c : 1;
    store.Vacancies.Add(new Vacancy { Id = Guid.NewGuid().ToString(), Title = title, Description = desc, RequiredSkills = skills, OpenPositions = count });
    Save();
    context.Response.Redirect("/vacancies");
});

app.MapGet("/matches", async context =>
{
    var matches = FindMatches();
    await context.Response.WriteAsync(HtmlMatches(matches));
});

void Save()
{
    var json = JsonSerializer.Serialize(store, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(dataFile, json);
}

Dictionary<JobSeeker, List<Vacancy>> FindMatches()
{
    var res = new Dictionary<JobSeeker, List<Vacancy>>();
    foreach (var j in store.JobSeekers)
    {
        var suitable = store.Vacancies.Where(v => v.RequiredSkills.Any(rs => j.Skills.Contains(rs, StringComparer.OrdinalIgnoreCase))).ToList();
        if (suitable.Any()) res[j] = suitable;
    }
    return res;
}

string HtmlPage() => """
<html lang='uk'>
<head><meta charset='utf-8'><title>Центр зайнятості</title></head>
<body style='font-family:Arial'>
<h2>Автоматизована інформаційна система для центру зайнятості</h2>
<ul>
<li><a href='/seekers'>Шукачі роботи</a></li>
<li><a href='/vacancies'>Вакансії</a></li>
<li><a href='/matches'>Відповідності</a></li>
</ul>
</body></html>
""";

string HtmlSeekers(DataStore s)
{
    var rows = string.Join("", s.JobSeekers.Select(j => $"<tr><td>{j.Name}</td><td>{j.Age}</td><td>{string.Join(", ", j.Skills)}</td></tr>"));
    return $@"
<html lang='uk'><head><meta charset='utf-8'><title>Шукачі роботи</title></head>
<body><h3>Шукачі роботи</h3>
<table border='1' cellpadding='4'><tr><th>Ім'я</th><th>Вік</th><th>Навички</th></tr>{rows}</table>
<h4>Додати шукача:</h4>
<form method='post' action='/addseeker'>
Ім'я: <input name='name'><br>
Вік: <input name='age'><br>
Навички (через кому): <input name='skills'><br>
<button type='submit'>Додати</button>
</form>
<a href='/'>На головну</a>
</body></html>";
}

string HtmlVacancies(DataStore s)
{
    var rows = string.Join("", s.Vacancies.Select(v => $"<tr><td>{v.Title}</td><td>{v.Description}</td><td>{string.Join(", ", v.RequiredSkills)}</td><td>{v.OpenPositions}</td></tr>"));
    return $@"
<html lang='uk'><head><meta charset='utf-8'><title>Вакансії</title></head>
<body><h3>Вакансії</h3>
<table border='1' cellpadding='4'><tr><th>Назва</th><th>Опис</th><th>Навички</th><th>Кількість</th></tr>{rows}</table>
<h4>Додати вакансію:</h4>
<form method='post' action='/addvacancy'>
Назва: <input name='title'><br>
Опис: <input name='desc'><br>
Навички (через кому): <input name='skills'><br>
Кількість: <input name='count' value='1'><br>
<button type='submit'>Додати</button>
</form>
<a href='/'>На головну</a>
</body></html>";
}

string HtmlMatches(Dictionary<JobSeeker, List<Vacancy>> matches)
{
    if (!matches.Any()) return "<html><body><h3>Відповідностей не знайдено.</h3><a href='/'>На головну</a></body></html>";
    var html = "<html lang='uk'><head><meta charset='utf-8'><title>Відповідності</title></head><body><h3>Відповідності</h3>";
    foreach (var kv in matches)
    {
        html += $"<b>{kv.Key.Name}</b> ({string.Join(", ", kv.Key.Skills)})<ul>";
        foreach (var v in kv.Value)
            html += $"<li>{v.Title} — {string.Join(", ", v.RequiredSkills)}</li>";
        html += "</ul>";
    }
    html += "<a href='/'>На головну</a></body></html>";
    return html;
}

app.Run();

public class JobSeeker
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public List<string> Skills { get; set; } = new();
}

public class Vacancy
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> RequiredSkills { get; set; } = new();
    public int OpenPositions { get; set; } = 1;
}

public class DataStore
{
    public List<JobSeeker> JobSeekers { get; set; } = new();
    public List<Vacancy> Vacancies { get; set; } = new();
}
