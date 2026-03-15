using CommunityToolkit.Mvvm.Input;

namespace AsynAwaitExamples.ViewModels;

// ============================================================================
// STEP 8 VIEWMODEL: EXCEPTION HANDLING IN ASYNC CODE
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. try/catch works normally with await.
// 2. Task.WhenAll and multiple exceptions — only the FIRST is thrown at await.
// 3. The exception call stack is preserved across async method calls.
//
// MVVM NOTE:
// ----------
// Each scenario is a separate async command. Exception handling lives in the
// ViewModel — the View simply binds buttons to commands and output to text.
// ============================================================================

public partial class Step08ViewModel : StepViewModelBase
{
    // ========================================================================
    // SCENARIO 1: Simple try/catch with a single async method.
    // ========================================================================
    [RelayCommand]
    private async Task SingleException()
    {
        Log("--- Scenario 1: Single Task Exception ---\n");

        try
        {
            Log("?? Calling a method that will fail...");
            await FailingOperationAsync("Database connection timeout");
            Log("This will never print.");
        }
        catch (InvalidOperationException ex)
        {
            Log($"?? Caught exception: {ex.Message}");
            Log("? The app didn't crash — we handled it gracefully!\n");
        }
    }

    // ========================================================================
    // SCENARIO 2: Multiple tasks that all fail (Task.WhenAll).
    // ========================================================================
    [RelayCommand]
    private async Task MultipleExceptions()
    {
        Log("--- Scenario 2: Multiple Task Exceptions ---\n");

        Task task1 = FailingOperationAsync("Server A is down");
        Task task2 = FailingOperationAsync("Server B timed out");
        Task task3 = FailingOperationAsync("Server C returned 500");

        Task allTasks = Task.WhenAll(task1, task2, task3);

        try
        {
            await allTasks;
        }
        catch (InvalidOperationException ex)
        {
            Log($"?? First exception caught: {ex.Message}");

            if (allTasks.Exception is not null)
            {
                Log($"\n?? Total exceptions: {allTasks.Exception.InnerExceptions.Count}");
                foreach (var innerEx in allTasks.Exception.InnerExceptions)
                {
                    Log($"   ? {innerEx.Message}");
                }
            }
            Log("");
        }
    }

    // ========================================================================
    // SCENARIO 3: Exception in a chain of async calls.
    // ========================================================================
    [RelayCommand]
    private async Task NestedException()
    {
        Log("--- Scenario 3: Nested Async Exception ---\n");

        try
        {
            Log("?? Calling MethodA ? MethodB ? MethodC (which throws)...");
            await MethodA();
        }
        catch (Exception ex)
        {
            Log($"?? Caught in event handler: {ex.Message}");
            Log($"\n?? Stack trace shows the full call chain:");

            string[] stackLines = ex.StackTrace?.Split('\n') ?? [];
            foreach (string line in stackLines.Take(5))
            {
                string trimmed = line.Trim();
                if (trimmed.Length > 0)
                    Log($"   {trimmed}");
            }
            Log("");
        }
    }

    // --- Helper methods ---

    private static async Task FailingOperationAsync(string errorMessage)
    {
        await Task.Delay(500);
        throw new InvalidOperationException(errorMessage);
    }

    private static async Task MethodA()
    {
        await Task.Delay(200);
        await MethodB();
    }

    private static async Task MethodB()
    {
        await Task.Delay(200);
        await MethodC();
    }

    private static async Task MethodC()
    {
        await Task.Delay(200);
        throw new ApplicationException("?? Something went wrong deep in MethodC!");
    }
}
