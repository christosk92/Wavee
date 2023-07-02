using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Spotify.Metadata;
using System;
using Wavee.Sqlite.Entities;
using Wavee.Sqlite.Mapping;

namespace Wavee.Sqlite.Repository;

public sealed class RawEntityRepository
{
    private readonly string _cachePath;

    public RawEntityRepository(string cachePath)
    {
        _cachePath = cachePath;
    }

    public async Task<Nullable<ReadOnlyMemory<byte>>> GetEntity(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_cachePath))
        {
            return null;
        }
        await using var db = LocalDbFactory.Create(_cachePath);
        var entity = await db.RawEntities.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (entity is not null && entity.Expiration < DateTimeOffset.UtcNow)
        {
            db.RawEntities.Remove(entity);
            await db.SaveChangesAsync(ct);
            return null;
        }
        var res = entity?.Data;
        if (res is not null)
        {
            return res;
        }

        return null;
    }

    public async Task SetEntity(string id, byte[] data, DateTimeOffset expiration, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_cachePath))
        {
            return;
        }

        await using var db = LocalDbFactory.Create(_cachePath);
        var entity = await db.RawEntities.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            entity = new RawEntity
            {
                Id = id,
                Data = data,
                Type = 0,
                Expiration = expiration
            };
            await db.RawEntities.AddAsync(entity, ct);
        }
        else
        {
            entity.Data = data;
        }

        await db.SaveChangesAsync(ct);
    }
}