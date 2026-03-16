using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AsynAwaitExamples.ViewModels;

// ============================================================================
// STEP 6 VIEWMODEL: CANCELLING ASYNC OPERATIONS WITH CancellationToken
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. CancellationTokenSource (CTS) -- The CONTROLLER. Call .Cancel() to stop.
// 2. CancellationToken -- The SIGNAL passed into async methods.
// 3. token.ThrowIfCancellationRequested() -- Throws OperationCanceledException.
// 4. Task.Delay(milliseconds, token) -- Stops immediately when cancelled.
//
// MVVM NOTE:
// ----------
// We use [RelayCommand(IncludeCancelCommand = true)] which auto-generates both
// a StartLongOperationCommand AND a StartLongOperationCancelCommand. The toolkit
// handles CancellationToken plumbing for us -- no manual CTS management needed!
// ============================================================================

public partial class Step06ViewModel : StepViewModelBase
{
    [ObservableProperty]
    private bool _isRunning;

    /// <summary>
    /// Starts a long-running operation that can be cancelled.
    /// The [RelayCommand(IncludeCancelCommand = true)] generates:
    ///   - StartLongOperationCommand (the main command)
    ///   - StartLongOperationCancelCommand (the cancel command)
    /// The CancellationToken is provided automatically by the toolkit.
    /// </summary>
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task StartLongOperation(CancellationToken token)
    {
        IsRunning = true;
        Log("[>] Starting a long operation (10 steps)...");
        Log("   Press Cancel to stop it early!\n");

        try
        {
            await DoLongWorkAsync(token);
            Log("[OK] Operation completed successfully!\n");
        }
        catch (OperationCanceledException)
        {
            Log("[CANCELLED] Operation was CANCELLED by the user.\n");
        }
        finally
        {
            IsRunning = false;
        }
    }

    /// <summary>
    /// A simulated long-running operation with 10 steps.
    /// It checks the CancellationToken at each step.
    /// </summary>
    private async Task DoLongWorkAsync(CancellationToken token)
    {
        for (int i = 1; i <= 10; i++)
        {
            token.ThrowIfCancellationRequested();
            Log($"   [>] Processing step {i} of 10...");
            await Task.Delay(800, token);
        }
    }
}
