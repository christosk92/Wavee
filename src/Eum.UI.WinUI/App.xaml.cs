// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.IO;
using Eum.Helpers;
using Eum.Logging;
using Eum.UI.Database;
using Eum.UI.Helpers;
using Eum.UI.Users;
using Eum.UI.ViewModels;
using Eum.UI.ViewModels.Login;
using Eum.UI.ViewModels.Profile.Create;
using Eum.UI.WinUI.Helper;
using Eum.Users;
using Path = System.IO.Path;
using ServiceCollection = Microsoft.Extensions.DependencyInjection.ServiceCollection;
using Eum.UI.ViewModels.Sidebar;
using LiteDB;
using JsonSerializer = System.Text.Json.JsonSerializer;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UI.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            var serviceCollection = new ServiceCollection();
      
            // Initialize the logger.
            string dataDir = EnvironmentHelpers.GetDataDir(Path.Combine("Eum", "WinUI"));
            S_Log.Instance.InitializeDefaults(dataDir, null);
            S_Log.Instance.LogDebug($"Eum was started with these argument(s): none");


            serviceCollection.AddSingleton<ILiteDatabase>(new LiteDatabase(Path.Combine(dataDir, "data.db")));
            serviceCollection.AddTransient<TracksRepository>();
            serviceCollection.AddTransient<ProfilesViewModel>();
            serviceCollection.AddSingleton<MainViewModel>();
            serviceCollection.AddSingleton(new UserDirectories(dataDir));
            serviceCollection.AddSingleton(s => new UserManager(dataDir, s.GetRequiredService<UserDirectories>()));

            serviceCollection.AddSingleton<UserManagerViewModel>();

            serviceCollection.AddSingleton(_ =>
            {
                var assetsPics = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledPath, "Assets",
                    "pics.json");
                using var fs = File.OpenRead(assetsPics);
                return JsonSerializer.Deserialize<IList<GroupedprofilePictures>>(fs);
            });

            serviceCollection.AddTransient<IDialogHelper, DialogHelper>();
            Ioc.Default.ConfigureServices(serviceCollection.BuildServiceProvider());



            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MWindow = new MainWindow();
            MWindow.Activate();
        }

        public static Window MWindow;

    }
}
