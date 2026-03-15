using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AsynAwaitExamples.ViewModels;

// ============================================================================
// STEP 2 VIEWMODEL: YOUR FIRST ASYNC/AWAIT
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
// MVVM NOTE:
// ----------
// With CommunityToolkit.Mvvm, [RelayCommand] on an async Task method automatically
// generates an AsyncRelayCommand — handling exceptions and busy state for us.
// This replaces the manual "async void" event handler + try/catch wrapper pattern.
// ============================================================================

public partial class Step02ViewModel : StepViewModelBase
{
    [ObservableProperty]
    private int _counter;

    public Step02ViewModel()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (s, e) => Counter++;
        timer.Start();
    }

    /// <summary>
    /// The [RelayCommand] attribute on an async Task method generates an
    /// IAsyncRelayCommand. The toolkit handles:
    ///   - Wrapping in try/catch (exceptions surface via Command.ExecutionTask)
    ///   - Providing IsRunning / CanBeCanceled state
    ///
    /// When we hit "await Task.Delay(3000)":
    ///   - A 3-second timer starts in the background.
    ///   - The UI thread is RELEASED — it goes back to handling clicks, drawing, etc.
    ///   - After 3 seconds, execution automatically comes back here and continues.
    ///
    /// The result: the UI stays perfectly responsive during the entire 3-second wait!
    /// </summary>
    [RelayCommand]
    private async Task RunAsync()
    {
        Log("?? Starting an ASYNC operation...");
        Log("   (Try moving the window — it still works!)");

        // ? GOOD: await Task.Delay does NOT block the UI thread.
        // The UI thread is free to update the counter, handle clicks, etc.
        await Task.Delay(3000);

        // This line runs after the 3-second delay, back on the UI thread.
        Log("? Async operation finished!");
        Log("   Notice the counter KEPT running the whole time!\n");
    }
}
