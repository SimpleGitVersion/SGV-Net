using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SimpleGitVersion
{
    static class SGVSchema
    {
        public static readonly XNamespace SVGNS = XNamespace.Get( "http://csemver.org/schemas/2015" );
        public static readonly XName RepositoryInfo = SVGNS + "RepositoryInfo";
        public static readonly XName StartingVersionForCSemVer = SVGNS + "StartingVersionForCSemVer";
        public static readonly XName IgnoreModifiedFiles = SVGNS + "IgnoreModifiedFiles";
        public static readonly XName Add = SVGNS + "Add";
        public static readonly XName Branches = SVGNS + "Branches";
        public static readonly XName Branch = SVGNS + "Branch";

        public static readonly XName Name = SVGNS + "Name";
        public static readonly XName CIVersionMode = SVGNS + "CIVersionMode";
        public static readonly XName VersionName = SVGNS + "VersionName";
        public static readonly XName IgnoreDirtyWorkingFolder = SVGNS + "IgnoreDirtyWorkingFolder";

        public static readonly XName NameNoNS = XNamespace.None + "Name";
        public static readonly XName CIVersionModeNoNS = XNamespace.None + "CIVersionMode";
        public static readonly XName VersionNameNoNS = XNamespace.None + "VersionName";
        public static readonly XName IgnoreDirtyWorkingFolderNoNS = XNamespace.None + "IgnoreDirtyWorkingFolder";


    }
}
