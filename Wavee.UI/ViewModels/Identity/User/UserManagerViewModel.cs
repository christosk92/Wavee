using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Wavee.UI.Identity.Messaging;
using Wavee.UI.Identity.Users;
using Wavee.UI.Identity.Users.Contracts;
using Wavee.UI.Navigation;
using Wavee.UI.ViewModels.Shell;

namespace Wavee.UI.ViewModels.Identity.User
{
    public class UserManagerViewModel : ObservableRecipient,
        IRecipient<LoggedInUserChangedMessage>,
        IRecipient<UserAddedMessage>
    {
        private readonly SourceList<WaveeUserViewModel> _usersSourceList = new();
        private readonly ObservableCollectionExtended<WaveeUserViewModel> _users = new();
        private readonly WaveeUserManagerFactory _userManager;
        private readonly Subject<WaveeUserViewModel?> _userManagerSubject;

        public UserManagerViewModel(WaveeUserManagerFactory factory)
        {
            this.IsActive = true;
            _userManager = factory;
            _userManagerSubject = new Subject<WaveeUserViewModel?>();

            _usersSourceList
                .Connect()
                .Sort(SortExpressionComparer<WaveeUserViewModel>
                    .Descending(i => i.IsLoggedIn)
                    .ThenByAscending(i => i.DisplayName))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(_users)
                .Subscribe();


            // Register the receiver in a module
            WeakReferenceMessenger.Default.Register<UserManagerViewModel, RequestViewModelForUser>(this,
                (r, m) =>
            {
                m.Reply(Task.Run(async () =>
                {
                    while (!r._users.Any(a => a.User.Id == m.UserId && a.User.ServiceType == m.ForService))
                    {
                        await Task.Delay(10, m.CancellatonToken);
                    }

                    return r._users.First(a => a.User.Id == m.UserId && a.User.ServiceType == m.ForService);
                }));
            });

        }

        public IObservable<WaveeUserViewModel?> CurrentUser => _userManagerSubject;
        public WaveeUserViewModel? CurrentUserVal { get; private set; }

        public ObservableCollection<WaveeUserViewModel> Users => _users;

        private void InsertUser(WaveeUserViewModel vm)
        {
            _usersSourceList.Add(vm);
        }


        public void Receive(LoggedInUserChangedMessage message)
        {
            //open user
            foreach (var signoutFor in _users.Where(a => a.User != message.Value && a.IsLoggedIn))
            {
                signoutFor.SignOut();
            }

            var vm = _users.Single(a => a.User == message.Value);
            _userManagerSubject.OnNext(vm);
            CurrentUserVal = vm;

            NavigationService.Instance.SetRoot<ShellViewModel>();
        }

        public void Receive(UserAddedMessage message)
        {
            var vm = WaveeUserViewModel.Create(message.Value);

            InsertUser(vm);
        }

    }
}
