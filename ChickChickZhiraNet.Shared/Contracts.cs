namespace ChickChickZhiraNet.Shared;

public enum MealType
{
    Breakfast = 0,
    Lunch = 1,
    Dinner = 2,
    Snack = 3
}

public enum Sex
{
    Male = 0,
    Female = 1
}

public record FoodDto(
    int Id,
    string Name,
    double Kcal,
    double Protein,
    double Fat,
    double Carbs);

public record AddMealRequest(
    DateOnly Date,
    int FoodId,
    double Grams,
    MealType MealType);

public record MealEntryDto(
    Guid Id,
    DateOnly Date,
    int FoodId,
    string FoodName,
    double Grams,
    MealType MealType,
    double Kcal,
    double Protein,
    double Fat,
    double Carbs);

public record DailySummaryDto(
    DateOnly Date,
    double Kcal,
    double Protein,
    double Fat,
    double Carbs,
    double? TargetKcal);