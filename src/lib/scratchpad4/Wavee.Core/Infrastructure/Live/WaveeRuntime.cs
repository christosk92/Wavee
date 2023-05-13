using System.Linq.Expressions;
using LanguageExt.Effects.Database;
using LinqToDB;
using LinqToDB.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wavee.Core.Infrastructure.Traits;

namespace Wavee.Core.Infrastructure.Live;

public readonly struct WaveeRuntime :
    HasCancel<WaveeRuntime>,
    HasTCP<WaveeRuntime>,
    HasHttp<WaveeRuntime>,
    HasAudioOutput<WaveeRuntime>,
    HasWebsocket<WaveeRuntime>,
    HasLog<WaveeRuntime>,
    HasDatabase<WaveeRuntime>
{
    readonly RuntimeEnv env;

    /// <summary>
    /// Constructor
    /// </summary>
    WaveeRuntime(RuntimeEnv env) =>
        this.env = env;

    /// <summary>
    /// Configuration environment accessor
    /// </summary>
    internal RuntimeEnv Env =>
        env ?? throw new InvalidOperationException(
            "Runtime Env not set.  Perhaps because of using default(Runtime) or new Runtime() rather than Runtime.New()");

    /// <summary>
    /// Constructor function
    /// </summary>
    public static WaveeRuntime New(Option<ILogger> logger,
        Option<AudioOutputIO> audioOutput,
        Option<DatabaseIO> db) =>
        new WaveeRuntime(new RuntimeEnv(new CancellationTokenSource(),
            logger.IfNone(NullLogger.Instance), audioOutput, db));

    /// <summary>
    /// Create a new Runtime with a fresh cancellation token
    /// </summary>
    /// <remarks>Used by localCancel to create new cancellation context for its sub-environment</remarks>
    /// <returns>New runtime</returns>
    public WaveeRuntime LocalCancel =>
        new WaveeRuntime(new RuntimeEnv(new CancellationTokenSource(), Env.Logger, Env.AudioOutputIo, Env.Database));

    /// <summary>
    /// Direct access to cancellation token
    /// </summary>
    public CancellationToken CancellationToken =>
        Env.Token;

    /// <summary>
    /// Directly access the cancellation token source
    /// </summary>
    /// <returns>CancellationTokenSource</returns>
    public CancellationTokenSource CancellationTokenSource =>
        Env.Source;


    public Eff<WaveeRuntime, Traits.LogIO> LogEff => Eff<WaveeRuntime, Traits.LogIO>(static
        rt => new LogIO(rt.env.Logger));

    public Eff<WaveeRuntime, TcpIO> TcpEff =>
        SuccessEff(Live.TcpIOImpl.Default);

    public Eff<WaveeRuntime, HttpIO> HttpEff =>
        SuccessEff(Live.HttpIOImpl.Default);


    public Eff<WaveeRuntime, Option<AudioOutputIO>> AudioOutputEff
        => Eff<WaveeRuntime, Option<AudioOutputIO>>(static rt => rt.Env.AudioOutputIo);

    public Eff<WaveeRuntime, WebsocketIO> WsEff =>
        SuccessEff(Live.WebsocketIOImpl.Default);

    public Aff<WaveeRuntime, DatabaseIO> Database =>
        Eff<WaveeRuntime, Option<DatabaseIO>>((rt) => rt.Env.Database)
            .Map(x => x.IfNone(EmptyDbIO.Default));

    internal class RuntimeEnv
    {
        public readonly CancellationTokenSource Source;
        public readonly CancellationToken Token;
        public Option<AudioOutputIO> AudioOutputIo;
        public ILogger Logger;
        public Option<DatabaseIO> Database;

        public RuntimeEnv(CancellationTokenSource source, CancellationToken token,
            ILogger logger, Option<AudioOutputIO> audioOutputIo, Option<DatabaseIO> database)
        {
            Source = source;
            Token = token;
            Logger = logger;
            AudioOutputIo = audioOutputIo;
            Database = database;
        }

        public RuntimeEnv(CancellationTokenSource source, ILogger logger, Option<AudioOutputIO> audioOutputIo,
            Option<DatabaseIO> database) :
            this(source,
                source.Token, logger, audioOutputIo, database)
        {
        }
    }

    private readonly struct EmptyDbIO : DatabaseIO
    {
        public static EmptyDbIO Default = new EmptyDbIO();
        public Aff<TKey> Insert<T, TKey>(T entity, CancellationToken token = new CancellationToken())
            where T : class, IEntity<TKey>
        {
            return SuccessAff<TKey>(default);
        }

        public Aff<TKey> Insert<T, TKey>(Func<IValueInsertable<T>, IValueInsertable<T>> provider,
            CancellationToken token = new CancellationToken()) where T : class, IEntity<TKey>
        {
            return SuccessAff<TKey>(default);
        }

        public Aff<Guid> InsertGuid<T>(T entity, CancellationToken token = new CancellationToken())
            where T : class, IEntity<Guid>
        {
            return SuccessAff<Guid>(default);
        }

        public Aff<Guid> InsertGuid<T>(Func<IValueInsertable<T>, IValueInsertable<T>> provider,
            CancellationToken token = new CancellationToken()) where T : class, IEntity<Guid>
        {
            return SuccessAff<Guid>(default);
        }

        public Aff<Unit> Update<T>(T entity, CancellationToken token = new CancellationToken()) where T : class
        {
            return SuccessAff<Unit>(unit);
        }

        public Aff<Unit> Update<T>(Func<ITable<T>, IUpdatable<T>> updater,
            CancellationToken token = new CancellationToken()) where T : class
        {
            return SuccessAff<Unit>(unit);
        }

        public Aff<Unit> Delete<T>(Expression<Func<T, bool>> filter, CancellationToken token = new CancellationToken())
            where T : class
        {
            return SuccessAff<Unit>(unit);
        }

        public Aff<Option<T>> FindOne<T>(Expression<Func<T, bool>> filter,
            CancellationToken token = new CancellationToken()) where T : class
        {
            return SuccessAff<Option<T>>(None);
        }

        public Aff<Arr<T>> Find<T>(Expression<Func<T, bool>> filter, CancellationToken token = new CancellationToken())
            where T : class
        {
            return SuccessAff<Arr<T>>(Empty);
        }

        public Aff<int> Count<T>(Func<ITable<T>, IQueryable<T>> query,
            CancellationToken token = new CancellationToken()) where T : class
        {
            return SuccessAff<int>(0);
        }

        public Aff<DataAndCount<T>> FindAndCount<T>(IQueryable<T> query, DataLimit limit,
            CancellationToken token = new CancellationToken()) where T : class
        {
            return SuccessAff<DataAndCount<T>>(new DataAndCount<T>(Empty, 0));
        }

        public Eff<ITable<T>> Table<T>() where T : class
        {
            return SuccessEff<ITable<T>>(default);
        }

        public Eff<IQueryable<A>> GetCte<T, A>(Func<IQueryable<T>, IQueryable<A>> body, Option<string> name)
            where T : class
        {
            return SuccessEff<IQueryable<A>>(new EnumerableQuery<A>(Enumerable.Empty<A>()));
        }

        public Eff<IQueryable<T>> GetRecursiveCte<T>(Func<IQueryable<T>, IQueryable<T>> body, Option<string> name)
            where T : class
        {
            return SuccessEff<IQueryable<T>>(new EnumerableQuery<T>(Enumerable.Empty<T>()));
        }
    }
}