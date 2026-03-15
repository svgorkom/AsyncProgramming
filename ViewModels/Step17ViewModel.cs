using System.Threading.Channels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AsynAwaitExamples.ViewModels;

// ============================================================================
// STEP 17 VIEWMODEL: Channel<T> — ASYNC PRODUCER/CONSUMER PATTERN
// ============================================================================
//
// KEY CONCEPTS:
// -------------
// 1. Channel<T> — A modern, thread-safe, async-friendly message queue.
//    - A PRODUCER writes items into the channel.
//    - A CONSUMER reads items from the channel.
//    - They can run concurrently on different threads/tasks.
//
// 2. BOUNDED vs UNBOUNDED channels:
//    - Channel.CreateUnbounded<T>() — unlimited buffer. Producer never waits.
//    - Channel.CreateBounded<T>(capacity) — limited buffer. Producer waits
//      (backpressure) when the channel is full. This prevents memory issues.
//
// 3. ChannelWriter<T> — The producer's end. Write items with WriteAsync().
//    - Call Complete() when done producing. This signals "no more items."
//
// 4. ChannelReader<T> — The consumer's end. Read with ReadAllAsync().
//    - ReadAllAsync() returns IAsyncEnumerable — works with await foreach!
//    - Automatically completes when the writer calls Complete().
//
// 5. WHY NOT BlockingCollection<T>?
//    - BlockingCollection uses threads (blocks while waiting).
//    - Channel<T> is async-native (awaits while waiting — no thread wasted).
//
// ANALOGY:
// --------
// Channel<T> is like a conveyor belt in a factory.
// The producer puts items on the belt, the consumer picks them off.
// Bounded channel = belt has limited space. Producer waits if belt is full.
// ============================================================================

public partial class Step17ViewModel : StepViewModelBase
{
    [ObservableProperty]
    private bool _isRunning;

    // ========================================================================
    // DEMO 1: Unbounded channel — producer and consumer running in parallel.
    // ========================================================================
    [RelayCommand]
    private async Task UnboundedChannel()
    {
        IsRunning = true;
        Log("--- Unbounded Channel: Producer/Consumer ---\n");

        var channel = Channel.CreateUnbounded<string>();

        // Start producer and consumer as parallel tasks.
        Task producer = ProduceItemsAsync(channel.Writer, 6, "Unbounded");
        Task consumer = ConsumeItemsAsync(channel.Reader, "Unbounded");

        // Wait for both to finish.
        await Task.WhenAll(producer, consumer);

        Log("   ?? Both producer and consumer finished!\n");
        IsRunning = false;
    }

    // ========================================================================
    // DEMO 2: Bounded channel — backpressure when full.
    // ========================================================================
    [RelayCommand]
    private async Task BoundedChannel()
    {
        IsRunning = true;
        Log("--- Bounded Channel (capacity=3): Backpressure Demo ---\n");
        Log("   ?? Channel can hold max 3 items. Producer waits when full.\n");

        // Channel can hold at most 3 items. Producer must wait when full.
        var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(3)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        // Producer is faster than consumer — it will hit the capacity limit.
        Task producer = ProduceItemsFastAsync(channel.Writer, 8);
        Task consumer = ConsumeItemsSlowAsync(channel.Reader);

        await Task.WhenAll(producer, consumer);

        Log("   ?? Done! Notice the producer had to WAIT when the channel was full.\n");
        IsRunning = false;
    }

    // ========================================================================
    // DEMO 3: Multiple consumers (fan-out).
    // ========================================================================
    [RelayCommand]
    private async Task MultipleConsumers()
    {
        IsRunning = true;
        Log("--- Multiple Consumers (Fan-Out) ---\n");
        Log("   ?? 1 producer, 3 consumers competing for items.\n");

        var channel = Channel.CreateUnbounded<string>();

        Task producer = ProduceItemsAsync(channel.Writer, 9, "FanOut");

        // 3 consumers all reading from the same channel.
        // Each item is consumed by exactly ONE consumer (first to grab it).
        Task consumer1 = ConsumeAsWorkerAsync(channel.Reader, "Worker-A");
        Task consumer2 = ConsumeAsWorkerAsync(channel.Reader, "Worker-B");
        Task consumer3 = ConsumeAsWorkerAsync(channel.Reader, "Worker-C");

        await Task.WhenAll(producer, consumer1, consumer2, consumer3);

        Log("\n   ?? All workers finished! Items were distributed among consumers.\n");
        IsRunning = false;
    }

    // --- Producer methods ---

    private async Task ProduceItemsAsync(ChannelWriter<string> writer, int count, string label)
    {
        for (int i = 1; i <= count; i++)
        {
            string item = $"{label}-Item-{i}";
            await writer.WriteAsync(item);
            Log($"   ?? Produced: {item}");
            await Task.Delay(300); // Simulate production time.
        }
        writer.Complete(); // Signal: no more items.
        Log("   ?? Producer finished (channel marked complete).");
    }

    private async Task ProduceItemsFastAsync(ChannelWriter<string> writer, int count)
    {
        for (int i = 1; i <= count; i++)
        {
            Log($"   ?? Trying to write Item-{i}...");
            await writer.WriteAsync($"Item-{i}"); // Will WAIT if channel is full!
            Log($"   ?? Written: Item-{i}");
            await Task.Delay(100); // Fast producer.
        }
        writer.Complete();
        Log("   ?? Producer finished.");
    }

    // --- Consumer methods ---

    private async Task ConsumeItemsAsync(ChannelReader<string> reader, string label)
    {
        // ReadAllAsync returns IAsyncEnumerable — works with await foreach!
        await foreach (string item in reader.ReadAllAsync())
        {
            Log($"   ?? Consumed: {item}");
            await Task.Delay(200); // Simulate processing time.
        }
        Log("   ?? Consumer finished (channel was completed).");
    }

    private async Task ConsumeItemsSlowAsync(ChannelReader<string> reader)
    {
        await foreach (string item in reader.ReadAllAsync())
        {
            Log($"   ?? Consuming: {item} (slow — 600ms)...");
            await Task.Delay(600); // Slow consumer — causes backpressure.
            Log($"   ?? Done: {item}");
        }
        Log("   ?? Consumer finished.");
    }

    private async Task ConsumeAsWorkerAsync(ChannelReader<string> reader, string workerName)
    {
        // Each worker tries to read items. First to grab it wins.
        while (await reader.WaitToReadAsync())
        {
            while (reader.TryRead(out string? item))
            {
                Log($"   ?? {workerName} processing: {item}");
                await Task.Delay(Random.Shared.Next(200, 500));
                Log($"   ? {workerName} done: {item}");
            }
        }
        Log($"   ?? {workerName} finished.");
    }
}
