using CompanyName.ServiceName.ConsoleClient;
using CompanyName.ServiceName.V1;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

var apiUrl = "http://localhost:5001";
using var channel = GrpcChannelBuilder.Build(apiUrl, isSecure: false, accessToken: null);
var client = new ServiceName.ServiceNameClient(channel);

var now = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow);

Console.WriteLine("Add 1:");
var add1Request = new SaveEntityNameRequest
{
  Item = new EntityName { Title = $"Add 1 title {DateTimeOffset.UtcNow.ToString("u")}" }
};
var add1Response = await client.SaveEntityNameAsync(add1Request);
Console.WriteLine(add1Response);

Console.WriteLine("Add 2:");
var add2Request = new SaveEntityNameRequest
{
  Item = new EntityName { Title = $"Add 2 title {DateTimeOffset.UtcNow.ToString("u")}" }
};
var add2Response = await client.SaveEntityNameAsync(add2Request);
Console.WriteLine(add2Response);

Console.WriteLine("Update 2:");
var updateItem = new EntityName(add2Response.Item);
updateItem.CreatedAt = Timestamp.FromDateTimeOffset(DateTimeOffset.MinValue);
updateItem.Title += ". Updated.";
var updateRequest = new SaveEntityNameRequest { Item = updateItem };
var updateResponse = await client.SaveEntityNameAsync(updateRequest);
Console.WriteLine(updateResponse);

Console.WriteLine("Get updated 2:");
var getRequest = new GetEntityNameRequest { Id = updateRequest.Item.Id };
var getResponse = await client.GetEntityNameAsync(getRequest);
Console.WriteLine(getResponse);

Console.WriteLine("Amount:");
var amountRequest = new GetEntityNameAmountRequest();
var amountResponse = await client.GetEntityNameAmountAsync(amountRequest);
Console.WriteLine(amountResponse);

Console.WriteLine("Get list without params:");
var getListWithoutParamsRequest = new GetEntityNameListStreamRequest();
var getListWithoutParamsListCall = client.GetEntityNameListStream(getListWithoutParamsRequest);
await foreach (var res in getListWithoutParamsListCall.ResponseStream.ReadAllAsync())
{
  Console.WriteLine(res);
}

Console.WriteLine("Get first 5 add updated list:");
var getFirst5UpdatedListRequest = new GetEntityNameListStreamRequest
{
  Filter = new EntityNameFilter { Q = "Add 1 Updated" },
  Cursor = new EntityNameCursor { Limit = 5 }
};
var getFirst5UpdatedListCall = client.GetEntityNameListStream(getFirst5UpdatedListRequest);
await foreach (var res in getFirst5UpdatedListCall.ResponseStream.ReadAllAsync())
{
  Console.WriteLine(res);
}

Console.WriteLine("Get first 5 add 2 updated with rank list:");
var getFirst5UpdatedWithRankListRequest = new GetEntityNameListStreamRequest
{
  Filter = new EntityNameFilter { Q = "Add 2 Updated" },
  Cursor = new EntityNameCursor
  {
    Limit = 5,
    Before = new EntityNameCursor.Types.Sorting
    {
      Rank = new EntityNameCursor.Types.UniqueStringKey
      {
        Value = "Add 2 Updated",
        Id = 1
      }
    }
  }
};
var getFirst5UpdatedWithRankListCall = client.GetEntityNameListStream(getFirst5UpdatedWithRankListRequest);
await foreach (var res in getFirst5UpdatedWithRankListCall.ResponseStream.ReadAllAsync())
{
  Console.WriteLine(res);
}

Console.WriteLine("Get first 5 after (>) 0 id list:");
var getFirst5UpdatedAfter0IdListRequest = new GetEntityNameListStreamRequest
{
  Cursor = new EntityNameCursor { Limit = 5, After = new EntityNameCursor.Types.Sorting { Id = 0 } }
};
var getFirst5UpdatedAfterIdListCall = client.GetEntityNameListStream(getFirst5UpdatedAfter0IdListRequest);
await foreach (var res in getFirst5UpdatedAfterIdListCall.ResponseStream.ReadAllAsync())
{
  Console.WriteLine(res);
}

Console.WriteLine("Get first 5 before (<) 0 id list:");
var getFirst5UpdatedBeforeNowListRequest = new GetEntityNameListStreamRequest
{
  Cursor = new EntityNameCursor { Limit = 5, Before = new EntityNameCursor.Types.Sorting { Id = 0 } }
};
var getFirst5UpdatedBeforeNowListCall = client.GetEntityNameListStream(getFirst5UpdatedBeforeNowListRequest);
await foreach (var res in getFirst5UpdatedBeforeNowListCall.ResponseStream.ReadAllAsync())
{
  Console.WriteLine(res);
}

Console.WriteLine("Get first 5 after (>) now CreatedAt list:");
var getFirst5UpdatedAfterNowCreatedAtListRequest = new GetEntityNameListStreamRequest
{
  Cursor = new EntityNameCursor
  {
    Limit = 5,
    After = new EntityNameCursor.Types.Sorting
    {
      CreatedAt = new EntityNameCursor.Types.UniqueTimestampKey { Value = now, Id = 0 }
    }
  }
};
var getFirst5UpdatedAfterNowCreatedAtListCall =
  client.GetEntityNameListStream(getFirst5UpdatedAfterNowCreatedAtListRequest);
await foreach (var res in getFirst5UpdatedAfterNowCreatedAtListCall.ResponseStream.ReadAllAsync())
{
  Console.WriteLine(res);
}

Console.WriteLine("Get first 5 before (<) now CreatedAt list:");
var getFirst5UpdatedBeforeNowCreatedAtListRequest = new GetEntityNameListStreamRequest
{
  Cursor = new EntityNameCursor
  {
    Limit = 5,
    After = new EntityNameCursor.Types.Sorting
    {
      CreatedAt = new EntityNameCursor.Types.UniqueTimestampKey { Value = now, Id = 0 }
    }
  }
};
var getFirst5UpdatedBeforeNowCreatedAtListCall =
  client.GetEntityNameListStream(getFirst5UpdatedBeforeNowCreatedAtListRequest);
await foreach (var res in getFirst5UpdatedBeforeNowCreatedAtListCall.ResponseStream.ReadAllAsync())
{
  Console.WriteLine(res);
}

Console.WriteLine("Add 3:");
var add3Request = new SaveEntityNameRequest
{
  Item = new EntityName { Title = $"Add 3 title {DateTime.UtcNow.ToString("u")}" }
};
var add3Response = await client.SaveEntityNameAsync(add3Request);
Console.WriteLine(add3Response);

Console.WriteLine("Delete 3:");
var delete3Request = new DeleteEntityNameRequest { Id = add3Response.Item.Id };
var delete3Response = await client.DeleteEntityNameAsync(delete3Request);
Console.WriteLine(delete3Response);

try
{
  Console.WriteLine("Delete not exist item:");
  var deleteNotExistRequest = new DeleteEntityNameRequest { Id = int.MaxValue };
  await client.DeleteEntityNameAsync(deleteNotExistRequest);
}
catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
{
  Console.WriteLine(ex.Message);
}
