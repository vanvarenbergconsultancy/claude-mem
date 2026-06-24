using System;
using System.Data;
using Dapper;

namespace ClaudeMem.Admin.Api.Infrastructure.Database;

/// <summary>Dapper type handler that maps nullable database values to a nullable <see cref="DateTimeOffset"/>, treating <c>NULL</c> and <see cref="DBNull"/> as <c>null</c>.</summary>
internal sealed class NullableDateTimeOffsetTypeHandler : SqlMapper.TypeHandler<DateTimeOffset?>
{
    public override DateTimeOffset? Parse(object value)
    {
        if (value is null or DBNull)
        {
            return null;
        }

        return value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(dt, TimeSpan.Zero),
            _ => throw new InvalidCastException($"Cannot convert {value.GetType()} to DateTimeOffset.")
        };
    }

    public override void SetValue(IDbDataParameter parameter, DateTimeOffset? value)
    {
        parameter.Value = value.HasValue ? (object)value.Value : DBNull.Value;
    }
}