using CommunityToolkit.Mvvm.Input;

namespace AsynAwaitExamples.ViewModels;

// ============================================================================
// STEP 3 VIEWMODEL: RETURNING VALUES FROM ASYNC METHODS (Task<T>)
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. Task<T>  - An async method that eventually produces a value of type T.
// 2. "return" in async methods - You just write "return myValue;" like normal.
// 3. "await" unwraps Task<T> - string result = await GetNameAsync();
//
// MVVM NOTE:
// ----------
// The [RelayCommand] attribute generates an AsyncRelayCommand from the
// async Task method. The View binds to the generated FetchDataCommand property.
// No more "async void" event handlers -- the command infrastructure manages
// exception handling and execution state.
// ============================================================================

public partial class Step03ViewModel : StepViewModelBase
{
    /// <summary>
    /// Fetches all simulated data sequentially, demonstrating Task&lt;T&gt; return values.
    /// The [RelayCommand] generates a FetchDataCommand that the View binds to.
    /// </summary>
    [RelayCommand]
    private async Task FetchData()
    {
        Log("[>] Fetching user name...");

        // "await" unwraps Task<string> into string.
        string name = await GetUserNameAsync();
        Log($"   Got name: {name}");

        Log("[>] Fetching user age...");

        // "await" unwraps Task<int> into int.
        int age = await GetUserAgeAsync();
        Log($"   Got age: {age}");

        Log("[>] Fetching temperature...");

        // "await" unwraps Task<double> into double.
        double temp = await GetTemperatureAsync();
        Log($"   Got temperature: {temp} C");

        Log($"\n[OK] Summary: {name}, age {age}, current temp {temp} C\n");
    }

    // ========================================================================
    // ASYNC METHODS THAT RETURN VALUES
    // ========================================================================

    private static async Task<string> GetUserNameAsync()
    {
        await Task.Delay(1000);
        return "Alice";
    }

    private static async Task<int> GetUserAgeAsync()
    {
        await Task.Delay(1000);
        return 30;
    }

    private static async Task<double> GetTemperatureAsync()
    {
        await Task.Delay(1000);
        return 22.5;
    }
}
