using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGitVersion
{
    class VersionOccurrence
    {
        public readonly string Version;
        public readonly int Start;
        public readonly int Length;
        public readonly bool ExpectComma;
        public int End { get { return Start + Length; } }
        public bool IsNakedVersionNumber { get { return Length == Version.Length + 2; } }

        public VersionOccurrence( string version, int start, int length, bool expectComma )
        {
            Version = version;
            Start = start;
            Length = length;
            ExpectComma = expectComma;
        }
    }

}
