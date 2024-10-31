using Microsoft.Extensions.Hosting;
using System.Net;
using Wavee.ViewModels.Models.Users;
using Wavee.ViewModels.Service;
using Wavee.ViewModels.State;

namespace Wavee.ViewModels;

public static class Services
{
    public static SingleInstanceChecker SingleInstanceChecker { get; private set; } = null!;
    public static string PersistentConfigFilePath { get; private set; } = null!;
    public static PersistentConfig PersistentConfig { get; private set; } = null!;
    public static TerminateService TerminateService { get; private set; } = null!;
    public static UserManager UserManager { get; private set; } = null!;

    public static UiConfig UiConfig { get; private set; } = null!;


    /// <summary>
    /// Initializes global services used by fluent project.
    /// </summary>
    public static void Initialize(
        string dataDir,
        string configFilePath,
        PersistentConfig persistentConfig,
        UiConfig uiConfig,
        SingleInstanceChecker singleInstanceChecker,
        TerminateService terminateService)
    {
        PersistentConfigFilePath = configFilePath;
        PersistentConfig = persistentConfig;

        SingleInstanceChecker = singleInstanceChecker;
        TerminateService = terminateService;
        UiConfig = uiConfig;

        UserFactory userFactory = new(dataDir);

        UserManager = new UserManager(dataDir, new UserDirectories(dataDir), userFactory);
    }
}