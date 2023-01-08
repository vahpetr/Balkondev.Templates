using Grpc.Core;

namespace CompanyName.ServiceName.Shared;

public static class ServiceErrors
{
  public static readonly Status NotFoundError = new(StatusCode.NotFound, "Объект не найден");

  public static readonly Status FilterError =
    new(StatusCode.InvalidArgument, "Ошибка параметров фильтрации");

  public static readonly Status SaveError = new(StatusCode.InvalidArgument, "Ошибка при сохранении объекта");

  public static readonly Status AddError =
    new(StatusCode.InvalidArgument, "Ошибка при добавлении объекта");

  public static readonly Status UpdateError =
    new(StatusCode.InvalidArgument, "Ошибка при обновлении объекта");

  public static readonly Status DeleteError =
    new(StatusCode.InvalidArgument, "Ошибка при удалении");

  public static readonly Status DbError = new(StatusCode.Aborted, "Ошибка при работе с БД");

  public static readonly Status DbConcurrencyError =
    new(StatusCode.Aborted, "Ошибка согласованности данных при работе с БД");
}
