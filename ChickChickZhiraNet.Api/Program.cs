using ChickChickZhiraNet.Api;
using ChickChickZhiraNet.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5000");

builder.Services.AddSingleton<InMemoryStore>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

var today = () => DateOnly.FromDateTime(DateTime.Now);

app.MapGet("/", () => "ChickChickZhiraNet API is running");

var api = app.MapGroup("/api");

api.MapGet("/foods", (string? search, InMemoryStore store) =>
    Results.Ok(store.SearchFoods(search)));

api.MapPost("/meals", (AddMealRequest request, InMemoryStore store) =>
{
    var errors = new Dictionary<string, string[]>();

    if (request.Date == default)
        errors["date"] = new[] { "Date is required (format 2026-11-20)." };

    if (request.Grams <= 0 || request.Grams > 5000)
        errors["grams"] = new[] { "Grams must be greater than 0 and at most 5000." };

    if (!Enum.IsDefined(request.MealType))
        errors["mealType"] = new[] { "Unknown meal type (0-3)." };

    var food = store.GetFood(request.FoodId);
    if (food is null)
        errors["foodId"] = new[] { "Food with this id was not found." };

    if (errors.Count > 0 || food is null)
        return Results.ValidationProblem(errors);

    var entry = store.AddMeal(request, food);
    return Results.Created($"/api/meals/{entry.Id}", entry);
});

api.MapGet("/meals", (DateOnly? date, InMemoryStore store) =>
    Results.Ok(store.GetMeals(date ?? today())));

api.MapDelete("/meals/{id:guid}", (Guid id, InMemoryStore store) =>
    store.RemoveMeal(id) ? Results.NoContent() : Results.NotFound());

api.MapGet("/summary", (
    DateOnly? date,
    double? weightKg,
    double? heightCm,
    int? age,
    Sex? sex,
    double? activity,
    InMemoryStore store) =>
{
    double? target = null;

    if (weightKg is > 0 && heightCm is > 0 && age is > 0 && sex is not null)
    {
        target = CalorieCalculator.Target(
            weightKg.Value, heightCm.Value, age.Value, sex.Value, activity ?? 1.375);
    }

    return Results.Ok(store.GetSummary(date ?? today(), target));
});

app.Run();