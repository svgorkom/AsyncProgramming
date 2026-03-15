using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.Input;

namespace AsynAwaitExamples.ViewModels;

// ============================================================================
// STEP 13 VIEWMODEL: ValueTask AND ValueTask<T>
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. ValueTask<T> — A lightweight alternative to Task<T>.
//    - Task<T> always allocates a heap object (even if the result is immediate).
//    - ValueTask<T> is a struct — when the result is already available, it avoids
//      the heap allocation entirely.
//
// 2. WHEN TO USE ValueTask<T>:
//    - When a method FREQUENTLY returns a cached or synchronous result.
//    - Example: a cache that hits 90% of the time. Only on cache-miss do you
//      actually do async I/O. ValueTask avoids allocating Task objects for the
//      90% cache-hit path.
//
// 3. WHEN NOT TO USE ValueTask<T>:
//    - When you need to await the result more than once.
//    - When you need to use Task.WhenAll / Task.WhenAny (they require Task<T>).
//    - When the method almost always does real async work (no benefit).
//
// 4. RULES:
//    - You can await a ValueTask<T> exactly ONCE.
//    - Do NOT store or reuse a ValueTask<T> — consume it immediately.
//    - If you need Task behavior, call .AsTask() to convert.
//
// ANALOGY:
// --------
// Task<T> is like ordering a package online — always involves processing.
// ValueTask<T> is like checking your mailbox — if the letter is already there,
// you just grab it (no allocation). If it's not, you wait for delivery.
// ============================================================================

public partial class Step13ViewModel : StepViewModelBase
{
    // Simulated cache — represents a hot-path lookup.
    private readonly ConcurrentDictionary<string, string> _cache = new();

    // ========================================================================
    // DEMO 1: Compare Task<T> vs ValueTask<T> allocation behavior.
    // ========================================================================
    [RelayCommand]
    private async Task CompareAllocations()
    {
        Log("--- Task<T> vs ValueTask<T> Allocation Comparison ---\n");

        // Pre-populate cache so most lookups are synchronous.
        _cache["user:1"] = "Alice";
        _cache["user:2"] = "Bob";
        _cache["user:3"] = "Charlie";

        // Scenario A: Using Task<T> — allocates every time.
        Log("?? Scenario A: Using Task<string> (always allocates)");
        for (int i = 1; i <= 3; i++)
        {
            string result = await GetUserWithTaskAsync($"user:{i}");
            Log($"   ? Got: {result} (Task<T> allocated on heap)");
        }

        Log("");

        // Scenario B: Using ValueTask<T> — avoids allocation on cache hit.
        Log("? Scenario B: Using ValueTask<string> (struct — no allocation on cache hit)");
        for (int i = 1; i <= 3; i++)
        {
            string result = await GetUserWithValueTaskAsync($"user:{i}");
            Log($"   ? Got: {result} (ValueTask<T> — no heap allocation!)");
        }

        Log("\n?? When the result is already available (cache hit), ValueTask<T>");
        Log("   avoids creating a Task object on the heap. For hot paths with");
        Log("   millions of calls, this saves significant GC pressure.\n");
    }

    // ========================================================================
    // DEMO 2: Cache miss — ValueTask falls back to real async work.
    // ========================================================================
    [RelayCommand]
    private async Task CacheMiss()
    {
        Log("--- ValueTask<T> Cache Miss Demonstration ---\n");

        _cache.Clear();

        Log("?? Requesting 'user:99' — NOT in cache...");
        string result = await GetUserWithValueTaskAsync("user:99");
        Log($"   ? Got: {result} (had to do async I/O — ValueTask wrapped a Task)");

        Log("\n?? Requesting 'user:99' again — NOW in cache...");
        string cached = await GetUserWithValueTaskAsync("user:99");
        Log($"   ? Got: {cached} (cache hit — no allocation!)");

        Log("\n?? ValueTask<T> shines when most calls hit cache and only a few");
        Log("   require real async work. The common path is allocation-free.\n");
    }

    // ========================================================================
    // DEMO 3: The rules — what you must NOT do with ValueTask<T>.
    // ========================================================================
    [RelayCommand]
    private void ShowRules()
    {
        Log("--- ValueTask<T> Rules ---\n");

        Log("? DO:");
        Log("   • Await it immediately:  var x = await GetValueTaskAsync();");
        Log("   • Use it for frequently-synchronous methods (caches, pools).");
        Log("   • Use it as a return type for interface methods that may be sync.");
        Log("");
        Log("? DO NOT:");
        Log("   • Await it more than once.");
        Log("   • Store it in a variable and await later (use .AsTask() instead).");
        Log("   • Use with Task.WhenAll / Task.WhenAny (convert with .AsTask()).");
        Log("   • Call .Result or .GetAwaiter().GetResult() before it completes.");
        Log("");
        Log("?? CONVERSION: If you need Task<T> behavior:");
        Log("   Task<string> task = myValueTask.AsTask();");
        Log("   // Now you can use Task.WhenAll, store it, await multiple times.\n");
    }

    // --- Helper methods ---

    /// <summary>
    /// Task<T> version — always allocates a Task object, even for cached results.
    /// </summary>
    private async Task<string> GetUserWithTaskAsync(string key)
    {
        if (_cache.TryGetValue(key, out string? cached))
        {
            // Even though we have the result, "async" forces a Task allocation.
            return cached;
        }

        // Simulate async I/O (database call).
        await Task.Delay(500);
        string result = $"User-{key} (fetched from DB)";
        _cache[key] = result;
        return result;
    }

    /// <summary>
    /// ValueTask<T> version — returns synchronously when cached (no allocation).
    /// </summary>
    private ValueTask<string> GetUserWithValueTaskAsync(string key)
    {
        if (_cache.TryGetValue(key, out string? cached))
        {
            // ? Synchronous return — no Task allocation at all!
            return new ValueTask<string>(cached);
        }

        // Cache miss — do the real async work.
        return new ValueTask<string>(FetchFromDatabaseAsync(key));
    }

    private async Task<string> FetchFromDatabaseAsync(string key)
    {
        await Task.Delay(500); // Simulate I/O
        string result = $"User-{key} (fetched from DB)";
        _cache[key] = result;
        return result;
    }
}
