using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using AutoMerge.Events;
using AutoMerge.Prism;
using AutoMerge.Prism.Command;
using AutoMerge.Prism.Events;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using Microsoft.TeamFoundation.VersionControl.Client;
using Task = System.Threading.Tasks.Task;

using TeamExplorerSectionViewModelBase = AutoMerge.Base.TeamExplorerSectionViewModelBase;

namespace AutoMerge
{
    public sealed class RecentChangesetsViewModel : TeamExplorerSectionViewModelBase
    {
        private readonly string _baseTitle;
        private readonly IEventAggregator _eventAggregator;

        public RecentChangesetsViewModel(ILogger logger)
            : base(logger)
        {
            Title = Resources.RecentChangesetSectionName;
            IsVisible = true;
            IsExpanded = true;
            IsBusy = false;
            Changesets = new ObservableCollection<ChangesetViewModel>();
            _baseTitle = Title;

            _eventAggregator = EventAggregatorFactory.Get();
            _eventAggregator.GetEvent<MergeCompleteEvent>()
                .Subscribe(OnMergeComplete);

            ViewChangesetDetailsCommand = new DelegateCommand(ViewChangesetDetailsExecute, ViewChangesetDetailsCanExecute);
            ToggleAddByIdCommand = new DelegateCommand(ToggleAddByIdExecute, ToggleAddByIdCanExecute);
            CancelAddChangesetByIdCommand = new DelegateCommand(CancelAddByIdExecute);
            AddChangesetByIdCommand = new DelegateCommand(AddChangesetByIdExecute, AddChangesetByIdCanExecute);

            SelectSourceBranchCommand = new SimpleDeletegateCommand(ExecuteSelectSourceBranch);
            SelectTargetBranchCommand = new SimpleDeletegateCommand(ExecuteSelectTargetBranch);
            SaveDefaultBranchesCommand = new SimpleDeletegateCommand(ExecuteSaveBranches, CanExecuteSaveBranches);
        }

        private void ExecuteSaveBranches(object obj)
        {
            SaveDefaultbranches();
            _branchesAreDirty = false;
        }

        private bool CanExecuteSaveBranches(object obj)
        {
            return _branchesAreDirty;
        }

        public ChangesetViewModel SelectedChangeset
        {
            get
            {
                return _selectedChangeset;
            }
            set
            {
                _selectedChangeset = value;
                RaisePropertyChanged("SelectedChangeset");
                _eventAggregator.GetEvent<SelectChangesetEvent>().Publish(value);
            }
        }
        private ChangesetViewModel _selectedChangeset;

        public ObservableCollection<ChangesetViewModel> Changesets
        {
            get
            {
                return _changesets;
            }
            private set
            {
                _changesets = value;
                RaisePropertyChanged("Changesets");
            }
        }
        private ObservableCollection<ChangesetViewModel> _changesets;

        public bool ShowAddByIdChangeset
        {
            get
            {
                return _showAddByIdChangeset;
            }
            set
            {
                _showAddByIdChangeset = value;
                RaisePropertyChanged("ShowAddByIdChangeset");
            }
        }
        private bool _showAddByIdChangeset;

        public string ChangesetIdsText
        {
            get
            {
                return _changesetIdsText;
            }
            set
            {
                _changesetIdsText = value;
                RaisePropertyChanged("ChangesetIdsText");
                InvalidateCommands();
            }
        }
        private string _changesetIdsText;

        public DelegateCommand ViewChangesetDetailsCommand { get; private set; }

        public DelegateCommand ToggleAddByIdCommand { get; private set; }

        public DelegateCommand AddChangesetByIdCommand { get; private set; }

        public DelegateCommand CancelAddChangesetByIdCommand { get; private set; }

        private void ViewChangesetDetailsExecute()
        {
            var changesetId = SelectedChangeset.ChangesetId;
            TeamExplorerUtils.Instance.NavigateToPage(TeamExplorerPageIds.ChangesetDetails, ServiceProvider, changesetId);
        }

        private bool ViewChangesetDetailsCanExecute()
        {
            return SelectedChangeset != null;
        }

        private async void OnMergeComplete(bool obj)
        {
            await RefreshAsync();
        }

        protected override async Task InitializeAsync(object sender, SectionInitializeEventArgs e)
        {
            if (e.Context == null)
            {
                await RefreshAsync();
            }
            else
            {
                RestoreContext(e);
            }
        }

        protected override async Task RefreshAsync()
        {
            InitBranchselectors();

            Changesets.Clear();

            var changesetProvider = new MyChangesetChangesetProvider(ServiceProvider, Settings.Instance.ChangesetCount);
            //var userLogin = VersionControlNavigationHelper.GetAuthorizedUser(ServiceProvider);

            //var source = "$/Bookhus husudlejning/Bookhus husudlejning/Feature-Newdesign";
            //var target = "$/Bookhus husudlejning/Bookhus husudlejning/Main_Version7.1";

            var source = SourceBranch;
            var target = TargetBranch;

            Logger.Info("Getting changesets ...");
            var changesets = await changesetProvider.GetChangesets(source, target);
            Logger.Info("Getting changesets end");

            Changesets = new ObservableCollection<ChangesetViewModel>(changesets);
            UpdateTitle();

            if (Changesets.Count > 0)
            {
                if (SelectedChangeset == null || SelectedChangeset.ChangesetId != Changesets[0].ChangesetId)
                    SelectedChangeset = Changesets[0];
            }
        }

        private void UpdateTitle()
        {
            Title = Changesets.Count > 0
                ? string.Format("{0} ({1})", _baseTitle, Changesets.Count)
                : _baseTitle;
        }

        private void ToggleAddByIdExecute()
        {
            try
            {
                ShowAddByIdChangeset = true;
                InvalidateCommands();
                ResetAddById();
                SetMvvmFocus(RecentChangesetFocusableControlNames.ChangesetIdTextBox);
            }
            catch (Exception ex)
            {
                ShowException(ex);
                throw;
            }
        }

        private bool ToggleAddByIdCanExecute()
        {
            return !ShowAddByIdChangeset;
        }

        private void CancelAddByIdExecute()
        {
            try
            {
                ShowAddByIdChangeset = false;
                InvalidateCommands();
                SetMvvmFocus(RecentChangesetFocusableControlNames.AddChangesetByIdLink);
                ResetAddById();
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private void ResetAddById()
        {
            ChangesetIdsText = string.Empty;
        }

        private async void AddChangesetByIdExecute()
        {
            ShowBusy();
            try
            {
                var changesetIds = GeChangesetIdsToAdd(ChangesetIdsText);
                if (changesetIds.Count > 0)
                {
                    var changesetProvider = new ChangesetByIdChangesetProvider(ServiceProvider, changesetIds);
                    var changesets = await changesetProvider.GetChangesets(null, null);

                    if (changesets.Count > 0)
                    {
                        Changesets.Add(changesets[0]);
                        SelectedChangeset = changesets[0];
                        SetMvvmFocus(RecentChangesetFocusableControlNames.ChangesetList);
                        UpdateTitle();
                    }
                    ShowAddByIdChangeset = false;
                }
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
            HideBusy();
        }

        private bool AddChangesetByIdCanExecute()
        {
            try
            {
                return GeChangesetIdsToAdd(ChangesetIdsText).Count > 0;
            }
            catch (Exception ex)
            {
                ShowException(ex);
                TeamFoundationTrace.TraceException(ex);
            }
            return false;
        }

        private static List<int> GeChangesetIdsToAdd(string text)
        {
            var list = new List<int>();
            var idsStrArray = string.IsNullOrEmpty(text) ? new string[0] : text.Split(new[] { ',', ';' });
            if (idsStrArray.Length > 0)
            {
                foreach (var idStr in idsStrArray)
                {
                    int result;
                    if (int.TryParse(idStr.Trim(), out result) && result > 0)
                        list.Add(result);
                }
            }
            return list;
        }

        private void InvalidateCommands()
        {
            ViewChangesetDetailsCommand.RaiseCanExecuteChanged();
            ToggleAddByIdCommand.RaiseCanExecuteChanged();
            CancelAddChangesetByIdCommand.RaiseCanExecuteChanged();
            AddChangesetByIdCommand.RaiseCanExecuteChanged();
        }

        public override void Dispose()
        {
            base.Dispose();
            _eventAggregator.GetEvent<MergeCompleteEvent>().Unsubscribe(OnMergeComplete);
        }

        public override void SaveContext(object sender, SectionSaveContextEventArgs e)
        {
            base.SaveContext(sender, e);
            var context = new RecentChangesetsViewModelContext
            {
                ChangesetIdsText = ChangesetIdsText,
                Changesets = Changesets,
                SelectedChangeset = SelectedChangeset,
                ShowAddByIdChangeset = ShowAddByIdChangeset,
                Title = Title
            };

            e.Context = context;
        }

        private void RestoreContext(SectionInitializeEventArgs e)
        {
            var context = (RecentChangesetsViewModelContext)e.Context;
            ChangesetIdsText = context.ChangesetIdsText;
            Changesets = context.Changesets;
            SelectedChangeset = context.SelectedChangeset;
            ShowAddByIdChangeset = context.ShowAddByIdChangeset;
            Title = context.Title;
        }

        protected override void OnContextChanged(object sender, ContextChangedEventArgs e)
        {
            Refresh();
        }


        // branchselectors
        public ICommand SelectSourceBranchCommand { get; private set; }
        public ICommand SelectTargetBranchCommand { get; private set; }
        public ICommand SaveDefaultBranchesCommand { get; private set; }
        private string _systemDefaultSourceBranch;
        private string _systemDefaultTargetBranch;
        private string _sourceBranch;
        private string _targetBranch;
        private BranchObject _sourceBranchObject;
        private BranchObject _targetBranchObject;
        private List<BranchObject> _sourceBranches, _targetBranches;
        private bool _branchesAreDirty;
        private void ExecuteSelectSourceBranch(object branchParam)
        {
            BranchObject s = branchParam as BranchObject;
            SetSourceBranch(s);
        }
        private void ExecuteSelectTargetBranch(object branchParam)
        {
            BranchObject t = branchParam as BranchObject;
            SetTargetBranch(t);
        }
        public string SourceBranch
        {
            get
            {
                return _sourceBranch;
            }
            set
            {
                if (_sourceBranch == value) return;
                _sourceBranch = value;
                _branchesAreDirty = _sourceBranch != _systemDefaultSourceBranch;
                SetTargetBranchProperties(_sourceBranchObject);
                RaisePropertyChanged("SourceBranch");
            }
        }

        public string TargetBranch
        {
            get
            {
                return _targetBranch;
            }
            set
            {
                if (_targetBranch == value) return;
                _targetBranch = value;
                _branchesAreDirty = _targetBranch != _systemDefaultTargetBranch;
                RaisePropertyChanged("TargetBranch");
            }
        }

        private async void SaveDefaultbranches()
        {
            try
            {
                if (String.IsNullOrEmpty(SourceBranch) || String.IsNullOrEmpty(TargetBranch)) return;

                Environment.SetEnvironmentVariable("DefaultSourceBranch", SourceBranch, EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable("DefaultTargetBranch", TargetBranch, EnvironmentVariableTarget.User);

                _systemDefaultSourceBranch = SourceBranch;
                _systemDefaultTargetBranch = TargetBranch;

                await RefreshAsync();
            }
            catch (Exception se)
            {
                Debug.WriteLine("Cannot save default source and target branch due to a security exception. " + se.ToString());
            }
        }

        public List<BranchObject> SourceBranches
        {
            get
            {
                return _sourceBranches;
            }
            set
            {
                _sourceBranches = value;
                RaisePropertyChanged("SourceBranches");
            }
        }

        public List<BranchObject> TargetBranches
        {
            get
            {
                return _targetBranches;
            }
            set
            {
                _targetBranches = value;
                RaisePropertyChanged("TargetBranches");
            }
        }

        private void InitBranchselectors()
        {
            PopulateBranchLists();
            PopulateFromDefaults();
        }

        private void PopulateFromDefaults()
        {
            _systemDefaultSourceBranch = Environment.GetEnvironmentVariable("DefaultSourceBranch", EnvironmentVariableTarget.User);
            _systemDefaultTargetBranch = Environment.GetEnvironmentVariable("DefaultTargetBranch", EnvironmentVariableTarget.User);

            if (_systemDefaultSourceBranch == null || _systemDefaultTargetBranch == null || SourceBranches == null || TargetBranches == null) return;

            var sb = SourceBranches.FirstOrDefault(s => s.Properties.RootItem.Item.Equals(_systemDefaultSourceBranch));
            SetSourceBranch(sb);
            var tb = TargetBranches.FirstOrDefault(t => t.Properties.RootItem.Item.Equals(_systemDefaultTargetBranch));
            SetTargetBranch(tb);
            _branchesAreDirty = sb == null || tb == null;
        }

        private void SetSourceBranch(BranchObject s)
        {
            if (s == null) return;
            _sourceBranchObject = s;
            SourceBranch = s.Properties.RootItem.Item;
        }

        private void SetTargetBranch(BranchObject t)
        {
            if (t == null) return;
            _targetBranchObject = t;
            TargetBranch = t.Properties.RootItem.Item;
        }

        private void PopulateBranchLists()
        {
            SetSourceBranchProperties();
            SetTargetBranchProperties(_sourceBranchObject);
        }

        private void SetSourceBranchProperties()
        {
            var changesetProvider = new MyChangesetChangesetProvider(ServiceProvider, Settings.Instance.ChangesetCount);
            var branches = changesetProvider.GetPossibliBranches();

            if (!branches.Any())
            {
                SourceBranches = null;
                SourceBranch = null;
                TargetBranch = null;
                TargetBranches = null;
                return;
            }

            SourceBranches = branches;
            _sourceBranchObject = branches.First();
            SourceBranch = _sourceBranchObject.Properties.RootItem.Item;
        }
        private void SetTargetBranchProperties(BranchObject source)
        {
            var changesetProvider = new MyChangesetChangesetProvider(ServiceProvider, Settings.Instance.ChangesetCount);

            List<BranchObject> possibleTargetBranches = new List<BranchObject>();
            possibleTargetBranches.AddRange(changesetProvider.GetAllBranches(source, false, RecursionType.OneLevel));

            //add parent as target in addition to any child branches
            if (source.Properties.ParentBranch != null)
            {
                possibleTargetBranches.Add(SourceBranches.Single(sb => sb.Properties.RootItem.Equals(source.Properties.ParentBranch))); //can assume using the source list as merging is happen
            }

            if (!possibleTargetBranches.Any())
            {
                TargetBranches = null;
                TargetBranch = null;
                return;
            }
            TargetBranches = possibleTargetBranches;
            TargetBranch = possibleTargetBranches.First().Properties.RootItem.Item;
        }
    }
}
