﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AutoMerge
{
    public abstract class ChangesetProviderBase : IChangesetProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Lazy<ChangesetService> _changesetService;

        protected ChangesetProviderBase(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _changesetService = new Lazy<ChangesetService>(InitChangesetService);
        }

        public Task<List<ChangesetViewModel>> GetChangesets(string source, string target)
        {
            return Task.Run(() => GetChangesetsInternal(source, target));
        }

        protected abstract List<ChangesetViewModel> GetChangesetsInternal(string source, string target);

        protected ChangesetViewModel ToChangesetViewModel(Changeset tfsChangeset, ChangesetService changesetService)
        {
            var changesetViewModel = new ChangesetViewModel
            {
                CommitterDisplayName = tfsChangeset.OwnerDisplayName,
                ChangesetId = tfsChangeset.ChangesetId,
                Comment = tfsChangeset.Comment,
                Branches = changesetService.GetAssociatedBranches(tfsChangeset.ChangesetId)
                    .Select(i => i.Item)
                    .ToList()
            };

            return changesetViewModel;
        }

        protected ChangesetService GetChangesetService()
        {
            return _changesetService.Value;
        }

        private ChangesetService InitChangesetService()
        {
            var context = VersionControlNavigationHelper.GetTeamFoundationContext(_serviceProvider);
            if (context != null && VersionControlNavigationHelper.IsConnectedToTfsCollectionAndProject(context))
            {
                var vcs = context.TeamProjectCollection.GetService<VersionControlServer>();
                if (vcs != null)
                {
                    return new ChangesetService(vcs);
                }
            }
            return null;
        }

        protected VersionControlServer GetVersionControlServer()
        {
            var context = VersionControlNavigationHelper.GetTeamFoundationContext(_serviceProvider);
            if (context == null || VersionControlNavigationHelper.IsConnectedToTfsCollectionAndProject(context) == false)
            {
                return null;
            }
            var vcs = context.TeamProjectCollection.GetService<VersionControlServer>();
            return vcs;
        }

        protected string GetProjectName()
        {
            var context = VersionControlNavigationHelper.GetTeamFoundationContext(_serviceProvider);
            if (context != null)
            {
                return context.TeamProjectName;
            }
            return null;
        }
    }
}
