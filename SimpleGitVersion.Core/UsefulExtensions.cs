using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SimpleGitVersion
{
    /// <summary>
    /// Miscellaneous extension methods.
    /// </summary>
    public static class UsefulExtensions
    {

        /// <summary>
        /// Returns the maximal element of the given sequence based on
        /// a projection of comparable keys. The sequence MUST NOT 
        /// be empty otherwise an <see cref="InvalidOperationException"/> is thrown.
        /// (borrowed from CK.Core.)
        /// </summary>
        /// <remarks>
        /// If more than one element has the maximal projected value, the first
        /// one encountered will be returned. This operator uses immediate execution, but
        /// only buffers a single result (the current maximal element).
        /// </remarks>
        /// <typeparam name="TSource">Type of the source sequence.</typeparam>
        /// <typeparam name="TKey">Type of the projected element. Must be <see cref="IComparable{TKey}"/>.</typeparam>
        /// <param name="this">Source sequence.</param>
        /// <param name="selector">Selector to use to pick the results to compare</param>
        /// <returns>The maximal element, according to the projection.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="this"/> or <paramref name="selector"/> is null</exception>
        /// <exception cref="InvalidOperationException"><paramref name="this"/> is empty</exception>       
        public static TSource MaxBy<TSource, TKey>( this IEnumerable<TSource> @this, Func<TSource, TKey> selector ) where TKey : IComparable<TKey>
        {
            if( @this == null ) throw new ArgumentNullException( "@this" );
            if( selector == null ) throw new ArgumentNullException( "selector" );
            using( IEnumerator<TSource> sourceIterator = @this.GetEnumerator() )
            {
                if( !sourceIterator.MoveNext() )
                {
                    throw new InvalidOperationException( "Sequence was empty." );
                }
                TSource max = sourceIterator.Current;
                TKey maxKey = selector( max );
                while( sourceIterator.MoveNext() )
                {
                    TSource candidate = sourceIterator.Current;
                    TKey candidateProjected = selector( candidate );
                    if( candidateProjected.CompareTo( maxKey ) > 0 )
                    {
                        max = candidate;
                        maxKey = candidateProjected;
                    }
                }
                return max;
            }
        }

        /// <summary>
        /// Binary search implementation that relies on <see cref="IComparable{TValue}"/> implemented by the <typeparamref name="T"/>.
        /// (Borrowed from CK.Core.)
        /// </summary>
        /// <typeparam name="T">Type of the elements. It must implement <see cref="IComparable{TValue}"/>.</typeparam>
        /// <typeparam name="TValue">Type of the value.</typeparam>
        /// <param name="this">This read only list of elements.</param>
        /// <param name="startIndex">The starting index in the list.</param>
        /// <param name="length">The number of elements to consider in the list.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>Same as <see cref="Array.BinarySearch(Array,object)"/>: negative index if not found which is the bitwise complement of (the index of the next element plus 1).</returns>
        public static int BinarySearch<T, TValue>( this IReadOnlyList<T> @this, int startIndex, int length, TValue value ) where T : IComparable<TValue>
        {
            int low = startIndex;
            int high = (startIndex + length) - 1;
            while( low <= high )
            {
                int mid = low + ((high - low) >> 1);
                int cmp = @this[mid].CompareTo( value );
                if( cmp == 0 ) return mid;
                if( cmp < 0 ) low = mid + 1;
                else high = mid - 1;
            }
            return ~low;
        }

        /// <summary>
        /// Binary search implementation that relies on <see cref="IComparable{TValue}"/> implemented by the <typeparamref name="T"/>.
        /// (Borrowed from CK.Core.)
        /// </summary>
        /// <typeparam name="T">Type of the elements. It must implement <see cref="IComparable{TValue}"/>.</typeparam>
        /// <typeparam name="TValue">Type of the value.</typeparam>
        /// <param name="this">This read only list of elements.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>Same as <see cref="Array.BinarySearch(Array,object)"/>: negative index if not found which is the bitwise complement of (the index of the next element plus 1).</returns>
        public static int BinarySearch<T, TValue>( this IReadOnlyList<T> @this, TValue value ) where T : IComparable<TValue>
        {
            return BinarySearch( @this, 0, @this.Count, value );
        }

    }
}
