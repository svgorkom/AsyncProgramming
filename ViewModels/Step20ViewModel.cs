using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.Input;

namespace AsynAwaitExamples.ViewModels;

// ============================================================================
// STEP 20 VIEWMODEL: ASYNC LINQ (Manual Async LINQ Operations)
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. IAsyncEnumerable<T> + LINQ-style operations.
//    - Standard LINQ (Where, Select, Take, etc.) doesn't work directly with
//      IAsyncEnumerable<T> because those are for IEnumerable<T>.
//    - You can write async LINQ manually using "await foreach" + yield return.
//    - For production code, the System.Linq.Async NuGet package provides
//      extension methods like WhereAwait, SelectAwait, ToListAsync, etc.
//
// 2. MANUAL ASYNC LINQ PATTERN:
//    - Where:   async IAsyncEnumerable<T> WhereAsync(source, predicate)
//    - Select:  async IAsyncEnumerable<U> SelectAsync(source, transform)
//    - Take:    async IAsyncEnumerable<T> TakeAsync(source, count)
//    - These compose just like regular LINQ — chain them together!
//
// 3. ToListAsync:
//    - Collects all items from an IAsyncEnumerable into a List<T>.
//    - Useful when you finally need all items in memory.
//
// 4. COMPOSITION:
//    - You can chain async LINQ methods: source.WhereAsync(...).SelectAsync(...)
//    - Each step processes items lazily, one at a time.
//
// ANALOGY:
// --------
// Regular LINQ = filtering/sorting a deck of cards on a table (all at once).
// Async LINQ = filtering cards as a dealer hands them to you one by one.
// ============================================================================

public partial class Step20ViewModel : StepViewModelBase
{
    // ========================================================================
    // DEMO 1: Filtering an async stream (WhereAsync).
    // ========================================================================
    [RelayCommand]
    private async Task FilterDemo()
    {
        Log("--- Async Where: Filtering an Async Stream ---\n");

        Log("   ?? Source stream: numbers 1 to 10 (arriving one at a time)");
        Log("   ?? Filter: keep only even numbers\n");

        await foreach (int number in WhereAsync(GenerateNumbersAsync(1, 10), n => n % 2 == 0))
        {
            Log($"   ? Passed filter: {number}");
        }

        Log("\n   ?? Items were filtered as they arrived — no buffering!\n");
    }

    // ========================================================================
    // DEMO 2: Transforming an async stream (SelectAsync).
    // ========================================================================
    [RelayCommand]
    private async Task TransformDemo()
    {
        Log("--- Async Select: Transforming an Async Stream ---\n");

        Log("   ?? Source: product IDs 1–5");
        Log("   ?? Transform: look up product details (async operation)\n");

        await foreach (string product in SelectAwaitAsync(
            GenerateNumbersAsync(1, 5),
            async id =>
            {
                await Task.Delay(300); // Simulate async lookup.
                return $"Product-{id}: ${id * 9.99m:F2}";
            }))
        {
            Log($"   ? {product}");
        }

        Log("\n   ?? Each item was transformed with an async operation.\n");
    }

    // ========================================================================
    // DEMO 3: Chaining multiple operations (pipeline).
    // ========================================================================
    [RelayCommand]
    private async Task PipelineDemo()
    {
        Log("--- Async LINQ Pipeline: Where ? Select ? Take ---\n");

        Log("   ?? Source: sensor readings 1–20");
        Log("   ?? Filter: temperature > 25°C");
        Log("   ?? Transform: format as string");
        Log("   ?? Take: first 4 results only\n");

        // Chain operations together — each processes lazily.
        var source = GenerateSensorDataAsync(20);
        var filtered = WhereAsync(source, r => r.Temperature > 25.0);
        var formatted = SelectAsync(filtered, r =>
            $"Sensor={r.Name}, Temp={r.Temperature:F1}°C");
        var limited = TakeAsync(formatted, 4);

        await foreach (string item in limited)
        {
            Log($"   ? {item}");
        }

        Log("\n   ?? The pipeline stopped after 4 matches — remaining items were");
        Log("   ?? never generated. This is lazy evaluation at work!\n");
    }

    // ========================================================================
    // DEMO 4: ToListAsync — collect into a list.
    // ========================================================================
    [RelayCommand]
    private async Task CollectDemo()
    {
        Log("--- ToListAsync: Collecting an Async Stream ---\n");

        Log("   ?? Collecting all even numbers from 1–10 into a list...\n");

        var source = WhereAsync(GenerateNumbersAsync(1, 10), n => n % 2 == 0);
        List<int> results = await ToListAsync(source);

        Log($"   ? Collected {results.Count} items: [{string.Join(", ", results)}]");
        Log("\n   ?? ToListAsync buffers everything into memory.");
        Log("   ?? Use 'await foreach' when possible to stay streaming.\n");
    }

    // ========================================================================
    // ASYNC LINQ HELPER METHODS
    // ========================================================================

    /// <summary>
    /// Async Where — filters items from an async stream.
    /// </summary>
    private static async IAsyncEnumerable<T> WhereAsync<T>(
        IAsyncEnumerable<T> source,
        Func<T, bool> predicate,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (T item in source.WithCancellation(ct))
        {
            if (predicate(item))
                yield return item;
        }
    }

    /// <summary>
    /// Async Select — transforms each item synchronously.
    /// </summary>
    private static async IAsyncEnumerable<TResult> SelectAsync<TSource, TResult>(
        IAsyncEnumerable<TSource> source,
        Func<TSource, TResult> selector,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (TSource item in source.WithCancellation(ct))
        {
            yield return selector(item);
        }
    }

    /// <summary>
    /// Async SelectAwait — transforms each item with an async operation.
    /// </summary>
    private static async IAsyncEnumerable<TResult> SelectAwaitAsync<TSource, TResult>(
        IAsyncEnumerable<TSource> source,
        Func<TSource, Task<TResult>> asyncSelector,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (TSource item in source.WithCancellation(ct))
        {
            yield return await asyncSelector(item);
        }
    }

    /// <summary>
    /// Async Take — yields at most 'count' items.
    /// </summary>
    private static async IAsyncEnumerable<T> TakeAsync<T>(
        IAsyncEnumerable<T> source,
        int count,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        int taken = 0;
        await foreach (T item in source.WithCancellation(ct))
        {
            yield return item;
            if (++taken >= count) yield break;
        }
    }

    /// <summary>
    /// Async ToList — collects all items into a List.
    /// </summary>
    private static async Task<List<T>> ToListAsync<T>(
        IAsyncEnumerable<T> source,
        CancellationToken ct = default)
    {
        var list = new List<T>();
        await foreach (T item in source.WithCancellation(ct))
        {
            list.Add(item);
        }
        return list;
    }

    // ========================================================================
    // DATA SOURCES
    // ========================================================================

    /// <summary>
    /// Generates numbers from 'start' to 'end' as an async stream.
    /// </summary>
    private static async IAsyncEnumerable<int> GenerateNumbersAsync(
        int start, int end,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        for (int i = start; i <= end; i++)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(200, ct); // Simulate async data arrival.
            yield return i;
        }
    }

    /// <summary>
    /// Generates simulated sensor data as an async stream.
    /// </summary>
    private static async IAsyncEnumerable<SensorData> GenerateSensorDataAsync(
        int count,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        string[] sensors = ["Living Room", "Kitchen", "Bedroom", "Garage", "Garden"];

        for (int i = 0; i < count; i++)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(150, ct);
            yield return new SensorData
            {
                Name = sensors[Random.Shared.Next(sensors.Length)],
                Temperature = 18.0 + Random.Shared.NextDouble() * 15.0
            };
        }
    }
}

/// <summary>
/// Simple data record for async LINQ demos.
/// </summary>
public record SensorData
{
    public required string Name { get; init; }
    public double Temperature { get; init; }
}
