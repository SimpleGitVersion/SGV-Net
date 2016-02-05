using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace SimpleGitVersion
{
    /// <summary>
    /// WIP
    /// </summary>
    class RepositoryWriter : IDisposable
    {
        readonly string _userName;
        readonly string _userEmail;
        readonly Repository _repo;

        RepositoryWriter( Repository r, string userName = null, string userEmail = null )
        {
            if( (userEmail == null) != (userName== null ) ) throw new ArgumentException();
            _repo = r;
            _userEmail = userEmail;
            _userName = userName;
        }

        Signature GetSignature()
        {
            return _userName != null ? new Signature( _userName, _userEmail, DateTimeOffset.Now ) : _repo.Config.BuildSignature( DateTimeOffset.Now );
        }

        public static RepositoryWriter LoadFromPath( string path )
        {
            using( var repo = GitHelper.LoadFromPath( path ) )
            {
                return new RepositoryWriter( repo );
            }
        }

        public void Dispose()
        {
            _repo.Dispose();
        }
    }
}

