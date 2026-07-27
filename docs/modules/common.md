# Common

Core utility types and patterns for defensive programming, async coordination, and common abstractions. Includes guard clauses, disposable pattern, Option/Result monads, object pooling, debouncing, retry policies, async caching, and async locks.

## Classes

### Guard
Static class providing defensive argument validation guard methods with automatic caller argument expression capture.

| Method | Returns | Description |
|--------|---------|-------------|
| ThrowIfNull&lt;T&gt;(T? argument) | void | Throws ArgumentNullException if argument is null |
| ThrowIfNull(object? argument) | void | Throws ArgumentNullException if argument is null |
| ThrowIfNullOrEmpty(string? argument) | void | Throws ArgumentException if argument is null or empty |
| ThrowIfNullOrWhiteSpace(string? argument) | void | Throws ArgumentException if argument is null, empty, or whitespace |
| ThrowIfOutOfRange(int value, int min, int max) | void | Throws ArgumentOutOfRangeException if value is outside [min, max] |
| ThrowIfDisposed(bool isDisposed) | void | Throws ObjectDisposedException if isDisposed is true |
| ThrowIfInvalidOperation(bool condition, string message) | void | Throws InvalidOperationException if condition is true |

### DisposableBase
Abstract base class implementing the standard IDisposable pattern with thread-safe dispose tracking.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| IsDisposed | bool | Gets whether this instance has been disposed (thread-safe) |
| DisposeManaged() | void | Abstract — release managed resources |
| DisposeUnmanaged() | void | Virtual — release unmanaged resources |
| Dispose() | void | Releases all resources; idempotent |

### Option&lt;T&gt;
A readonly struct representing an optional value that may or may not exist. Provides a type-safe alternative to null references.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| Option&lt;T&gt;.Some(T value) | Option&lt;T&gt; | Creates an option containing the value |
| Option&lt;T&gt;.None | Option&lt;T&gt; | Creates an empty option |
| HasValue | bool | Gets whether the option contains a value |
| Value | T | Gets the encapsulated value (throws if None) |
| OrDefault(T? defaultValue) | T? | Returns value or default |
| Map&lt;TNew&gt;(Func&lt;T, TNew&gt;) | Option&lt;TNew&gt; | Transforms the value |
| Bind&lt;TNew&gt;(Func&lt;T, Option&lt;TNew&gt;&gt;) | Option&lt;TNew&gt; | Chains another option-returning operation |
| Match(Action&lt;T&gt;, Action) | void | Executes one of two actions based on presence |
| ToResult(Exception?) | Result&lt;T&gt; | Converts to a Result |
| Where(Func&lt;T, bool&gt;) | Option&lt;T&gt; | Filters by predicate |

### Result / Result&lt;T&gt;
Represents the result of an operation that can succeed or fail. The non-generic version is for void operations.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| Result.Ok() | Result | Creates a successful result |
| Result.Fail(Exception) | Result | Creates a failed result from an exception |
| Result&lt;T&gt;.Ok(T value) | Result&lt;T&gt; | Creates a successful result with value |
| Result&lt;T&gt;.Fail(Exception) | Result&lt;T&gt; | Creates a failed result |
| IsSuccess | bool | Gets whether the operation succeeded |
| IsFailure | bool | Gets whether the operation failed |
| Error | Exception? | Gets the failure exception |
| Value | T | Gets the success value (throws if failed) |
| Map&lt;TNew&gt;(Func&lt;T, TNew&gt;) | Result&lt;TNew&gt; | Transforms the success value |
| Bind&lt;TNew&gt;(Func&lt;T, Result&lt;TNew&gt;&gt;) | Result&lt;TNew&gt; | Chains another result-returning operation |
| OrDefault(T?) | T? | Returns value or default |
| OrThrow() | T | Returns value or throws the failure exception |
| Match(Action&lt;T&gt;, Action&lt;Exception&gt;) | void | Executes one of two actions |

### ObjectPool&lt;T&gt;
Thread-safe object pool using ConcurrentBag for lock-free access. Items are recycled via PooledObject&lt;T&gt; handles.

| Method/Property | Returns | Description |
|-----------------|---------|-------------|
| ObjectPool(Func&lt;T&gt;, int maxPoolSize) | — | Creates a pool with factory and max size |
| Get() | PooledObject&lt;T&gt; | Gets or creates an item |
| Return(T item) | void | Returns an item to the pool |
| Dispose() | void | Releases all pooled items |

### Debounce
Provides debouncing for actions and async operations. Each invocation resets a timer; execution only occurs after the delay elapses without a new call.

| Method | Returns | Description |
|--------|---------|-------------|
| Debounce(TimeSpan delay) | — | Creates a debouncer with the specified delay |
| Invoke(Action) | void | Invokes the action after debounce delay |
| InvokeAsync(Func&lt;Task&gt;) | void | Invokes async operation after debounce delay |
| Cancel() | void | Cancels any pending operation |
| Dispose() | void | Disposes the debouncer |

### RetryPolicy
Configurable retry logic with backoff strategies (Constant, Linear, Exponential, Jitter) and exception filtering.

| Method | Returns | Description |
|--------|---------|-------------|
| RetryPolicy(int maxRetries, TimeSpan baseDelay, RetryBackoffType) | — | Creates a retry policy |
| RetryOn&lt;TException&gt;() | RetryPolicy | Filters which exceptions to retry |
| OnRetry(Action&lt;int, Exception, TimeSpan&gt;) | RetryPolicy | Registers a retry callback |
| ExecuteAsync&lt;T&gt;(Func&lt;CancellationToken, Task&lt;T&gt;&gt;, CancellationToken) | Task&lt;T&gt; | Executes with retry logic |
| ExecuteAsync(Func&lt;CancellationToken, Task&gt;, CancellationToken) | Task | Executes void operation with retry |

### AsyncCache&lt;TKey, TValue&gt;
Thread-safe, lazy-evaluated async cache with optional time-based expiry.

| Method | Returns | Description |
|--------|---------|-------------|
| AsyncCache(Func&lt;TKey, CancellationToken, Task&lt;TValue&gt;&gt;, TimeSpan?) | — | Creates a cache with factory and optional expiry |
| GetAsync(TKey, CancellationToken) | Task&lt;TValue&gt; | Gets or creates the cached value |
| Invalidate(TKey) | void | Removes a cached value |
| Clear() | void | Removes all cached values |
| Dispose() | void | Disposes the cache |

### AsyncLock
Asynchronous mutual-exclusion lock based on SemaphoreSlim(1,1).

| Method | Returns | Description |
|--------|---------|-------------|
| LockAsync(CancellationToken) | ValueTask&lt;AsyncLockReleaser&gt; | Asynchronously acquires the lock |
| Lock(CancellationToken) | AsyncLockReleaser | Synchronously acquires the lock |
| Dispose() | void | Disposes the lock |

### ObservableObject
Base class for objects with observable property changes. Implements INotifyPropertyChanged and INotifyPropertyChanging.

| Method | Returns | Description |
|--------|---------|-------------|
| SetProperty&lt;T&gt;(ref T, T, string?) | bool | Sets backing field and raises change notifications |
| OnPropertyChanged(string?) | void | Raises PropertyChanged event |
| OnPropertyChanging(string?) | void | Raises PropertyChanging event |

## Usage

```csharp
using BPlusLib.Foundation.Common;

// Guard
Guard.ThrowIfNull(myObject);
Guard.ThrowIfNullOrEmpty(name);

// Option
var opt = Option<string>.Some("hello");
string result = opt.OrDefault("default");

// Result
var res = Result<int>.Ok(42);
int value = res.OrThrow();

// Debounce
using var debouncer = new Debounce(TimeSpan.FromMilliseconds(300));
debouncer.Invoke(() => Search(text));

// Retry
var policy = new RetryPolicy(3, TimeSpan.FromSeconds(1), RetryBackoffType.Exponential);
var data = await policy.ExecuteAsync(ct => httpClient.GetAsync(url, ct));

// AsyncLock
using var asyncLock = new AsyncLock();
using (await asyncLock.LockAsync())
{
    // Critical section
}

// Object Pool
var pool = new ObjectPool<MemoryStream>(() => new MemoryStream(), 10);
using var handle = pool.Get();
// Use handle.Item
```

## Dependencies
- None (uses only BCL types)
