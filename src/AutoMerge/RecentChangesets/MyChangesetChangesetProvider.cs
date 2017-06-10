using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AutoMerge
{
	public class MyChangesetChangesetProvider : ChangesetProviderBase
	{
	    private readonly int _maxChangesetCount;

		public MyChangesetChangesetProvider(IServiceProvider serviceProvider, int maxChangesetCount)
			: base(serviceProvider)
		{
		    _maxChangesetCount = maxChangesetCount;
		}

	    protected override List<ChangesetViewModel> GetChangesetsInternal(string source, string target)
		{
			var changesets = new List<ChangesetViewModel>();

			if (!string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(target))
			{
				var changesetService = GetChangesetService();

				if (changesetService != null)
				{
				    //var projectName = GetProjectName();
                    //var tfsChangesets = changesetService.GetUserChangesets(projectName, userLogin, _maxChangesetCount);
				    var tfsChangesets = changesetService.GetUserChangesets(source, target);
                    changesets = tfsChangesets
						.Select(tfsChangeset => ToChangesetViewModel(tfsChangeset, changesetService))
						.ToList();
				}
			}

			return changesets;
		}

	    public List<BranchObject> GetPossibliBranches()
	    {
	        var vcs = GetVersionControlServer();
            
            List<BranchObject> possibleBranches = new List<BranchObject>();

	        BranchObject[] branches = vcs.QueryRootBranchObjects(RecursionType.None);

	        foreach (var rootBranch in branches)
	        {
	            possibleBranches.AddRange(GetAllBranches(rootBranch, true, RecursionType.Full));
	        }

	        return possibleBranches;
	    }

	    public IEnumerable<BranchObject> GetAllBranches(BranchObject bo, bool includeSelf, RecursionType recursionType)
	    {
	        var vcs = GetVersionControlServer();

            BranchObject[] branches = vcs.QueryBranchObjects(bo.Properties.RootItem, recursionType);
	        return branches.Where(b =>
	            !b.Properties.RootItem.IsDeleted
	            && (includeSelf || !b.Properties.RootItem.Equals(bo.Properties.RootItem))
	            && (b.ChildBranches.Length > 0 || b.Properties.ParentBranch != null));
	    }
    }
}
