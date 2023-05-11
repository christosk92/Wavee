using System.Linq.Expressions;
using LanguageExt;
using LanguageExt.Effects.Database;
using LanguageExt.Effects.Traits;
using LinqToDB;
using LinqToDB.Linq;
using static LanguageExt.Prelude;

namespace Wavee.Spotify.Cache;

public readonly struct EmptyDb : DatabaseIO
{
    public Aff<TKey> Insert<T, TKey>(T entity, CancellationToken token = new CancellationToken())
        where T : class, IEntity<TKey>
    {
        return SuccessAff(default(TKey));
    }

    public Aff<TKey> Insert<T, TKey>(Func<IValueInsertable<T>, IValueInsertable<T>> provider,
        CancellationToken token = new CancellationToken()) where T : class, IEntity<TKey>
    {
        return SuccessAff(default(TKey));
    }

    public Aff<Guid> InsertGuid<T>(T entity, CancellationToken token = new CancellationToken())
        where T : class, IEntity<Guid>
    {
        return SuccessAff(Guid.Empty);
    }

    public Aff<Guid> InsertGuid<T>(Func<IValueInsertable<T>, IValueInsertable<T>> provider,
        CancellationToken token = new CancellationToken()) where T : class, IEntity<Guid>
    {
        return SuccessAff(Guid.Empty);
    }

    public Aff<Unit> Update<T>(T entity, CancellationToken token = new CancellationToken()) where T : class
    {
        return SuccessAff(unit);
    }

    public Aff<Unit> Update<T>(Func<ITable<T>, IUpdatable<T>> updater,
        CancellationToken token = new CancellationToken()) where T : class
    {
        return SuccessAff(unit);
    }

    public Aff<Unit> Delete<T>(Expression<Func<T, bool>> filter, CancellationToken token = new CancellationToken())
        where T : class
    {
        return SuccessAff(unit);
    }

    public Aff<Option<T>> FindOne<T>(Expression<Func<T, bool>> filter,
        CancellationToken token = new CancellationToken()) where T : class
    {
        return SuccessAff(Option<T>.None);
    }

    public Aff<Arr<T>> Find<T>(Expression<Func<T, bool>> filter, CancellationToken token = new CancellationToken())
        where T : class
    {
        return SuccessAff(Arr<T>.Empty);
    }

    public Aff<int> Count<T>(Func<ITable<T>, IQueryable<T>> query, CancellationToken token = new CancellationToken())
        where T : class
    {
        return SuccessAff(0);
    }

    public Aff<DataAndCount<T>> FindAndCount<T>(IQueryable<T> query, DataLimit limit,
        CancellationToken token = new CancellationToken()) where T : class
    {
        return SuccessAff(new DataAndCount<T>(Arr<T>.Empty, 0));
    }

    public Eff<ITable<T>> Table<T>() where T : class
    {
        return SuccessEff(default(ITable<T>));
    }

    public Eff<IQueryable<A>> GetCte<T, A>(Func<IQueryable<T>, IQueryable<A>> body, Option<string> name) where T : class
    {
       return SuccessEff(default(IQueryable<A>));
    }

    public Eff<IQueryable<T>> GetRecursiveCte<T>(Func<IQueryable<T>, IQueryable<T>> body, Option<string> name)
        where T : class
    {
       return SuccessEff(default(IQueryable<T>));
    }
}