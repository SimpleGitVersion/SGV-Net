using Cake.Core;
using Cake.Core.Diagnostics;
using System;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace Code.Cake
{
    /// <summary>
    /// Provides extension methods for Cake context.
    /// </summary>
    public static class DotNetSupport
    {
        /// <summary>
        /// Runs dotnet restore.
        /// </summary>
        /// <param name="context">This cake context.</param>
        /// <param name="config">The configuration to use.</param>
        public static void DotNetRestore( this ICakeContext context, Action<DotNetRestoreSettings> config )
        {
            var c = new DotNetRestoreSettings();
            config( c );
            var b = new StringBuilder();
            b.Append( "dotnet restore " );
            c.ToString( b );
            SimpleCommandExecutor.RunSuccessfullCmd( context, b.ToString() );
        }

        /// <summary>
        /// Runs dotnet build.
        /// </summary>
        /// <param name="context">This cake context.</param>
        /// <param name="config">The configuration to use.</param>
        public static void DotNetBuild( this ICakeContext context, Action<DotNetBuildSettings> config )
        {
            var c = new DotNetBuildSettings();
            config( c );
            var b = new StringBuilder();
            b.Append( "dotnet build " );
            c.ToString( b );
            SimpleCommandExecutor.RunSuccessfullCmd( context, b.ToString() );
        }

        /// <summary>
        /// Runs dotnet pack.
        /// </summary>
        /// <param name="context">This cake context.</param>
        /// <param name="config">The configuration to use.</param>
        public static void DotNetPack( this ICakeContext context, Action<DotNetPackSettings> config )
        {
            var c = new DotNetPackSettings();
            config( c );
            var b = new StringBuilder();
            b.Append( "dotnet pack " );
            c.ToString( b );
            SimpleCommandExecutor.RunSuccessfullCmd( context, b.ToString() );
        }

    }
}
