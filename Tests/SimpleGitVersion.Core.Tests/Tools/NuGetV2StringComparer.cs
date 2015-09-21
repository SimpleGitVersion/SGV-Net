using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Semver;
using NUnit.Framework;
using System.Text.RegularExpressions;

namespace SimpleGitVersion
{
    public class NuGetV2StringComparer : IComparer<string>
    {
         static Regex _nuGetV2 = new Regex( @"^(?<Version>\d+(\s*\.\s*\d+){0,3})(?<Release>-[a-z][0-9a-z-]*)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture );

        public static readonly IComparer<string> DefaultComparer = new NuGetV2StringComparer();

        static public void CheckValid( string v2Version )
        {
            var m = _nuGetV2.Match( v2Version );
            Assert.That( m.Success, "Invalid version: {0}", v2Version );
            Assert.That( m.Groups["Release"].Length, Is.LessThanOrEqualTo( 20 ), "Invalid version (too long): {0}", v2Version );
        }

        public int Compare( string x, string y )
        {
            var vX = SemVersion.Parse( x, true );
            var vY = SemVersion.Parse( y, true );
            CheckValid( x );
            CheckValid( y );
            Assert.That( vX.Prerelease.Length <= 20, "{0} => PreRelease must not contain more than 20 characters (lenght is {1}).", x, x.Length );
            Assert.That( vY.Prerelease.Length <= 20, "{0} => PreRelease must not contain more than 20 characters (lenght is {1}).", y, y.Length );
            int cmp = vX.Major - vY.Major;
            if( cmp != 0 ) return cmp;
            cmp = vX.Minor - vY.Minor;
            if( cmp != 0 ) return cmp;
            cmp = vX.Patch - vY.Patch;
            if( cmp != 0 ) return cmp;
            if( vX.Prerelease.Length == 0 && vY.Prerelease.Length == 0 ) return 0;
            if( vX.Prerelease.Length == 0 ) return 1;
            if( vY.Prerelease.Length == 0 ) return -1;
            return string.CompareOrdinal( vX.Prerelease, vY.Prerelease );
        }
    }
}
