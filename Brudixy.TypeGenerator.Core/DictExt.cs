using System;
using System.Collections;
using System.Collections.Generic;

namespace Brudixy.TypeGenerator.Core
{
  public static class DictExtensions
  {
    /// <summary>Gets if key exists or return given default value.</summary>
    /// <param name="dict"></param>
    /// <param name="key"></param>
    /// <param name="defaultVal"></param>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <returns></returns>
    public static TSource GetOrDefault<TSource, TKey>(
      this IReadOnlyDictionary<TKey, TSource> dict,
      TKey key)
    {
      return GetOrDefault<TSource, TKey>(dict, key, default(TSource));
    }

    /// <summary>Gets if key exists or return given default value.</summary>
    /// <param name="dict"></param>
    /// <param name="key"></param>
    /// <param name="defaultVal"></param>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <returns></returns>
    public static TSource GetOrDefault<TSource, TKey>(
      this IReadOnlyDictionary<TKey, TSource> dict,
      TKey key,
      TSource defaultVal)
    {
      TSource source;
      return dict.TryGetValue(key, out source) ? source : defaultVal;
    }

    /// <summary>Gets if key exists or add new key value.</summary>
    /// <param name="dict"></param>
    /// <param name="key"></param>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <returns></returns>
    /// <exception cref="T:System.ArgumentNullException"></exception>
    public static TSource GetOrAdd<TSource, TKey>(this IDictionary<TKey, TSource> dict, TKey key)
      where TSource : ICollection, new()
    {
      if (dict == null)
        throw new ArgumentNullException(nameof(dict));
      TSource source;
      return !dict.TryGetValue(key, out source) ? (dict[key] = new TSource()) : source;
    }

    /// <summary>Gets if key exists or add new key value.</summary>
    /// <param name="dict"></param>
    /// <param name="key"></param>
    /// <param name="valueFactory"></param>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <returns></returns>
    /// <exception cref="T:System.ArgumentNullException"></exception>
    public static TSource GetOrAdd<TSource, TKey>(
      this IDictionary<TKey, TSource> dict,
      TKey key,
      Func<TSource> valueFactory)
    {
      if (dict == null)
        throw new ArgumentNullException(nameof(dict));
      if (valueFactory == null)
        throw new ArgumentNullException(nameof(valueFactory));
      TSource source;
      return !dict.TryGetValue(key, out source) ? (dict[key] = valueFactory()) : source;
    }
  }
}