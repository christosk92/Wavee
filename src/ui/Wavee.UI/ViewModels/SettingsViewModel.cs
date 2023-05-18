using System.Reactive.Linq;
using ReactiveUI;
using LanguageExt;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.Infrastructure.Traits;

namespace Wavee.UI.ViewModels;

public sealed class SettingsViewModel<R> : ReactiveObject where R : struct, HasFile<R>, HasDirectory<R>, HasLocalPath<R>
{
    private bool _setupCompleted;
    private int _setupProgress;
    private int _width;
    private int _height;

    public SettingsViewModel(R runtime)
    {
        Instance = this;
        // this.WhenAnyValue(
        //         x => x.SetupCompleted,
        //         x => x.SetupProgress)
        //     .Skip(1)
        //     .ObserveOn(RxApp.TaskpoolScheduler)
        //     .Select(async (___) =>
        //     {
        //         var aff =
        //             from _ in UiConfig<R>.SetSetupCompleted(SetupCompleted)
        //             from __ in UiConfig<R>.SetSetupProgress(SetupProgress)
        //             select Unit.Default;
        //
        //         var run = await aff.Run(runtime);
        //     })
        //     .Subscribe();

        this.WhenAnyValue(
                x => x.Height,
                x => x.Width)
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Select(async (___) =>
            {
                var aff =
                    from _ in UiConfig<R>.SetWindowWidth((uint)Width)
                    from __ in UiConfig<R>.SetWindowHeight((uint)Height)
                    select Unit.Default;

                var run = await aff.Run(runtime);
            })
            .Subscribe();
    }
    public static SettingsViewModel<R> Instance { get; private set; }

    // #region Setup
    // public bool SetupCompleted
    // {
    //     get => _setupCompleted;
    //     set => this.RaiseAndSetIfChanged(ref _setupCompleted, value);
    // }
    //
    // public int SetupProgress
    // {
    //     get => _setupProgress;
    //     set => this.RaiseAndSetIfChanged(ref _setupProgress, value);
    // }
    // #endregion


    #region Window 

    public int Width
    {
        get => _width;
        set => this.RaiseAndSetIfChanged(ref _width, value);
    }

    public int Height
    {
        get => _height;
        set => this.RaiseAndSetIfChanged(ref _height, value);
    }

    #endregion
}