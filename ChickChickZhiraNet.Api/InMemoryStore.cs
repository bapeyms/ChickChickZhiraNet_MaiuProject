using System.Text.Json;
using ChickChickZhiraNet.Shared;

namespace ChickChickZhiraNet.Api;

public class InMemoryStore
{
    private readonly List<FoodDto> _foods;
    private readonly List<MealEntryDto> _meals = new();
    private readonly object _lock = new();

    public InMemoryStore()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "foods.json");
        var json = File.ReadAllText(path);
        _foods = JsonSerializer.Deserialize<List<FoodDto>>(
                     json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                 ?? new List<FoodDto>();
    }

    public IReadOnlyList<FoodDto> SearchFoods(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return _foods;

        var q = search.Trim();
        return _foods
            .Where(f => f.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public FoodDto? GetFood(int id) => _foods.FirstOrDefault(f => f.Id == id);

    public MealEntryDto AddMeal(AddMealRequest request, FoodDto food)
    {
        var k = request.Grams / 100.0;

        var entry = new MealEntryDto(
            Id: Guid.NewGuid(),
            Date: request.Date,
            FoodId: food.Id,
            FoodName: food.Name,
            Grams: request.Grams,
            MealType: request.MealType,
            Kcal: Math.Round(food.Kcal * k, 1),
            Protein: Math.Round(food.Protein * k, 1),
            Fat: Math.Round(food.Fat * k, 1),
            Carbs: Math.Round(food.Carbs * k, 1));

        lock (_lock)
        {
            _meals.Add(entry);
        }

        return entry;
    }

    public List<MealEntryDto> GetMeals(DateOnly date)
    {
        lock (_lock)
        {
            return _meals.Where(m => m.Date == date).ToList();
        }
    }

    public bool RemoveMeal(Guid id)
    {
        lock (_lock)
        {
            return _meals.RemoveAll(m => m.Id == id) > 0;
        }
    }
    

    public DailySummaryDto GetSummary(DateOnly date, double? targetKcal)
    {
        var meals = GetMeals(date);

        return new DailySummaryDto(
            Date: date,
            Kcal: Math.Round(meals.Sum(m => m.Kcal), 1),
            Protein: Math.Round(meals.Sum(m => m.Protein), 1),
            Fat: Math.Round(meals.Sum(m => m.Fat), 1),
            Carbs: Math.Round(meals.Sum(m => m.Carbs), 1),
            TargetKcal: targetKcal);
    }
}