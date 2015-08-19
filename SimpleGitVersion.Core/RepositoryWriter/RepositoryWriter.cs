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

        public bool ValidReleaseTag( string exactTagName )
        {
            if( exactTagName == null ) throw new ArgumentNullException();
            Tag cTag = _repo.Tags[ exactTagName ];
            if( cTag == null ) return false;
            Commit c = cTag.ResolveTarget() as Commit;
            if( c == null ) return false;
            ReleaseTagVersion t = ReleaseTagVersion.TryParse( exactTagName );
            if( !t.IsValid ) return false;
            if( t.IsMarkedValid && t.IsPreReleaseNameStandard ) return true;
            var tV = t.MarkValid();
            _repo.Tags.Add( tV.ToString( ReleaseTagFormat.Normalized ), c, true );
            _repo.Tags.Remove( exactTagName );
            return true;
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
