using CompanyName.ServiceName.Ef.Data.ServiceName;
using CompanyName.ServiceName.Shared;
using CompanyName.ServiceName.V1;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CompanyName.ServiceName.Ef.Postgres.Services.ServiceName;

// https://github.com/grpc-ecosystem/grpc-gateway/blob/main/runtime/errors.go#L34

public class ServiceName : V1.ServiceName.ServiceNameBase
{
  private readonly Lazy<IServiceNameDbContext> _serviceNameDbContext;

  private readonly Lazy<IServiceNameContext> _serviceNameContext;

  // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/loggermessage?view=aspnetcore-7.0
  // open telemetry
  private readonly ILogger<ServiceName> _logger;

  public ServiceName(Lazy<IServiceNameDbContext> serviceNameDbContext, Lazy<IServiceNameContext> serviceNameContext,
    ILogger<ServiceName> logger)
  {
    _serviceNameDbContext = serviceNameDbContext;
    _serviceNameContext = serviceNameContext;
    _logger = logger;
  }

  // ReSharper disable once MemberCanBePrivate.Global
  protected void AssertSaveEntityName(EntityName item)
  {
    if (string.IsNullOrWhiteSpace(item.Title) || item.Title.Length > 256)
    {
      var trailers = new Metadata
      {
        { "stage", "assert" },
        { "entity", EntityName.Descriptor.FullName },
        { "property", "title" },
        { "rule", "{ \"required\": true, \"maxLength\": 256 }" },
      };
      throw new RpcException(ServiceErrors.SaveError, trailers);
    }

    if (item.Description?.Length > 4096)
    {
      var trailers = new Metadata
      {
        { "stage", "assert" },
        { "entity", EntityName.Descriptor.FullName },
        { "property", "description" },
        { "rule", "{ \"maxLength\": 4096 }" },
      };
      throw new RpcException(ServiceErrors.SaveError, trailers);
    }
  }

  // ReSharper disable once MemberCanBePrivate.Global
  protected void AssertAddEntityName(EntityName item)
  {
    if (item.Id != 0)
    {
      var trailers = new Metadata
      {
        { "stage", "assert" },
        { "entity", EntityName.Descriptor.FullName },
        { "property", "id" },
        { "rule", $"{{ \"notEquals\": 0 }}" },
      };
      throw new RpcException(ServiceErrors.AddError, trailers);
    }
  }

  // ReSharper disable once MemberCanBePrivate.Global
  protected void AssertUpdateEntityName(EntityName dbItem, EntityName newItem)
  {
    if (dbItem.ChangedAt != newItem.ChangedAt)
    {
      var trailers = new Metadata
      {
        { "stage", "assert" },
        { "entity", EntityName.Descriptor.FullName },
        { "property", "changedAt" },
        { "rule", $"{{ \"equals\": \"{dbItem.ChangedAt.ToDateTimeOffset().ToString("O")}\" }}" },
      };
      throw new RpcException(ServiceErrors.UpdateError, trailers);
    }
  }

  // ReSharper disable once MemberCanBePrivate.Global
  protected void AssertDeleteEntityName(int id)
  {
    if (id <= 0)
    {
      var trailers = new Metadata
      {
        { "stage", "assert" },
        { "entity", EntityName.Descriptor.FullName },
        { "property", "id" },
        { "rule", $"{{ \"moreOrEqual\": 1 }}" },
      };
      throw new RpcException(ServiceErrors.DeleteError, trailers);
    }
  }

  // ReSharper disable once MemberCanBePrivate.Global
  protected void AssertEntityNameFilter(EntityNameFilter filter)
  {
    var q = filter.Q?.Trim();
    if (!string.IsNullOrEmpty(q) && (q.Length < 3 || q.Length > 64))
    {
      throw new RpcException(ServiceErrors.FilterError,
        new Metadata
        {
          { "stage", "assert" },
          { "entity", EntityNameFilter.Descriptor.FullName },
          { "property", "q" },
          { "rule", "{ \"minLength\": 3, \"maxLength\": 64 }" },
        }
      );
    }
  }

  protected void AddEntityName(EntityName item, CancellationToken cancellationToken = default)
  {
    AssertAddEntityName(item);

    var now = Timestamp.FromDateTimeOffset(_serviceNameContext.Value.Now);
    item.CreatedAt = now;
    item.ChangedAt = now;

    // add stage validation (business logic) here

    _serviceNameDbContext.Value.EntityNames.Add(item);
  }

  protected void UpdateEntityName(EntityName dbItem, EntityName newItem)
  {
    AssertUpdateEntityName(dbItem, newItem);

    dbItem.Title = newItem.Title;
    dbItem.Description = newItem.Description;
    dbItem.ChangedAt = Timestamp.FromDateTimeOffset(_serviceNameContext.Value.Now);

    // add stage validation (business logic) here

    _serviceNameDbContext.Value.EntityNames.Update(dbItem);
  }

  public override async Task<DeleteEntityNameResponse> DeleteEntityName(DeleteEntityNameRequest request,
    ServerCallContext context)
  {
    AssertDeleteEntityName(request.Id);

    var item = new EntityName { Id = request.Id };
    _serviceNameDbContext.Value.EntityNames.Remove(item);

    var trailers = new Metadata
    {
      { "stage", "delete" }, { "entity", EntityName.Descriptor.FullName }, { "id", request.Id.ToString() }
    };
    await SaveChangesAsync(request.Id, trailers, context.CancellationToken).ConfigureAwait(false);

    return new DeleteEntityNameResponse();
  }

  public override async Task<GetEntityNameResponse> GetEntityName(GetEntityNameRequest request,
    ServerCallContext context)
  {
    int id = request.Id;
    var item = await GetEntityNameQuery(_serviceNameDbContext.Value.EntityNames)
      .FirstOrDefaultAsync(p => p.Id == id, context.CancellationToken)
      .ConfigureAwait(false);
    if (item == null)
    {
      throw new RpcException(ServiceErrors.NotFoundError,
        new Metadata
        {
          { "stage", "get" }, { "entity", EntityName.Descriptor.FullName }, { "id", request.Id.ToString() }
        }
      );
    }

    return new GetEntityNameResponse { Item = item };
  }

  public override async Task<GetEntityNameAmountResponse> GetEntityNameAmount(GetEntityNameAmountRequest request,
    ServerCallContext context)
  {
    var query = ApplyEntityNameFilter(_serviceNameDbContext.Value.EntityNames, request.Filter);
    var total = await query.CountAsync(context.CancellationToken).ConfigureAwait(false);
    return new GetEntityNameAmountResponse { Total = total };
  }

  public override async Task GetEntityNameListStream(GetEntityNameListStreamRequest request,
    IServerStreamWriter<GetEntityNameListStreamResponse> responseStream, ServerCallContext context)
  {
    var query = GetEntityNameQuery(_serviceNameDbContext.Value.EntityNames);
    query = ApplyEntityNameFilter(query, request.Filter);
    query = ApplyEntityNameCursor(query, request.Cursor);
    var items = query.AsAsyncEnumerable();
    await foreach (var item in items.WithCancellation(context.CancellationToken).ConfigureAwait(false))
    {
      var response = new GetEntityNameListStreamResponse { Item = item };
      await responseStream.WriteAsync(response, context.CancellationToken).ConfigureAwait(false);
    }
  }

  public override async Task<SaveEntityNameResponse> SaveEntityName(SaveEntityNameRequest request,
    ServerCallContext context)
  {
    AssertSaveEntityName(request.Item);

    var actionType = SaveEntityNameResponse.Types.ActionType.Added;

    if (request.Item.Id == 0)
    {
      AddEntityName(request.Item);
    }
    else
    {
      try
      {
        var getEntityNameRequest = new GetEntityNameRequest { Id = request.Item.Id };
        var getEntityNameResponse = await GetEntityName(getEntityNameRequest, context).ConfigureAwait(false);
        UpdateEntityName(getEntityNameResponse.Item, request.Item);
        actionType = SaveEntityNameResponse.Types.ActionType.Updated;
      }
      catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
      {
        AddEntityName(request.Item);
      }
    }

    var id = request.Item.Id;
    var trailers = new Metadata
    {
      { "stage", "save" },
      { "entity", EntityName.Descriptor.FullName },
      { "id", id.ToString() },
      { "action", actionType.ToString("G") }
    };
    await SaveChangesAsync(id, trailers, context.CancellationToken).ConfigureAwait(false);

    return new SaveEntityNameResponse { Item = request.Item, ActionType = actionType };
  }

  // ReSharper disable once MemberCanBePrivate.Global
  protected Task<bool> ExistEntityNameAsync(int id, CancellationToken cancellationToken = default)
  {
    return _serviceNameDbContext.Value.EntityNames.AsNoTracking()
      .AnyAsync(p => p.Id == id, cancellationToken);
  }

  // ReSharper disable once MemberCanBePrivate.Global
  protected IQueryable<EntityName> ApplyEntityNameFilter(IQueryable<EntityName> query, EntityNameFilter? filter)
  {
    if (filter == null)
    {
      return query;
    }

    AssertEntityNameFilter(filter);

    var q = filter.Q?.Trim();
    if (!string.IsNullOrEmpty(q))
    {
      query = query.Where(p => EF.Functions.ToTsVector("english", p.Title + " " + p.Description + " " + p.Content)
        .Matches(EF.Functions.WebSearchToTsQuery(q))
      );
    }

    if (filter.AfterCreatedAt != null)
    {
      var afterCreatedAt = filter.AfterCreatedAt;
      query = query.Where(p => p.CreatedAt >= afterCreatedAt);
    }

    if (filter.BeforeCreatedAt != null)
    {
      var beforeCreatedAt = filter.BeforeCreatedAt;
      query = query.Where(p => p.CreatedAt < beforeCreatedAt);
    }

    if (filter.AfterChangedAt != null)
    {
      var afterChangedAt = filter.AfterChangedAt;
      query = query.Where(p => p.ChangedAt >= afterChangedAt);
    }

    if (filter.BeforeChangedAt != null)
    {
      var beforeChangedAt = filter.BeforeChangedAt;
      query = query.Where(p => p.ChangedAt < beforeChangedAt);
    }

    return query;
  }

  // ReSharper disable once MemberCanBePrivate.Global
  protected IQueryable<EntityName> ApplyEntityNameCursor(IQueryable<EntityName> query, EntityNameCursor? cursor)
  {
    if (cursor == null)
    {
      query = query.OrderByDescending(p => p.Id);
    }
    else
    {
      switch (cursor.DirectionCase)
      {
        case EntityNameCursor.DirectionOneofCase.After:
          var after = cursor.After;
          switch (after.ByCase)
          {
            case EntityNameCursor.Types.Sorting.ByOneofCase.Rank:
              var rank = after.Rank.Trim();
              query = query.OrderBy(p => EF.Functions
                .ToTsVector("english", p.Title + " " + p.Description + " " + p.Content)
                .RankCoverDensity(EF.Functions.WebSearchToTsQuery(rank),
                  NpgsqlTsRankingNormalization.DivideByMeanHarmonicDistanceBetweenExtents)
              ).ThenByDescending(p => p.Id);
              break;
            case EntityNameCursor.Types.Sorting.ByOneofCase.ChangedAt:
              query = query.OrderBy(p => p.ChangedAt).ThenByDescending(p => p.Id).Where(p =>
                p.ChangedAt > after.ChangedAt.Time || (p.ChangedAt == after.ChangedAt.Time && p.Id < after.ChangedAt.Id)
              );
              break;
            case EntityNameCursor.Types.Sorting.ByOneofCase.CreatedAt:
              query = query.OrderBy(p => p.CreatedAt).ThenByDescending(p => p.Id).Where(p =>
                p.CreatedAt > after.CreatedAt.Time || (p.CreatedAt == after.CreatedAt.Time && p.Id < after.CreatedAt.Id)
              );
              break;
            case EntityNameCursor.Types.Sorting.ByOneofCase.None:
            default:
              query = query.OrderBy(p => p.Id).Where(p => p.Id > after.Id);
              break;
          }

          break;
        case EntityNameCursor.DirectionOneofCase.Before:
          var before = cursor.Before;
          switch (before.ByCase)
          {
            case EntityNameCursor.Types.Sorting.ByOneofCase.Rank:
              var rank = before.Rank.Trim();
              query = query.OrderByDescending(p => EF.Functions
                .ToTsVector("english", p.Title + " " + p.Description + " " + p.Content)
                .RankCoverDensity(EF.Functions.WebSearchToTsQuery(rank),
                  NpgsqlTsRankingNormalization.DivideByMeanHarmonicDistanceBetweenExtents)
              ).ThenByDescending(p => p.Id);
              break;
            case EntityNameCursor.Types.Sorting.ByOneofCase.ChangedAt:
              query = query.OrderByDescending(p => p.ChangedAt).ThenByDescending(p => p.Id).Where(p =>
                p.ChangedAt < before.ChangedAt.Time ||
                (p.ChangedAt == before.ChangedAt.Time && p.Id < before.ChangedAt.Id)
              );
              break;
            case EntityNameCursor.Types.Sorting.ByOneofCase.CreatedAt:
              query = query.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id).Where(p =>
                p.CreatedAt < before.CreatedAt.Time ||
                (p.CreatedAt == before.CreatedAt.Time && p.Id < before.CreatedAt.Id)
              );
              break;
            case EntityNameCursor.Types.Sorting.ByOneofCase.None:
            case EntityNameCursor.Types.Sorting.ByOneofCase.Id:
            default:
              query = query.OrderByDescending(p => p.Id).Where(p => p.Id < cursor.Before.Id);
              break;
          }

          break;
        case EntityNameCursor.DirectionOneofCase.None:
        default:
          query = query.OrderByDescending(p => p.Id);
          // no order
          break;
      }
    }

    var limit = cursor?.Limit switch
    {
      null => 20,
      < 1 => 1,
      > 1000 => 1000,
      _ => cursor.Limit
    };
    return query.Take(limit);
  }

  // ReSharper disable once MemberCanBePrivate.Global
  protected IQueryable<EntityName> GetEntityNameQuery(IQueryable<EntityName> query)
  {
    return query.AsNoTracking();
  }

  // ReSharper disable once MemberCanBePrivate.Global
  protected async Task<int> SaveChangesAsync(int? id = null, Metadata? trailers = default,
    CancellationToken cancellationToken = default)
  {
    try
    {
      return await ((DbContext)_serviceNameDbContext.Value).SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
    catch (DbUpdateConcurrencyException)
    {
      if (id.HasValue && !(await ExistEntityNameAsync(id.Value, cancellationToken).ConfigureAwait(false)))
      {
        throw new RpcException(ServiceErrors.NotFoundError, trailers ?? new Metadata());
      }

      // https://learn.microsoft.com/en-us/ef/ef6/saving/concurrency
      throw new RpcException(ServiceErrors.DbConcurrencyError, trailers ?? new Metadata());
    }
    catch (DbUpdateException)
    {
      if (id.HasValue && !(await ExistEntityNameAsync(id.Value, cancellationToken).ConfigureAwait(false)))
      {
        throw new RpcException(ServiceErrors.NotFoundError, trailers ?? new Metadata());
      }

      throw new RpcException(ServiceErrors.DbError, trailers ?? new Metadata());
    }
  }
}
