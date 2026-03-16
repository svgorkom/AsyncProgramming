using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AsynAwaitExamples.ViewModels;

// ============================================================================
// STEP 22 VIEWMODEL: Parallel.ForEachAsync
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. Parallel.ForEachAsync -- The modern, async-friendly parallel loop.
//    - Available since .NET 6.
//    - Processes a collection of items in parallel, with async support.
//    - Built-in concurrency control (MaxDegreeOfParallelism).
//    - Returns a Task, so you can await it!
//
// 2. HOW IT DIFFERS FROM Task.WhenAll:
//    - Task.WhenAll: Start ALL tasks at once, wait for all.
//      -> 1000 items = 1000 simultaneous tasks (might overwhelm resources).
//    - Parallel.ForEachAsync: Process items in parallel, but with a CAP.
//      -> 1000 items, max 4 at a time = controlled parallelism.
//
// 3. HOW IT DIFFERS FROM Parallel.ForEach (old):
//    - Parallel.ForEach: Synchronous. Blocks threads. No async support.
//    - Parallel.ForEachAsync: Async. Uses await. Doesn't waste threads on I/O.
//
// 4. OPTIONS:
//    - MaxDegreeOfParallelism: How many items to process at once.
//      Default: Environment.ProcessorCount (for CPU work).
//      For I/O work, you might set it higher (e.g., 10 or 20).
//    - CancellationToken: Stop processing early.
//
// ANALOGY:
// --------
// You have 20 letters to mail. 
// Sequential: Mail one, wait for delivery, mail the next.
// Task.WhenAll: Drop all 20 in the mailbox at once (might jam).
// Parallel.ForEachAsync: Use 4 mailboxes simultaneously, 5 batches of 4.
// ============================================================================

public partial class Step22ViewModel : StepViewModelBase
{
    [ObservableProperty]
    private bool _isRunning;

    // ========================================================================
    // DEMO 1: Basic Parallel.ForEachAsync usage.
    // ========================================================================
    [RelayCommand]
    private async Task BasicDemo()
    {
        IsRunning = true;
        Log("--- Basic Parallel.ForEachAsync ---\n");

        int[] itemIds = Enumerable.Range(1, 10).ToArray();
        var sw = Stopwatch.StartNew();

        Log($"   [>] Processing {itemIds.Length} items with MaxDegreeOfParallelism = 3\n");

        await Parallel.ForEachAsync(itemIds, new ParallelOptions
        {
            MaxDegreeOfParallelism = 3
        },
        async (id, ct) =>
        {
            int delay = Random.Shared.Next(300, 700);
            await Task.Delay(delay, ct);
            Log($"   [OK] Item {id,2} processed ({delay}ms) " +
                $"[Thread: {Environment.CurrentManagedThreadId}]");
        });

        sw.Stop();
        Log($"\n   [i] Total time: {sw.ElapsedMilliseconds}ms");
        Log("   [i] Only 3 items ran at a time. Much faster than sequential!\n");
        IsRunning = false;
    }

    // ========================================================================
    // DEMO 2: Compare sequential vs Parallel.ForEachAsync.
    // ========================================================================
    [RelayCommand]
    private async Task CompareDemo()
    {
        IsRunning = true;
        Log("--- Sequential vs Parallel.ForEachAsync ---\n");

        string[] urls = Enumerable.Range(1, 6)
            .Select(i => $"https://api.example.com/item/{i}")
            .ToArray();

        // --- Sequential ---
        var sw = Stopwatch.StartNew();
        Log("   [>] Sequential (one at a time):");

        foreach (string url in urls)
        {
            await SimulateDownloadAsync(url);
        }

        var seqTime = sw.ElapsedMilliseconds;
        Log($"   [i] Sequential time: {seqTime}ms\n");

        // --- Parallel ---
        sw.Restart();
        Log("   [OK] Parallel.ForEachAsync (max 3 concurrent):");

        await Parallel.ForEachAsync(urls, new ParallelOptions
        {
            MaxDegreeOfParallelism = 3
        },
        async (url, ct) =>
        {
            await SimulateDownloadAsync(url);
        });

        var parTime = sw.ElapsedMilliseconds;
        Log($"   [i] Parallel time: {parTime}ms");
        Log($"\n   [i] Parallel was ~{seqTime / Math.Max(parTime, 1)}x faster!");
        Log($"   [i] {seqTime}ms -> {parTime}ms\n");
        IsRunning = false;
    }

    // ========================================================================
    // DEMO 3: With cancellation.
    // ========================================================================
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task WithCancellation(CancellationToken token)
    {
        IsRunning = true;
        Log("--- Parallel.ForEachAsync with Cancellation ---\n");

        int[] items = Enumerable.Range(1, 20).ToArray();

        Log($"   [>] Processing {items.Length} items (max 2 concurrent)...");
        Log("   Press Cancel to stop early!\n");

        try
        {
            await Parallel.ForEachAsync(items, new ParallelOptions
            {
                MaxDegreeOfParallelism = 2,
                CancellationToken = token
            },
            async (id, ct) =>
            {
                await Task.Delay(500, ct);
                Log($"   [OK] Item {id} processed");
            });

            Log("\n   [OK] All items processed!\n");
        }
        catch (OperationCanceledException)
        {
            Log("\n   [CANCELLED] Cancelled! Remaining items were not processed.\n");
        }
        finally
        {
            IsRunning = false;
        }
    }

    // --- Helper methods ---

    private async Task SimulateDownloadAsync(string url)
    {
        string name = url.Split('/')[^1];
        int delay = Random.Shared.Next(300, 600);
        await Task.Delay(delay);
        Log($"      [>] Downloaded {name} ({delay}ms)");
    }
}
