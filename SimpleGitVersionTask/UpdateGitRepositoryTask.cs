using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SimpleGitVersion
{

    // WIP
    class UpdateGitRepositoryTask : Task 
    {
        [Required]
        public string SolutionDirectory { get; set; }

        [Required]
        public string CommitSha { get; set; }

        [Required]
        public string TagName { get; set; }

        public bool DeleteTag { get; set; }

        public UpdateGitRepositoryTask()
        {
        }

        public override bool Execute()
        {
            try
            {
                //using( var r = RepositoryWriter.LoadFromPath( SolutionDirectory ) )
                //{
                //}
                return true;
            }
            catch( Exception exception )
            {
                this.LogError( "Error occurred: " + exception );
                return false;
            }
        }

    }
}
