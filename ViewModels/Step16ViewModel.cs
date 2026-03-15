using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;

namespace AsynAwaitExamples.ViewModels;

// ============================================================================
// STEP 16 VIEWMODEL: SemaphoreSlim & ASYNC THROTTLING
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. SemaphoreSlim — A lightweight synchronization primitive.
//    - Controls how many threads/tasks can access a resource CONCURRENTLY.
//    - Use WaitAsync() for async-friendly locking (unlike lock{} which blocks).
//
// 2. THROTTLING — Limiting the number of concurrent async operations.
//    - Example: You have 100 URLs to download, but you want at most 3 at a time.
//    - Without throttling: 100 simultaneous connections ? server overload!
//    - With SemaphoreSlim(3): Only 3 download at a time, others wait their turn.
//
// 3. WaitAsync(token) — Async version of Wait(). Respects CancellationToken.
//    - Always call Release() in a finally block!
//
// 4. ASYNC LOCK — SemaphoreSlim(1, 1) acts as an async-compatible mutex.
//    - C# "lock" keyword blocks the thread (bad for async).
//    - SemaphoreSlim(1, 1) + WaitAsync() is the async alternative.
//
// ANALOGY:
// --------
// SemaphoreSlim is like a bouncer at a club. The club has a max capacity (say 3).
// When it's full, new people wait outside. When someone leaves, the next person
// enters. WaitAsync() = get in line without blocking the street (thread).
// ============================================================================

public partial class Step16ViewModel : StepViewModelBase
{
    // ========================================================================
    // DEMO 1: No throttling — all tasks run at once.
    // ========================================================================
    [RelayCommand]
    private async Task NoThrottling()
    {
        Log("--- No Throttling: All Tasks at Once ---\n");

        var sw = Stopwatch.StartNew();
        string[] urls = CreateUrls(8);

        Log($"   ?? Starting all {urls.Length} downloads simultaneously...\n");

        Task[] tasks = urls.Select(url => DownloadSimulatedAsync(url, null)).ToArray();
        await Task.WhenAll(tasks);

        sw.Stop();
        Log($"\n   ? All done in {sw.ElapsedMilliseconds}ms");
        Log($"   ?? All {urls.Length} ran at the same time — could overwhelm a server!\n");
    }

    // ========================================================================
    // DEMO 2: With throttling — at most 3 concurrent tasks.
    // ========================================================================
    [RelayCommand]
    private async Task WithThrottling()
    {
        Log("--- With Throttling: Max 3 Concurrent ---\n");

        var sw = Stopwatch.StartNew();
        string[] urls = CreateUrls(8);

        // Create a semaphore that allows max 3 concurrent operations.
        using var semaphore = new SemaphoreSlim(3, 3);

        Log($"   ?? Starting {urls.Length} downloads with max 3 concurrent...\n");

        Task[] tasks = urls.Select(url => DownloadWithThrottleAsync(url, semaphore)).ToArray();
        await Task.WhenAll(tasks);

        sw.Stop();
        Log($"\n   ? All done in {sw.ElapsedMilliseconds}ms");
        Log("   ? Only 3 ran at a time — server is not overwhelmed!\n");
    }

    // ========================================================================
    // DEMO 3: Async lock — SemaphoreSlim(1, 1) as a mutex.
    // ========================================================================
    [RelayCommand]
    private async Task AsyncLock()
    {
        Log("--- Async Lock: SemaphoreSlim(1, 1) ---\n");

        // SemaphoreSlim(1, 1) = only 1 task at a time = mutex.
        using var mutex = new SemaphoreSlim(1, 1);
        int sharedCounter = 0;

        Log("   ?? 5 tasks incrementing a shared counter with async lock...\n");

        Task[] tasks = Enumerable.Range(1, 5).Select(async i =>
        {
            await mutex.WaitAsync();
            try
            {
                // Critical section — only one task at a time.
                int before = sharedCounter;
                await Task.Delay(200); // Simulate some async work.
                sharedCounter = before + 1;
                Log($"   ?? Task {i}: counter {before} ? {sharedCounter}");
            }
            finally
            {
                mutex.Release();
            }
        }).ToArray();

        await Task.WhenAll(tasks);

        Log($"\n   ? Final counter: {sharedCounter} (correct! no race conditions)");
        Log("   ?? Without the lock, tasks would overwrite each other's work.\n");
    }

    // --- Helper methods ---

    private static string[] CreateUrls(int count)
        => Enumerable.Range(1, count).Select(i => $"https://api.example.com/data/{i}").ToArray();

    private async Task DownloadSimulatedAsync(string url, SemaphoreSlim? semaphore)
    {
        string name = url.Split('/')[^1];
        int delay = Random.Shared.Next(300, 800);
        Log($"   ?? Downloading item {name}...");
        await Task.Delay(delay);
        Log($"   ? Item {name} done ({delay}ms)");
    }

    private async Task DownloadWithThrottleAsync(string url, SemaphoreSlim semaphore)
    {
        string name = url.Split('/')[^1];

        // Wait for a "slot" to open up.
        await semaphore.WaitAsync();
        try
        {
            int delay = Random.Shared.Next(300, 800);
            Log($"   ?? Downloading item {name} (slot acquired)...");
            await Task.Delay(delay);
            Log($"   ? Item {name} done ({delay}ms)");
        }
        finally
        {
            // Always release the slot, even if an exception occurs!
            semaphore.Release();
        }
    }
}
