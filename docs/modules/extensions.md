# Extensions

Extension methods for Task, collections, streams, and strings. Provides common patterns for async operations, collection manipulation, I/O, and text processing without requiring LINQ in hot paths.

## Classes

### TaskExtensions
Extension methods for Task and Task&lt;T&gt; to simplify common async patterns.

| Method | Returns | Description |
|--------|---------|-------------|
| FireAndForget(this Task, Action&lt;Exception&gt;?) | void | Fires task in background; swallows exceptions |
| WithTimeout&lt;T&gt;(this Task&lt;T&gt;, TimeSpan) | Task&lt;T&gt; | Adds timeout; throws TimeoutException on expiry |
| WithTimeout(this Task, TimeSpan) | Task | Adds timeout to void task |
| WithRetry&lt;T&gt;(this Func&lt;Task&lt;T&gt;&gt;, int, TimeSpan?) | Task&lt;T&gt; | Retries factory with optional delay |
| WithCancellation&lt;T&gt;(this Task&lt;T&gt;, CancellationToken) | Task&lt;T&gt; | Adds cancellation support |
| Memoize&lt;T&gt;(this Func&lt;Task&lt;T&gt;&gt;) | Lazy&lt;Task&lt;T&gt;&gt; | Caches async factory result |
| SuppressException&lt;T&gt;(this Task&lt;T&gt;) | Task&lt;T?&gt; | Returns default on failure |

### CollectionExtensions
Extension methods for collections and enumerables.

| Method | Returns | Description |
|--------|---------|-------------|
| AddRange&lt;T&gt;(this ICollection&lt;T&gt;, IEnumerable&lt;T&gt;) | void | Adds multiple items to collection |
| RemoveWhere&lt;T&gt;(this ICollection&lt;T&gt;, Func&lt;T, bool&gt;) | int | Removes matching items; returns count removed |
| Batch&lt;T&gt;(this IEnumerable&lt;T&gt;, int) | IEnumerable&lt;IReadOnlyList&lt;T&gt;&gt; | Batches into chunks |
| DistinctBy&lt;T, TKey&gt;(this IEnumerable&lt;T&gt;, Func&lt;T, TKey&gt;) | IEnumerable&lt;T&gt; | Distinct by key selector |
| ForEach&lt;T&gt;(this IEnumerable&lt;T&gt;, Action&lt;T, int&gt;) | void | Action with index |
| IsNullOrEmpty&lt;T&gt;(this ICollection&lt;T&gt;?) | bool | Checks null or empty |
| Shuffle&lt;T&gt;(this IList&lt;T&gt;, Random?) | void | Fisher-Yates in-place shuffle |
| ToDictionary&lt;TKey, TValue&gt;(this IEnumerable&lt;KVP&gt;) | Dictionary&lt;TKey, TValue&gt; | Creates dictionary from KVP sequence |
| GetValueOrDefault&lt;TKey, TValue&gt;(this IDictionary&lt;TKey, TValue&gt;, TKey) | TValue? | Safe dictionary lookup |

### StreamExtensions
Extension methods for Stream to simplify I/O operations.

| Method | Returns | Description |
|--------|---------|-------------|
| ReadAllBytes(this Stream) | byte[] | Reads entire stream to byte array |
| ReadAllText(this Stream, Encoding?) | string | Reads entire stream as text |
| CopyToAsync(this Stream, Stream, IProgress&lt;long&gt;, CancellationToken, int) | Task | Async copy with progress reporting |
| Drain(this Stream) | void | Reads and discards all remaining data |
| WriteText(this Stream, string, Encoding?) | void | Writes string to stream |
| ReadExact(this Stream, int) | byte[] | Reads exactly N bytes; throws EndOfStreamException |
| TryRead(this Stream, byte[], int, int) | int | Reads up to N bytes without throwing on EOS |

### StringExtensions
Extension methods for string manipulation.

| Method | Returns | Description |
|--------|---------|-------------|
| Truncate(this string, int, bool) | string | Truncates with optional ellipsis |
| IsMissing(this string?) | bool | True if null, empty, or whitespace |
| IsPresent(this string?) | bool | True if not null/whitespace |
| ToLines(this string) | string[] | Splits into lines (handles \r\n, \n, \r) |
| Reverse(this string) | string | Reverses characters |
| Repeat(this string, int) | string | Repeats string N times |
| CountOccurrences(this string, string, StringComparison) | int | Counts substring occurrences |
| SafeSubstring(this string, int, int?) | string | Substring without exceptions |
| StripHtml(this string) | string | Removes HTML tags |
| ToBase64(this string) | string | Encodes to Base64 |
| FromBase64(this string) | string | Decodes from Base64 |
| RemoveDiacritics(this string) | string | Removes accent marks |
| XmlEscape(this string) | string | Escapes XML entities |
| XmlUnescape(this string) | string | Unescapes XML entities |

## Usage

```csharp
using BPlusLib.Foundation.Extensions;

// Task extensions
someTask.FireAndForget();
var result = await GetDataAsync().WithTimeout(TimeSpan.FromSeconds(5));
var data = await RetryAsync().WithRetry(maxRetries: 3);

// Collection extensions
var batches = items.Batch(100);
list.AddRange(newItems);
list.RemoveWhere(x => x.IsActive == false);
list.Shuffle();

// Stream extensions
byte[] bytes = stream.ReadAllBytes();
string text = stream.ReadAllText();
stream.WriteText("hello");
await source.CopyToAsync(dest, progress: myProgress);

// String extensions
string truncated = longString.Truncate(50, ellipsis: true);
bool hasValue = name.IsPresent();
string[] lines = multiline.Split(new[] { '\r', '\n' });
string reversed = "hello".Reverse(); // "olleh"
string encoded = "data".ToBase64();
string stripped = "<p>Hello</p>".StripHtml(); // "Hello"
```

## Dependencies
- None (uses only BCL types)
