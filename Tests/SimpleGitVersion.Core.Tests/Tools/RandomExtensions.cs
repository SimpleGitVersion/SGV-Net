using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion.Core.Tests
{
    public static class RandomExtensions
    {
        /// <summary>
        /// Returns a nonnegative decimal floating point random number less than 1.0.
        /// (borrowed from Math.Net.)
        /// </summary>
        /// <param name="rnd">The random number generator.</param>
        /// <returns>
        /// A decimal floating point number greater than or equal to 0.0, and less than 1.0; that is, 
        /// the range of return values includes 0.0 but not 1.0.
        /// </returns>
        public static decimal NextDecimal( this Random rnd )
        {
            decimal candidate;

            // 50.049 % chance that the number is below 1.0. Try until we have one.
            // Guarantees that any decimal in the interval can
            // indeed be reached, with uniform probability.
            do
            {
                candidate = new decimal(
                    rnd.NextFullRangeInt32(),
                    rnd.NextFullRangeInt32(),
                    rnd.NextFullRangeInt32(),
                    false,
                    28 );
            }
            while( candidate >= 1.0m );
            return candidate;
        }

        /// <summary>
        /// Returns a random number of the full Int32 range. 
        /// (borrowed from Math.Net.)
        /// </summary>
        /// <param name="rnd">The random number generator.</param>
        /// <returns>
        /// A 32-bit signed integer of the full range, including 0, negative numbers,
        /// <see cref="Int32.MaxValue"/> and <see cref="Int32.MinValue"/>.
        /// </returns>
        public static int NextFullRangeInt32( this Random rnd )
        {
            var buffer = new byte[sizeof( int )];
            rnd.NextBytes( buffer );
            return BitConverter.ToInt32( buffer, 0 );
        }

    }
}
