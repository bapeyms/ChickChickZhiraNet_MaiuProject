using ChickChickZhiraNet.Shared;

namespace ChickChickZhiraNet.Api;

public static class CalorieCalculator
{
    public static double Target(double weightKg, double heightCm, int age, Sex sex, double activity = 1.375)
    {
        var bmr = 10 * weightKg + 6.25 * heightCm - 5 * age + (sex == Sex.Male ? 5 : -161);
        return Math.Round(bmr * activity);
    }
}