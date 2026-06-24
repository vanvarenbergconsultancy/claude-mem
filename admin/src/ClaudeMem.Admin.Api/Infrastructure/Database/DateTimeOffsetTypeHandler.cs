using System;
using System.Data;
using Dapper;

namespace ClaudeMem.Admin.Api.Infrastructure.Database;

/// <summary>Dapper type handler that maps database values to <see cref="DateTimeOffset"/>, including plain <see cref="DateTime"/> values from SQLite.</summary>
internal sealed class DateTimeOffsetTypeHandler : SqlMapper.TypeHandler<DateTimeOffset>
{
    public override DateTimeOffset Parse(object value)
    {
        return value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(dt, TimeSpan.Zero),
            _ => throw new InvalidCastException($"Cannot convert {value.GetType()} to DateTimeOffset.")
        };
    }

    public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
    {
        parameter.Value = value;
    }
}
