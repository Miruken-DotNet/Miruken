using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

namespace Miruken.Concurrency
{
    public static class LinqToPromises
    {
        public static Promise<TResult> Select<TResult>(
             this Promise source, Func<object, TResult> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return source.Then((result, s) => selector(null));
        }

        public static Promise<TResult> Select<TSource, TResult>(
            this Promise<TSource> source, Func<TSource, TResult> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return source.Then((result,s) => selector(result));
        }

        public static Promise<TResult> SelectMany<TSource, TResult>(
            this Promise<TSource> source, Func<TSource, Promise<TResult>> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return source.Then((result,s) => selector(result));
        }

        public static Promise<TResult> SelectMany<TSource, TCollection, TResult>(
            this Promise<TSource> source,
            Func<TSource, Promise<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (collectionSelector == null) throw new ArgumentNullException(nameof(collectionSelector));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return source.Then((result,s) => collectionSelector(result)
                .Then((collection,ss) => resultSelector(result, collection)));
        }

        public static Promise<TSource> Where<TSource>(
            this Promise<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return source.Then((result,s) => {
                if (predicate(result)) return result;
                throw new CancelledException();
            });
        }

        public static Promise<TResult> Join<TOuter, TInner, TKey, TResult>(
            this Promise<TOuter> outer, Promise<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector)
        {
            return Join(outer, inner, outerKeySelector, innerKeySelector, resultSelector, EqualityComparer<TKey>.Default);
        }

        public static Promise<TResult> Join<TOuter, TInner, TKey, TResult>(
            this Promise<TOuter> outer, Promise<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector,
            IEqualityComparer<TKey> comparer)
        {
            if (outer == null) throw new ArgumentNullException(nameof(outer));
            if (inner == null) throw new ArgumentNullException(nameof(inner));
            if (outerKeySelector == null) throw new ArgumentNullException(nameof(outerKeySelector));
            if (innerKeySelector == null) throw new ArgumentNullException(nameof(innerKeySelector));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));

            return outer.Then((outResult,s) => inner.Then((inResult,ss) => {
                if (comparer.Equals(outerKeySelector(outResult), innerKeySelector(inResult)))
                    return resultSelector(outResult, inResult);
                throw new CancelledException();
            }));
        }

        public static Promise<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(
            this Promise<TOuter> outer, Promise<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, Promise<TInner>, TResult> resultSelector)
        {
            return GroupJoin(outer, inner, outerKeySelector, innerKeySelector, resultSelector, EqualityComparer<TKey>.Default);
        }

        public static Promise<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(
            this Promise<TOuter> outer, Promise<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, Promise<TInner>, TResult> resultSelector,
            IEqualityComparer<TKey> comparer)
        {
            if (outer == null) throw new ArgumentNullException(nameof(outer));
            if (inner == null) throw new ArgumentNullException(nameof(inner));
            if (outerKeySelector == null) throw new ArgumentNullException(nameof(outerKeySelector));
            if (innerKeySelector == null) throw new ArgumentNullException(nameof(innerKeySelector));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));

            return outer.Then((outResult,s) => inner.Then((inResult,ss) => {
                if (comparer.Equals(outerKeySelector(outResult), innerKeySelector(inResult)))
                    return resultSelector(outResult, inner);
                throw new CancelledException();
            }));
        }

        public static Promise<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
            this Promise<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (elementSelector == null) throw new ArgumentNullException(nameof(elementSelector));

            return source.Then((result,s) => {
                var key     = keySelector(result);
                var element = elementSelector(result);
                return (IGrouping<TKey, TElement>)new OneElementGrouping<TKey, TElement>
                {
                    Key     = key,
                    Element = element
                };
            });
        }

        public static Promise<TSource> OrderBy<TSource, TKey>(
            this Promise<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            return source;
        }

        public static Promise<TSource> OrderByDescending<TSource, TKey>(
            this Promise<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            return source;
        }

        public static Promise<TSource> ThenBy<TSource, TKey>(
            this Promise<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            return source;
        }

        public static Promise<TSource> ThenByDescending<TSource, TKey>(
            this Promise<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            return source;
        }

        class OneElementGrouping<TKey, TElement> : IGrouping<TKey, TElement>
        {
            public   TKey     Key     { get; internal set; }
            internal TElement Element { private get; set; }
            public IEnumerator<TElement> GetEnumerator() { yield return Element; }
            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        }
    }
}
