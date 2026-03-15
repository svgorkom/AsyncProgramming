using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AsynAwaitExamples.Steps;

// ============================================================================
// STEP 2: YOUR FIRST ASYNC/AWAIT
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. "async" keyword  ? You put this on a method to say "this method can use await inside it."
//                        It does NOT make the method run on another thread by itself!
//
// 2. "await" keyword  ? You put this before an operation that takes time (like Task.Delay).
//                        It means: "Start this operation, release the current thread so it can
//                        do other work, and come back to this spot when the operation finishes."
//
// 3. "Task"           ? Represents "a piece of work that will complete in the future."
//                        Think of it like a promise: "I promise I'll finish eventually."
//
// 4. "Task.Delay()"   ? The async-friendly replacement for Thread.Sleep().
//                        Thread.Sleep BLOCKS the thread (bad for UI).
//                        Task.Delay WAITS without blocking (good for UI).
//
// RETURN TYPES FOR ASYNC METHODS:
// --------------------------------
//   - async Task       ? The method does work but returns no value (like void, but awaitable).
//   - async Task<int>  ? The method does work and returns an int (we'll cover this in Step 3).
//   - async void       ? ONLY for event handlers (like button Click). Avoid everywhere else!
//                         Why? Because you can't await an "async void" method, and exceptions
//                         in them can crash your app. We use it here only because WPF event
//                         handlers require a void return type.
//                         See Step 3 for a full explanation of the safe "async void" pattern.
// ============================================================================

public partial class Step02_FirstAsyncAwait : Page
{
    private int _counter;

    public Step02_FirstAsyncAwait()
    {
        InitializeComponent();

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (s, e) =>
        {
            _counter++;
            CounterText.Text = $"Counter: {_counter}";
        };
        timer.Start();
    }

    /// <summary>
    /// THIS IS THE GOOD WAY — using async/await.
    /// 
    /// "async void" is used here ONLY because this is a WPF event handler.
    /// We wrap everything in try/catch so exceptions don't crash the app.
    /// The real work is delegated to an async Task method below.
    /// (See Step 3 for a deep explanation of this pattern.)
    /// </summary>
    private async void RunAsync_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await RunAsyncDemoAsync();
        }
        catch (Exception ex)
        {
            Log($"? Error: {ex.Message}\n");
        }
    }

    /// <summary>
    /// The real work lives in this "async Task" method — safe, awaitable, testable.
    /// 
    /// When we hit "await Task.Delay(3000)":
    ///   - A 3-second timer starts in the background.
    ///   - The UI thread is RELEASED — it goes back to handling clicks, drawing, etc.
    ///   - After 3 seconds, execution automatically comes back here and continues
    ///     with the next line (the Log call).
    /// 
    /// The result: the UI stays perfectly responsive during the entire 3-second wait!
    /// </summary>
    private async Task RunAsyncDemoAsync()
    {
        Log("? Starting an ASYNC operation...");
        Log("   (Try moving the window — it still works!)");

        // ? GOOD: await Task.Delay does NOT block the UI thread.
        // The UI thread is free to update the counter, handle clicks, etc.
        await Task.Delay(3000);

        // This line runs after the 3-second delay, back on the UI thread.
        Log("? Async operation finished!");
        Log("   Notice the counter KEPT running the whole time!\n");
    }

    private void Log(string message)
    {
        Output.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        Output.ScrollToEnd();
    }
}
