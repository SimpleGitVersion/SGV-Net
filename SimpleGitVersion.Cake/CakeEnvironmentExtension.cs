using Cake.Core;
using Cake.Core.IO;
using System;

namespace CodeCake
{
    /// <summary>
    /// Provides extension methods ro <see cref="ICakeEnvironment"/>.
    /// </summary>
    public static class CakeEnvironmentExtension
    {
        class Reset : IDisposable
        {
            readonly ICakeEnvironment _e;
            readonly DirectoryPath _p;

            public Reset(ICakeEnvironment e, DirectoryPath p )
            {
                _e = e;
                _p = p;
            }

            public void Dispose()
            {
                _e.WorkingDirectory = _p;
            }
        }

        /// <summary>
        /// Temporary sets the <see cref="ICakeEnvironment.WorkingDirectory"/>.
        /// </summary>
        /// <param name="this">This <see cref="ICakeContext"/>.</param>
        /// <param name="path">The path to set.</param>
        /// <returns>Disposable that reverts the working folder to its original value.</returns>
        public static IDisposable SetWorkingDirectory(this ICakeEnvironment @this, string path)
        {
            return SetWorkingDirectory(@this, new DirectoryPath(path));
        }

        /// <summary>
        /// Temporary sets the <see cref="ICakeEnvironment.WorkingDirectory"/>.
        /// </summary>
        /// <param name="this">This <see cref="ICakeContext"/>.</param>
        /// <param name="path">The path to set.</param>
        /// <returns>Disposable that reverts the working folder to its original value.</returns>
        public static IDisposable SetWorkingDirectory(this ICakeEnvironment @this, DirectoryPath path)
        {
            var current = @this.WorkingDirectory;
            @this.WorkingDirectory = path;
            return new Reset(@this, current);
        }
    }
}
