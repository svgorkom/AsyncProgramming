using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AsynAwaitExamples.ViewModels;

// ============================================================================
// STEP 1 VIEWMODEL: THE FREEZING UI PROBLEM
// ============================================================================
//
// KEY CONCEPT:
// -----------
// Every WPF application has a single "UI thread" (also called the "main thread").
// This thread does ALL the visual work: drawing controls, handling button clicks,
// updating text, processing mouse movements, etc.
//
// PROBLEM:
// --------
// If you run a SLOW operation on the UI thread (like Thread.Sleep, a big loop,
// a file download, or a database query), the UI thread is BUSY with that work
// and CANNOT update the screen or respond to user input.
// The app looks "frozen" -- you can't move the window, click buttons, or type.
//
// WHY THIS MATTERS:
// -----------------
// This is the #1 reason we need async programming: to keep the UI responsive
// while slow work happens in the background.
//
// MVVM NOTE:
// ----------
// The ViewModel exposes an IRelayCommand that the View binds to.
// The counter is now a bindable property instead of directly updating a TextBlock.
// ============================================================================

public partial class Step01ViewModel : StepViewModelBase
{
    private readonly DispatcherTimer _timer;

    [ObservableProperty]
    private int _counter;

    public Step01ViewModel()
    {
        // This timer ticks every second to update the counter.
        // It proves the UI is alive and responsive.
        // When the UI freezes, this counter will STOP updating.
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (s, e) => Counter++;
        _timer.Start();
    }

    /// <summary>
    /// THIS IS THE BAD WAY -- running slow code directly on the UI thread.
    /// 
    /// Thread.Sleep(3000) tells the current thread to "do nothing for 3 seconds".
    /// Since this runs on the UI thread, the ENTIRE APPLICATION freezes for 3 seconds.
    /// No buttons work, no animations play, the counter stops, and you can't even
    /// move the window.
    /// 
    /// In the next step, we'll learn how to fix this with async/await!
    /// </summary>
    [RelayCommand]
    private void RunBlocking()
    {
        Log("[!] Starting a BLOCKING operation on the UI thread...");
        Log("   (Try to move the window -- you can't!)");

        // BAD: This blocks the UI thread for 3 seconds!
        // The entire application is unresponsive during this time.
        Thread.Sleep(3000);

        // This line only runs AFTER the 3-second block is over.
        Log("[OK] Blocking operation finished. The UI is responsive again.");
        Log("   Notice the counter stopped during the freeze!\n");
    }
}
