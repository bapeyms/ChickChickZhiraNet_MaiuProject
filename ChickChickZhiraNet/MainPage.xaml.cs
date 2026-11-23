using System.Collections.ObjectModel;
using System.Globalization;
using ChickChickZhiraNet.Services;
using ChickChickZhiraNet.Shared;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;

namespace ChickChickZhiraNet;

public partial class MainPage : ContentPage
{
    private const string CalorieGoalPreferenceKey = "daily-calorie-goal";

    private readonly ApiServices api = new();
    private FoodDto? selectedFood;
    private MealEntryDto? pendingUndoMeal;
    private double? dailyCalorieGoal;
    private double currentCalories;
    private int foodSearchVersion;
    private int undoVersion;
    private bool? dateToolbarIsCompact;

#if ANDROID
    private const string ApiAddress = "http://10.0.2.2:5000";
#else
    private const string ApiAddress = "http://127.0.0.1:5000";
#endif

    public ObservableCollection<FoodDto> Foods { get; } = new();
    public ObservableCollection<MealEntryDto> Meals { get; } = new();

    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;

        MealTypePicker.Items.Add("Breakfast");
        MealTypePicker.Items.Add("Lunch");
        MealTypePicker.Items.Add("Dinner");
        MealTypePicker.Items.Add("Snack");
        MealTypePicker.SelectedIndex = 0;

        double savedGoal = Preferences.Default.Get(
            CalorieGoalPreferenceKey,
            0d);

        if (savedGoal > 0)
        {
            dailyCalorieGoal = savedGoal;
            CalorieGoalEntry.Text = savedGoal.ToString(
                "0",
                CultureInfo.CurrentCulture);
        }

        UpdateAddButtonState();
        UpdateDateNavigation();
        LoadPage();
    }

    private async void LoadPage()
    {
        StatusLabel.Text = "Loading data…";

        try
        {
            await LoadFoods(FoodSearchBar.Text);
            await LoadDay();
            StatusLabel.Text = "Data updated";
        }
        catch (Exception error)
        {
            ShowError(error);
        }
    }

    private async Task LoadFoods(
        string? searchText,
        bool debounce = false)
    {
        int requestVersion = ++foodSearchVersion;

        FoodRetryButton.IsVisible = false;
        FoodListMessageLabel.Text = "";

        if (debounce)
        {
            await Task.Delay(300);

            if (requestVersion != foodSearchVersion)
            {
                return;
            }
        }

        FoodLoadingIndicator.IsVisible = true;
        FoodLoadingIndicator.IsRunning = true;

        try
        {
            List<FoodDto> foods =
                await api.SearchFoodsAsync(ApiAddress, searchText ?? "");

            if (requestVersion != foodSearchVersion)
            {
                return;
            }

            Foods.Clear();

            foreach (FoodDto food in foods)
            {
                Foods.Add(food);
            }

            FoodListMessageLabel.Text = Foods.Count == 0
                ? "No foods found. Try another search."
                : "";
        }
        catch
        {
            if (requestVersion == foodSearchVersion)
            {
                Foods.Clear();
                FoodListMessageLabel.Text =
                    "Could not load foods. Check the connection and try again.";
                FoodRetryButton.IsVisible = true;
            }
        }
        finally
        {
            if (requestVersion == foodSearchVersion)
            {
                FoodLoadingIndicator.IsRunning = false;
                FoodLoadingIndicator.IsVisible = false;
            }
        }
    }

    private async Task LoadDay()
    {
        DateOnly date = GetSelectedDate();

        List<MealEntryDto> meals =
            await api.GetMealsAsync(ApiAddress, date);

        Meals.Clear();

        foreach (MealEntryDto meal in meals)
        {
            Meals.Add(meal);
        }

        MealsEmptyLabel.IsVisible = Meals.Count == 0;

        DailySummaryDto summary = await api.GetSummaryAsync(
            ApiAddress,
            date,
            null,
            null,
            null,
            null,
            1.375);

        currentCalories = summary.Kcal;

        KcalValueLabel.Text = $"{summary.Kcal:0} kcal";
        ProteinValueLabel.Text = $"{summary.Protein:0.#} g";
        FatValueLabel.Text = $"{summary.Fat:0.#} g";
        CarbsValueLabel.Text = $"{summary.Carbs:0.#} g";

        UpdateCalorieGoalDisplay();
    }

    private void UpdateCalorieGoalDisplay()
    {
        if (!dailyCalorieGoal.HasValue)
        {
            CalorieGoalProgressBar.IsVisible = false;
            TargetKcalLabel.Text = "No daily goal set";
            return;
        }

        double goal = dailyCalorieGoal.Value;
        double progress = Math.Clamp(currentCalories / goal, 0, 1);

        CalorieGoalProgressBar.Progress = progress;
        CalorieGoalProgressBar.IsVisible = true;

        double remaining = goal - currentCalories;

        TargetKcalLabel.Text = remaining >= 0
            ? $"Daily goal: {goal:0} kcal · {remaining:0} kcal left"
            : $"Daily goal exceeded by {Math.Abs(remaining):0} kcal";
    }

    private async void OnDateSelected(
        object? sender,
        DateChangedEventArgs e)
    {
        UpdateDateNavigation();

        try
        {
            await LoadDay();
            StatusLabel.Text = "Data updated";
        }
        catch (Exception error)
        {
            ShowError(error);
        }
    }

    private void OnDateToolbarSizeChanged(
        object? sender,
        EventArgs e)
    {
        bool shouldUseCompactLayout = DateToolbar.Width < 350;

        if (dateToolbarIsCompact == shouldUseCompactLayout)
        {
            return;
        }

        dateToolbarIsCompact = shouldUseCompactLayout;
        DateToolbar.ColumnDefinitions.Clear();
        DateToolbar.RowDefinitions.Clear();

        if (shouldUseCompactLayout)
        {
            DateToolbar.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Auto));
            DateToolbar.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));
            DateToolbar.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Auto));

            DateToolbar.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));
            DateToolbar.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            Grid.SetRow(PreviousDateButton, 0);
            Grid.SetColumn(PreviousDateButton, 0);

            Grid.SetRow(DatePickerBorder, 0);
            Grid.SetColumn(DatePickerBorder, 1);

            Grid.SetRow(NextDateButton, 0);
            Grid.SetColumn(NextDateButton, 2);

            Grid.SetRow(TodayDateButton, 1);
            Grid.SetColumn(TodayDateButton, 1);
            TodayDateButton.HorizontalOptions = LayoutOptions.Center;
        }
        else
        {
            DateToolbar.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Auto));
            DateToolbar.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));
            DateToolbar.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Auto));
            DateToolbar.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Auto));

            DateToolbar.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            Grid.SetRow(PreviousDateButton, 0);
            Grid.SetColumn(PreviousDateButton, 0);

            Grid.SetRow(DatePickerBorder, 0);
            Grid.SetColumn(DatePickerBorder, 1);

            Grid.SetRow(NextDateButton, 0);
            Grid.SetColumn(NextDateButton, 2);

            Grid.SetRow(TodayDateButton, 0);
            Grid.SetColumn(TodayDateButton, 3);
            TodayDateButton.HorizontalOptions = LayoutOptions.Fill;
        }
    }

    private void OnPreviousDateClicked(
        object? sender,
        EventArgs e)
    {
        DateTime currentDate = SelectedDatePicker.Date ?? DateTime.Today;
        SelectedDatePicker.Date = currentDate.AddDays(-1);
    }

    private void OnNextDateClicked(
        object? sender,
        EventArgs e)
    {
        DateTime currentDate = SelectedDatePicker.Date ?? DateTime.Today;

        if (currentDate.Date < DateTime.Today)
        {
            SelectedDatePicker.Date = currentDate.AddDays(1);
        }
    }

    private async void OnTodayDateClicked(
        object? sender,
        EventArgs e)
    {
        DateTime today = DateTime.Today;
        DateTime selectedDate = SelectedDatePicker.Date ?? today;

        if (selectedDate.Date != today)
        {
            SelectedDatePicker.Date = today;
            return;
        }

        try
        {
            await LoadDay();
            StatusLabel.Text = "Showing today's meals";
        }
        catch (Exception error)
        {
            ShowError(error);
        }
    }

    private void UpdateDateNavigation()
    {
        DateTime selectedDate =
            SelectedDatePicker.Date ?? DateTime.Today;

        NextDateButton.IsEnabled = selectedDate.Date < DateTime.Today;
    }

    private async void OnFoodSearchFocused(
        object? sender,
        FocusEventArgs e)
    {
        FoodResultsBorder.IsVisible = true;

        if (Foods.Count == 0)
        {
            await LoadFoods(FoodSearchBar.Text);
        }
    }

    private async void OnFoodSearchTextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        FoodResultsBorder.IsVisible = true;
        await LoadFoods(e.NewTextValue, debounce: true);
    }

    private async void OnFoodRetryClicked(
        object? sender,
        EventArgs e)
    {
        await LoadFoods(FoodSearchBar.Text);
    }

    private void OnFoodSelected(object? sender, EventArgs e)
    {
        Button button = (Button)sender!;

        if (button.CommandParameter is FoodDto food)
        {
            selectedFood = food;
            SelectedFoodLabel.Text = $"Selected food: {food.Name}";
            StatusLabel.Text = "Enter the portion weight and meal type";

            FoodResultsBorder.IsVisible = false;
            FoodSearchBar.Unfocus();

            UpdateAddButtonState();
        }
    }

    private void OnGramsTextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        bool isValid = TryGetGrams(out _);

        WeightValidationLabel.IsVisible = !isValid
            && !string.IsNullOrWhiteSpace(GramsEntry.Text);

        UpdateAddButtonState();
    }

    private void UpdateAddButtonState()
    {
        AddMealButton.IsEnabled = selectedFood != null
            && TryGetGrams(out _)
            && MealTypePicker.SelectedIndex >= 0;
    }

    private bool TryGetGrams(out double grams)
    {
        bool parsed = double.TryParse(
            GramsEntry.Text,
            NumberStyles.Number,
            CultureInfo.CurrentCulture,
            out grams);

        return parsed && grams >= 1 && grams <= 5000;
    }

    private async void OnAddMealClicked(
        object? sender,
        EventArgs e)
    {
        if (selectedFood == null)
        {
            StatusLabel.Text = "Select a food first";
            return;
        }

        if (!TryGetGrams(out double grams))
        {
            WeightValidationLabel.IsVisible = true;
            StatusLabel.Text = "Please correct the portion weight.";
            return;
        }

        if (MealTypePicker.SelectedIndex < 0)
        {
            StatusLabel.Text = "Select a meal type";
            return;
        }

        AddMealRequest request = new(
            GetSelectedDate(),
            selectedFood.Id,
            grams,
            (MealType)MealTypePicker.SelectedIndex);

        AddMealButton.IsEnabled = false;

        try
        {
            await api.AddMealAsync(ApiAddress, request);

            GramsEntry.Unfocus();
            GramsEntry.Text = "";
            WeightValidationLabel.IsVisible = false;

            await LoadDay();
            StatusLabel.Text = "Meal added";
        }
        catch (Exception error)
        {
            ShowError(error);
        }
        finally
        {
            UpdateAddButtonState();
        }
    }

    private async void OnDeleteMealClicked(
        object? sender,
        EventArgs e)
    {
        Button button = (Button)sender!;

        if (button.CommandParameter is not Guid mealId)
        {
            return;
        }

        MealEntryDto? meal =
            Meals.FirstOrDefault(item => item.Id == mealId);

        if (meal == null)
        {
            return;
        }

        try
        {
            await api.DeleteMealAsync(ApiAddress, mealId);

            pendingUndoMeal = meal;
            await LoadDay();

            UndoMessageLabel.Text = $"{meal.FoodName} deleted";
            UndoBar.IsVisible = true;

            int currentUndoVersion = ++undoVersion;
            _ = HideUndoAfterDelay(currentUndoVersion);

            StatusLabel.Text = "Meal entry deleted";
        }
        catch (Exception error)
        {
            ShowError(error);
        }
    }

    private async Task HideUndoAfterDelay(int version)
    {
        await Task.Delay(TimeSpan.FromSeconds(6));

        if (version == undoVersion)
        {
            UndoBar.IsVisible = false;
            pendingUndoMeal = null;
        }
    }

    private async void OnUndoDeleteClicked(
        object? sender,
        EventArgs e)
    {
        if (pendingUndoMeal is not MealEntryDto meal)
        {
            return;
        }

        pendingUndoMeal = null;
        undoVersion++;
        UndoBar.IsVisible = false;

        AddMealRequest request = new(
            meal.Date,
            meal.FoodId,
            meal.Grams,
            meal.MealType);

        try
        {
            await api.AddMealAsync(ApiAddress, request);
            await LoadDay();
            StatusLabel.Text = "Meal restored";
        }
        catch (Exception error)
        {
            ShowError(error);
        }
    }

    private void OnSaveCalorieGoalClicked(
        object? sender,
        EventArgs e)
    {
        bool isValid = double.TryParse(
            CalorieGoalEntry.Text,
            NumberStyles.Number,
            CultureInfo.CurrentCulture,
            out double goal);

        if (!isValid || goal < 500 || goal > 10000)
        {
            CalorieGoalErrorLabel.IsVisible = true;
            return;
        }

        CalorieGoalErrorLabel.IsVisible = false;
        dailyCalorieGoal = goal;

        Preferences.Default.Set(
            CalorieGoalPreferenceKey,
            goal);

        UpdateCalorieGoalDisplay();
        StatusLabel.Text = "Daily goal saved";
        CalorieGoalEntry.Unfocus();
    }

    private void OnResponsiveGridSizeChanged(
        object? sender,
        EventArgs e)
    {
        bool isWide = ResponsiveGrid.Width >= 760;
        bool isAlreadyWide = ResponsiveGrid.ColumnDefinitions.Count == 2;

        if (isWide == isAlreadyWide)
        {
            return;
        }

        ResponsiveGrid.ColumnDefinitions.Clear();
        ResponsiveGrid.RowDefinitions.Clear();

        if (isWide)
        {
            ResponsiveGrid.ColumnDefinitions.Add(
                new ColumnDefinition(new GridLength(1.2, GridUnitType.Star)));

            ResponsiveGrid.ColumnDefinitions.Add(
                new ColumnDefinition(new GridLength(1, GridUnitType.Star)));

            ResponsiveGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            Grid.SetRow(MainColumn, 0);
            Grid.SetColumn(MainColumn, 0);
            Grid.SetRow(MealsColumn, 0);
            Grid.SetColumn(MealsColumn, 1);
        }
        else
        {
            ResponsiveGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));

            ResponsiveGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            ResponsiveGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            Grid.SetRow(MainColumn, 0);
            Grid.SetColumn(MainColumn, 0);
            Grid.SetRow(MealsColumn, 1);
            Grid.SetColumn(MealsColumn, 0);
        }
    }

    private DateOnly GetSelectedDate()
    {
        DateTime date = SelectedDatePicker.Date ?? DateTime.Today;
        return DateOnly.FromDateTime(date);
    }

    private void ShowError(Exception error)
    {
        StatusLabel.Text = $"Backend error: {error.Message}";
    }
}

public class MealTypeTextConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is not MealType mealType)
        {
            return "";
        }

        return mealType switch
        {
            MealType.Breakfast => "Breakfast",
            MealType.Lunch => "Lunch",
            MealType.Dinner => "Dinner",
            MealType.Snack => "Snack",
            _ => "Other"
        };
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class MealTypeBackgroundConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is not MealType mealType)
        {
            return Color.FromArgb("#F5F6F1");
        }

        return mealType switch
        {
            MealType.Breakfast => Color.FromArgb("#E8F2EB"),
            MealType.Lunch => Color.FromArgb("#F5F6F1"),
            MealType.Dinner => Color.FromArgb("#E3E8E2"),
            MealType.Snack => Colors.White,
            _ => Color.FromArgb("#F5F6F1")
        };
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class MealTypeAccentConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is not MealType mealType)
        {
            return Color.FromArgb("#397553");
        }

        return mealType switch
        {
            MealType.Breakfast => Color.FromArgb("#397553"),
            MealType.Lunch => Color.FromArgb("#68746C"),
            MealType.Dinner => Color.FromArgb("#20352A"),
            MealType.Snack => Color.FromArgb("#68746C"),
            _ => Color.FromArgb("#397553")
        };
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}