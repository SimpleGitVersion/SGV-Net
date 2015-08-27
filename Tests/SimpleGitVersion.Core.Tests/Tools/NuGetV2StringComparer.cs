using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Semver;
using NUnit.Framework;

namespace SimpleGitVersion
{
    public class NuGetV2StringComparer : IComparer<string>
    {

        public static readonly IComparer<string> Default = new NuGetV2StringComparer();

        public int Compare( string x, string y )
        {
            var vX = SemVersion.Parse( x, true );
            var vY = SemVersion.Parse( y, true );
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
