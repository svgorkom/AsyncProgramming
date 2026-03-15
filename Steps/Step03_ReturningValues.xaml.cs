using System.Windows;
using System.Windows.Controls;

namespace AsynAwaitExamples.Steps;

// ============================================================================
// STEP 3: RETURNING VALUES FROM ASYNC METHODS (Task<T>)
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. Task<T>  ? An async method that eventually produces a value of type T.
//               For example, Task<string> means "I'll give you a string when I'm done."
//
// 2. "return" in async methods ? You just write "return myValue;" like normal.
//               The compiler automatically wraps it in a Task<T> for you.
//               You do NOT write "return Task.FromResult(myValue);" — the compiler does that.
//
// 3. "await" unwraps Task<T> ? When you write:  string result = await GetNameAsync();
//               The "await" waits for GetNameAsync to finish, then extracts the string from
//               the Task<string> and puts it into the "result" variable.
//
// ANALOGY:
// --------
// Imagine you order a package online (Task<Package>).
///   - You don't have the package yet, but you have a tracking number (the Task).
///   - "await" is like waiting at the door for delivery.
///   - When it arrives, you open the box (unwrap Task<Package>) and get your Package.
// ============================================================================
// ?? IMPORTANT LESSON: THE "async void" PATTERN
// ============================================================================
//
// You may have noticed that all our button click handlers use "async void".
// You were told this is wrong — and that advice is MOSTLY correct! Here's the full story:
//
// WHY "async void" IS DANGEROUS:
// ------------------------------
//   1. EXCEPTIONS CANNOT BE CAUGHT BY THE CALLER.
//      If an async void method throws, the exception escapes to the
//      SynchronizationContext. In WPF, this means an unhandled exception
//      that can crash your entire application!
//
//   2. THE CALLER CANNOT AWAIT IT.
//      Since it returns void (not Task), nobody can write:
//          await MyAsyncVoidMethod();   // ? Compiler error — void can't be awaited
//      This means the caller has no way to know when the method finishes.
//
//   3. HARD TO TEST.
//      Unit tests need to await methods to verify results. You can't await void.
//
// THE ONE EXCEPTION: EVENT HANDLERS
// ---------------------------------
//   WPF event handlers (Click, Loaded, SelectionChanged, etc.) REQUIRE a void
//   return type. The delegate signature is fixed:
//       void Handler(object sender, RoutedEventArgs e)
//   You CANNOT change this to return Task — the compiler won't allow it.
//   So "async void" is UNAVOIDABLE for event handlers.
//
// THE CORRECT PATTERN:
// --------------------
//   Keep the "async void" event handler as a THIN WRAPPER:
//     1. Wrap everything in try/catch (to prevent unhandled exceptions).
//     2. Delegate the real work to a separate "async Task" method.
//
//   ? BAD — all logic crammed into async void, no try/catch:
//       private async void Button_Click(object sender, RoutedEventArgs e)
//       {
//           var data = await FetchDataAsync();   // if this throws, app crashes!
//           ProcessData(data);
//       }
//
//   ? GOOD — thin wrapper with try/catch, logic in async Task method:
//       private async void Button_Click(object sender, RoutedEventArgs e)
//       {
//           try
//           {
//               await FetchAndDisplayDataAsync();
//           }
//           catch (Exception ex)
//           {
//               Log($"Error: {ex.Message}");  // handle gracefully, no crash
//           }
//       }
//
//       private async Task FetchAndDisplayDataAsync()  // ? async TASK, not void!
//       {
//           var data = await FetchDataAsync();
//           ProcessData(data);
//       }
//
// SUMMARY:
//   "async void" ? ONLY in event handlers, always with try/catch.
//   "async Task"  ? EVERYWHERE ELSE. This is the default you should use.
//   "async Task<T>" ? When you need to return a value.
// ============================================================================

public partial class Step03_ReturningValues : Page
{
    public Step03_ReturningValues()
    {
        InitializeComponent();
    }

    /// <summary>
    /// EVENT HANDLER — the only place where "async void" is acceptable.
    /// 
    /// Notice the pattern:
    ///   1. It's a THIN wrapper — barely any logic here.
    ///   2. Everything is inside try/catch — exceptions won't crash the app.
    ///   3. The real work is in FetchAndDisplayAllDataAsync(), which returns Task.
    /// 
    /// This is the pattern you should follow for ALL async event handlers.
    /// </summary>
    private async void FetchData_Click(object sender, RoutedEventArgs e)
    {
        // ? PATTERN: async void event handler wraps everything in try/catch
        // and delegates to an async Task method.
        try
        {
            await FetchAndDisplayAllDataAsync();
        }
        catch (Exception ex)
        {
            // Because we catch here, the exception is handled gracefully.
            // Without this try/catch, an exception would crash the application!
            Log($"? Error: {ex.Message}\n");
        }
    }

    /// <summary>
    /// THE REAL WORK — in an "async Task" method (not async void!).
    /// 
    /// This method can be:
    ///   - Awaited by the caller (the event handler awaits it above).
    ///   - Tested in a unit test:  await page.FetchAndDisplayAllDataAsync();
    ///   - Called from other async methods.
    ///   - Safely composed with Task.WhenAll, Task.WhenAny, etc.
    /// 
    /// None of this would be possible if this were "async void".
    /// </summary>
    private async Task FetchAndDisplayAllDataAsync()
    {
        Log("? Fetching user name...");

        // "await" unwraps Task<string> into string.
        // While GetUserNameAsync is "working", the UI stays responsive.
        string name = await GetUserNameAsync();
        Log($"   Got name: {name}");

        Log("? Fetching user age...");

        // "await" unwraps Task<int> into int.
        int age = await GetUserAgeAsync();
        Log($"   Got age: {age}");

        Log("? Fetching temperature...");

        // "await" unwraps Task<double> into double.
        double temp = await GetTemperatureAsync();
        Log($"   Got temperature: {temp}°C");

        Log($"\n?? Summary: {name}, age {age}, current temp {temp}°C\n");
    }

    // ========================================================================
    // ASYNC METHODS THAT RETURN VALUES
    // ========================================================================
    // Notice: the return type is Task<string>, but we just write "return string_value".
    // The compiler handles wrapping it in a Task for us. Magic! ?

    /// <summary>
    /// Simulates fetching a user's name from a slow source (like a database).
    /// Returns Task&lt;string&gt; — a string that will be available in the future.
    /// </summary>
    private static async Task<string> GetUserNameAsync()
    {
        // Simulate a 1-second delay (like a database query)
        await Task.Delay(1000);

        // Just return the string — the compiler wraps it in Task<string> for us.
        return "Alice";
    }

    /// <summary>
    /// Simulates fetching a user's age.
    /// Returns Task&lt;int&gt; — an int that will be available in the future.
    /// </summary>
    private static async Task<int> GetUserAgeAsync()
    {
        await Task.Delay(1000);
        return 30;
    }

    /// <summary>
    /// Simulates fetching a temperature reading.
    /// Returns Task&lt;double&gt; — a double that will be available in the future.
    /// </summary>
    private static async Task<double> GetTemperatureAsync()
    {
        await Task.Delay(1000);
        return 22.5;
    }

    private void Log(string message)
    {
        Output.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        Output.ScrollToEnd();
    }
}
