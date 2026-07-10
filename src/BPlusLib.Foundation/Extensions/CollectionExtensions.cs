// <copyright file="CollectionExtensions.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// </copyright>

namespace BPlusLib.Foundation.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Provides extension methods for collections and enumerables,
    /// filling gaps that are commonly addressed by LINQ but without
    /// taking a dependency on System.Linq in hot paths where it matters.
    /// All methods are thread-safe in the sense they do not retain
    /// mutable shared state, but callers must synchronize access to
    /// the underlying collections themselves.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Adds the elements of the specified collection to the end of the
        /// <see cref="ICollection{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="collection">The collection to add items to.</param>
        /// <param name="items">The items to add.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="collection"/> or <paramref name="items"/> is
        /// <see langword="null"/>.
        /// </exception>
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            // Optimisation for List<T> which already has AddRange.
            if (collection is List<T> list)
            {
                list.AddRange(items);
                return;
            }

            foreach (T item in items)
            {
                collection.Add(item);
            }
        }

        /// <summary>
        /// Removes all elements that match the conditions defined by the
        /// specified predicate.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="collection">The collection to remove items from.</param>
        /// <param name="predicate">
        /// The predicate that defines the conditions of the elements to remove.
        /// </param>
        /// <returns>The number of elements removed.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="collection"/> or <paramref name="predicate"/> is
        /// <see langword="null"/>.
        /// </exception>
        public static int RemoveWhere<T>(this ICollection<T> collection, Func<T, bool> predicate)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            // Optimisation for List<T> which already has RemoveAll.
            if (collection is List<T> list)
            {
                return list.RemoveAll(new Predicate<T>(predicate));
            }

            var toRemove = new List<T>();
            foreach (T item in collection)
            {
                if (predicate(item))
                {
                    toRemove.Add(item);
                }
            }

            foreach (T item in toRemove)
            {
                collection.Remove(item);
            }

            return toRemove.Count;
        }

        /// <summary>
        /// Batches the source sequence into chunks of the specified size.
        /// Each batch is materialized as an <see cref="IReadOnlyList{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the source.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="batchSize">The maximum number of elements per batch.</param>
        /// <returns>
        /// A sequence of batches, where each batch is a read-only list of
        /// up to <paramref name="batchSize"/> elements.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="batchSize"/> is less than 1.
        /// </exception>
        public static IEnumerable<IReadOnlyList<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (batchSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize, "batchSize must be at least 1.");
            }

            return BatchIterator(source, batchSize);
        }

        /// <summary>
        /// Returns distinct elements from the source sequence based on the
        /// specified key selector.
        /// </summary>
        /// <typeparam name="T">The type of elements in the source.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="keySelector">A function to extract the key for each element.</param>
        /// <returns>
        /// A sequence of distinct elements (by key).
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> is
        /// <see langword="null"/>.
        /// </exception>
        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            return DistinctByIterator(source, keySelector);
        }

        /// <summary>
        /// Performs the specified action on each element of the sequence,
        /// providing the element and its zero-based index.
        /// </summary>
        /// <typeparam name="T">The type of elements in the source.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="action">
        /// The action to perform on each element; receives
        /// <c>(element, index)</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="action"/> is
        /// <see langword="null"/>.
        /// </exception>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            int index = 0;
            foreach (T item in source)
            {
                action(item, index);
                index++;
            }
        }

        /// <summary>
        /// Determines whether the collection is <see langword="null"/> or
        /// contains no elements.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="collection">The collection to test.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="collection"/> is
        /// <see langword="null"/> or empty; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this ICollection<T>? collection)
        {
            return collection == null || collection.Count == 0;
        }

        /// <summary>
        /// Randomly shuffles the elements of the list in-place using the
        /// Fisher-Yates algorithm.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to shuffle.</param>
        /// <param name="rng">
        /// An optional <see cref="Random"/> instance. If <see langword="null"/>,
        /// a new thread-safe <see cref="Random"/> is used.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="list"/> is <see langword="null"/>.
        /// </exception>
        public static void Shuffle<T>(this IList<T> list, Random? rng = null)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

#if NET6_0_OR_GREATER
            rng ??= Random.Shared;
#else
            rng ??= new Random();
#endif

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey, TValue}"/> from a sequence of
        /// key-value pairs, without relying on LINQ's <c>ToDictionary</c>.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <typeparam name="TValue">The type of the values.</typeparam>
        /// <param name="source">The sequence of key-value pairs.</param>
        /// <returns>
        /// A dictionary containing all the key-value pairs from the source.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The source contains duplicate keys.
        /// </exception>
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
            this IEnumerable<KeyValuePair<TKey, TValue>> source)
            where TKey : notnull
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var dict = new Dictionary<TKey, TValue>();
            foreach (var kvp in source)
            {
                dict.Add(kvp.Key, kvp.Value);
            }

            return dict;
        }

        /// <summary>
        /// Gets the value associated with the specified key, or
        /// <see langword="default"/>(<typeparamref name="TValue"/>) if
        /// the key does not exist.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <typeparam name="TValue">The type of the values.</typeparam>
        /// <param name="dict">The dictionary to search.</param>
        /// <param name="key">The key to locate.</param>
        /// <returns>
        /// The value associated with <paramref name="key"/>, or
        /// <see langword="default"/> if the key is not found.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dict"/> or <paramref name="key"/> is
        /// <see langword="null"/>.
        /// </exception>
        public static TValue? GetValueOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dict,
            TKey key)
            where TKey : notnull
        {
            if (dict == null)
            {
                throw new ArgumentNullException(nameof(dict));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (dict.TryGetValue(key, out TValue? value))
            {
                return value;
            }

            return default;
        }

        private static IEnumerable<IReadOnlyList<T>> BatchIterator<T>(IEnumerable<T> source, int batchSize)
        {
            var batch = new List<T>(batchSize);
            foreach (T item in source)
            {
                batch.Add(item);
                if (batch.Count == batchSize)
                {
                    yield return batch.AsReadOnly();
                    batch = new List<T>(batchSize);
                }
            }

            if (batch.Count > 0)
            {
                yield return batch.AsReadOnly();
            }
        }

        private static IEnumerable<T> DistinctByIterator<T, TKey>(IEnumerable<T> source, Func<T, TKey> keySelector)
        {
            var seen = new HashSet<TKey>();
            foreach (T item in source)
            {
                if (seen.Add(keySelector(item)))
                {
                    yield return item;
                }
            }
        }
    }
}
