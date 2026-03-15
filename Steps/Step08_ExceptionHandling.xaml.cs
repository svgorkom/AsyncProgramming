using System.Windows;
using System.Windows.Controls;

namespace AsynAwaitExamples.Steps;

// ============================================================================
// STEP 8: EXCEPTION HANDLING IN ASYNC CODE
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. try/catch works normally with await.
//    - When you "await" a task that throws, the exception is re-thrown at the
//      "await" line, and your catch block catches it. Simple!
//
// 2. Task.WhenAll and multiple exceptions:
//    - If you await Task.WhenAll(taskA, taskB) and BOTH tasks throw exceptions,
//      only the FIRST exception is thrown at the await line.
//    - To see ALL exceptions, check the .Exception property of each task, or
//      check the combined task's .Exception.InnerExceptions collection.
//
// 3. The exception call stack is preserved!
//    - When an async method throws, the stack trace shows you WHERE the error
//      happened, even across multiple async method calls. Very helpful for debugging.
//
// 4. ?? DANGER: "async void" methods
//    - If an async void method throws, the exception CANNOT be caught by the caller.
//    - It goes to the SynchronizationContext (in WPF: the Dispatcher).
//    - If unhandled, it crashes the application!
//    - This is why we only use async void for event handlers (which have try/catch inside).
// ============================================================================

public partial class Step08_ExceptionHandling : Page
{
    public Step08_ExceptionHandling()
    {
        InitializeComponent();
    }

    // ========================================================================
    // SCENARIO 1: Simple try/catch with a single async method.
    // This is the most common case — works exactly like synchronous try/catch.
    // ========================================================================
    private async void SingleException_Click(object sender, RoutedEventArgs e)
    {
        Log("--- Scenario 1: Single Task Exception ---\n");

        try
        {
            Log("? Calling a method that will fail...");

            // When this method throws, the exception appears right here at the "await".
            await FailingOperationAsync("Database connection timeout");

            // This line NEVER runs because the exception is thrown above.
            Log("This will never print.");
        }
        catch (InvalidOperationException ex)
        {
            // ? We caught the exception! Just like normal synchronous code.
            Log($"?? Caught exception: {ex.Message}");
            Log("? The app didn't crash — we handled it gracefully!\n");
        }
    }

    // ========================================================================
    // SCENARIO 2: Multiple tasks that all fail (Task.WhenAll).
    // We need to inspect each task to see ALL the errors.
    // ========================================================================
    private async void MultipleExceptions_Click(object sender, RoutedEventArgs e)
    {
        Log("--- Scenario 2: Multiple Task Exceptions ---\n");

        // Start three tasks — all of them will fail!
        Task task1 = FailingOperationAsync("Server A is down");
        Task task2 = FailingOperationAsync("Server B timed out");
        Task task3 = FailingOperationAsync("Server C returned 500");

        // Combine them into one big task.
        Task allTasks = Task.WhenAll(task1, task2, task3);

        try
        {
            // This will throw the FIRST exception only.
            await allTasks;
        }
        catch (InvalidOperationException ex)
        {
            // This catches only the first exception from WhenAll.
            Log($"?? First exception caught: {ex.Message}");

            // To see ALL exceptions, check the combined task's Exception property.
            // It contains an AggregateException with all inner exceptions.
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
    // The exception bubbles up through the entire chain — stack trace is preserved!
    // ========================================================================
    private async void NestedException_Click(object sender, RoutedEventArgs e)
    {
        Log("--- Scenario 3: Nested Async Exception ---\n");

        try
        {
            Log("? Calling MethodA ? MethodB ? MethodC (which throws)...");
            await MethodA();
        }
        catch (Exception ex)
        {
            Log($"?? Caught in event handler: {ex.Message}");
            Log($"\n?? Stack trace shows the full call chain:");

            // The stack trace preserves the full chain: MethodC ? MethodB ? MethodA
            // This makes debugging async code much easier!
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
        await Task.Delay(500); // Simulate some work before failing
        throw new InvalidOperationException(errorMessage);
    }

    private static async Task MethodA()
    {
        await Task.Delay(200);
        await MethodB(); // calls MethodB, which calls MethodC
    }

    private static async Task MethodB()
    {
        await Task.Delay(200);
        await MethodC(); // calls MethodC, which throws
    }

    private static async Task MethodC()
    {
        await Task.Delay(200);
        throw new ApplicationException("?? Something went wrong deep in MethodC!");
    }

    private void Log(string message)
    {
        Output.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        Output.ScrollToEnd();
    }
}
