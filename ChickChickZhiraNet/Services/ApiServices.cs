using System.Globalization;
using System.Net.Http.Json;
using ChickChickZhiraNet.Shared;

namespace ChickChickZhiraNet.Services;

public class ApiServices
{
    private readonly HttpClient _httpClient = new HttpClient();

    // Search products by name.
    public async Task<List<FoodDto>> SearchFoodsAsync(
        string baseAddress,
        string searchText)
    {
        string url = baseAddress.TrimEnd('/') + "/api/foods";

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            string encodedSearch = Uri.EscapeDataString(searchText);
            url += "?search=" + encodedSearch;
        }

        using HttpResponseMessage response =
            await _httpClient.GetAsync(url);

        await CheckResponseAsync(response);

        List<FoodDto>? foods =
            await response.Content.ReadFromJsonAsync<List<FoodDto>>();

        return foods ?? new List<FoodDto>();
    }

    // Get meal entries for one date.
    public async Task<List<MealEntryDto>> GetMealsAsync(
        string baseAddress,
        DateOnly date)
    {
        string dateText =
            date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        string url = baseAddress.TrimEnd('/')
            + "/api/meals?date="
            + dateText;

        using HttpResponseMessage response =
            await _httpClient.GetAsync(url);

        await CheckResponseAsync(response);

        List<MealEntryDto>? meals =
            await response.Content.ReadFromJsonAsync<List<MealEntryDto>>();

        return meals ?? new List<MealEntryDto>();
    }

    // Get the daily calorie and nutrition summary.
    public async Task<DailySummaryDto> GetSummaryAsync(
        string baseAddress,
        DateOnly date,
        double? weightKg,
        double? heightCm,
        int? age,
        int? sex,
        double activity)
    {
        string dateText =
            date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        string url = baseAddress.TrimEnd('/')
            + "/api/summary?date="
            + dateText;

        // Send profile data only when all required profile fields are filled in.
        if (weightKg.HasValue
            && heightCm.HasValue
            && age.HasValue
            && sex.HasValue)
        {
            url += "&weightKg="
                + weightKg.Value.ToString(CultureInfo.InvariantCulture);

            url += "&heightCm="
                + heightCm.Value.ToString(CultureInfo.InvariantCulture);

            url += "&age="
                + age.Value.ToString(CultureInfo.InvariantCulture);

            // The backend uses 0 for Male and 1 for Female.
            url += "&sex="
                + sex.Value.ToString(CultureInfo.InvariantCulture);
        }

        url += "&activity="
            + activity.ToString(CultureInfo.InvariantCulture);

        using HttpResponseMessage response =
            await _httpClient.GetAsync(url);

        await CheckResponseAsync(response);

        DailySummaryDto? summary =
            await response.Content.ReadFromJsonAsync<DailySummaryDto>();

        if (summary == null)
        {
            throw new Exception("The backend returned an empty summary.");
        }

        return summary;
    }

    // Send a new food entry to the backend.
    public async Task<MealEntryDto> AddMealAsync(
        string baseAddress,
        AddMealRequest request)
    {
        string url = baseAddress.TrimEnd('/') + "/api/meals";

        using HttpResponseMessage response =
            await _httpClient.PostAsJsonAsync(url, request);

        await CheckResponseAsync(response);

        MealEntryDto? addedMeal =
            await response.Content.ReadFromJsonAsync<MealEntryDto>();

        if (addedMeal == null)
        {
            throw new Exception("The backend did not return the added meal.");
        }

        return addedMeal;
    }

    // Delete a food entry by its ID.
    public async Task DeleteMealAsync(
        string baseAddress,
        Guid mealId)
    {
        string url = baseAddress.TrimEnd('/')
            + "/api/meals/"
            + mealId;

        using HttpResponseMessage response =
            await _httpClient.DeleteAsync(url);

        await CheckResponseAsync(response);
    }

    // Stop and show the message if the backend reports an error.
    private async Task CheckResponseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string errorMessage =
            await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            errorMessage = "The backend returned an error.";
        }

        throw new Exception(errorMessage);
    }
}