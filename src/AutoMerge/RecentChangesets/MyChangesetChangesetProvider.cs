using System;
using System.Collections.Generic;
using System.Linq;

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
	}
}
