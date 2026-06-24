using System;
using System.Data;
using Dapper;

namespace ClaudeMem.Admin.Api.Infrastructure.Database;

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
