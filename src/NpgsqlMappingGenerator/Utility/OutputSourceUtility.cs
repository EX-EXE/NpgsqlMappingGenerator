using System;
using System.Collections.Generic;
using System.Text;
using CodeAnalyzeUtility;

namespace NpgsqlMappingGenerator.Utility
{
    internal static class OutputSourceUtility
    {
        public static string CreateDbTableProperty(AnalyzeDbTable dbTable)
            => $$"""
    public static readonly string DbTableName = "{{dbTable.DbTableName}}";
""";
        public static string CreateColumnProperty(AnalyzeDbColumn[] dbColumns)
        {
            return dbColumns.ForEachIndexLines((i, x) => $$"""public {{x.PropertyType}} {{x.PropertyName}} { get; set; } {{(string.IsNullOrEmpty(x.PropertyDefaultValue) ? string.Empty : $" = {x.PropertyDefaultValue};")}}""").OutputLine(1);
        }

        public static string CreateDbType(AnalyzeDbColumn[] dbColumns, AnalyzeDbColumn[] dbQueries, (string Key, string Value)[] AppendEnumValues)
        {
            return $$"""
    [Flags]
    public enum DbQueryType
    {
{{dbQueries.ForEachIndexLines((i, x) => $"{x.PropertyName} = 1 << {i},").OutputLine(2)}}
        None = 0,
        All = {{(0 < dbQueries.Length ? dbQueries.ForEachLines(x => x.PropertyName).OutputLine("|") : "0")}},
        AllColumns = {{(0 < dbColumns.Length ? dbColumns.ForEachLines(x => x.PropertyName).OutputLine("|") : "0")}},
{{AppendEnumValues.ForEachLines(x => $"{x.Key} = {x.Value},").OutputLine(2)}}
    }
    [Flags]
    public enum DbColumnType
    {
{{dbColumns.ForEachIndexLines((i, x) => $"{x.PropertyName} = DbQueryType.{x.PropertyName},").OutputLine(2)}}
        None = 0,
        All = {{(0 < dbColumns.Length ? dbColumns.ForEachLines(x => x.PropertyName).OutputLine("|") : "0")}},
{{AppendEnumValues.ForEachLines(x => $"{x.Key} = {x.Value},").OutputLine(2)}}
    }
    public static readonly DbQueryType[] DbQueryTypes = {
{{dbQueries.ForEachLines(x => $"DbQueryType.{x.PropertyName},").OutputLine(3)}}
        };
    public static string GetDbQuery(DbQueryType queryType)
        => queryType switch
        {
{{dbQueries.ForEachLines(x => $"DbQueryType.{x.PropertyName} => \"{x.DbQuery}\",").OutputLine(3)}}
            _ => throw new NotImplementedException(),
        };
""";
        }

        public static string CreateDbParam(AnalyzeDbColumn[] dbColumns)
        {
            var dbParams = new StringBuilder();
            dbParams.AppendLine($$"""
    public interface IDbParam
    {
        DbQueryType QueryType { get; }
        string DbTable { get; }
        string DbQuery { get; }
        NpgsqlParameter CreateParameter(string paramName);
    }
""");
            foreach (var dbColumn in dbColumns)
            {
                var defaultValue = dbColumn.PropertyType == "string" || dbColumn.PropertyType == "System.String"
                    ? "string.Empty"
                    : "default";
                dbParams.AppendLine($$"""
    public class DbParam{{dbColumn.PropertyName}} : IDbParam
    {
        public DbQueryType QueryType => DbQueryType.{{dbColumn.PropertyName}};
        public string DbTable => DbTableName;
        public string DbQuery => GetDbQuery(QueryType);
        public {{dbColumn.PropertyType}} Value { get; private set; } = {{defaultValue}};

        public static DbCondition CreateCondition(DbCompareOperator compareOperator ,{{dbColumn.PropertyType}} value)
        {
            return new DbCondition(compareOperator,new DbParam{{dbColumn.PropertyName}}(value));
        }
        public DbParam{{dbColumn.PropertyName}}()
        {
        }
        public DbParam{{dbColumn.PropertyName}}({{dbColumn.PropertyType}} value)
        {
            Value = value;
        }
        public NpgsqlParameter CreateParameter(string name)
        {
            return {{dbColumn.ConverterType}}.CreateParameter(name, Value);
        }
    }
""");
            }
            return dbParams.ToString();
        }

        public static string CreateDbSelect(string className, AnalyzeDbColumn[] dbColumns, string joinQuery)
            => $$"""
    public static async IAsyncEnumerable<{{className}}> SelectAsync(
        NpgsqlConnection connection,
        DbColumnType distinctColumns = DbColumnType.None,
        DbQueryType selectColumns = DbQueryType.None,
        IDbCondition? where = null,
        IDbCondition? having = null,
        IDbOrder? order = null,
        long limit = 0,
        long offset = 0,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var parameters = new List<NpgsqlParameter>();

        var sqlBuilder = new StringBuilder("SELECT");

        var distinctColumnQueries = new List<string>();
        var selectQueries = new List<string>();
        var distinctQueryType = (DbQueryType)distinctColumns;
        foreach (var columnQueryTypes in DbQueryTypes)
        {
            if (distinctQueryType.HasFlag(columnQueryTypes))
            {
                distinctColumnQueries.Add(GetDbQuery(columnQueryTypes));
            }
            if (selectColumns.HasFlag(columnQueryTypes))
            {
                selectQueries.Add(GetDbQuery(columnQueryTypes));
            }
        }
        if (0 < distinctColumnQueries.Count)
        {
            sqlBuilder.Append($" DISTINCT ON ({string.Join(",", distinctColumnQueries)})");
        }
        if (0 < selectQueries.Count)
        {
            sqlBuilder.Append($" {string.Join(",", selectQueries)}");
        }
        sqlBuilder.Append($" FROM {DbTableName} {{joinQuery}}");
        int conditionOrdinal = 0;
        if (where != null)
        {
            sqlBuilder.Append($" WHERE {where.CreateQueryAndParameter(ref parameters, ref conditionOrdinal)}");
        }
        if (having != null)
        {
            sqlBuilder.Append($" HAVING {having.CreateQueryAndParameter(ref parameters, ref conditionOrdinal)}");
        }
        if (order != null)
        {
            sqlBuilder.Append($" ORDER BY {order.CreateQuery()}");
        }
        if (0 < limit)
        {
            sqlBuilder.Append($" LIMIT {limit}");
        }
        if (0 < offset)
        {
            sqlBuilder.Append($" OFFSET {offset}");
        }

        await using var command = new NpgsqlCommand(sqlBuilder.ToString(), connection);
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }
        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            int readerOrdinal = 0;
            var result = new {{className}}();
{{dbColumns.ForEachLines(x => $"selectColumns.HasFlag(DbQueryType.{x.PropertyName})".OutputIfStatement($"result.{x.PropertyName} = {x.ConverterType}.ReadData(reader ,readerOrdinal++);").OutputLine(3)).OutputLine()}}
            yield return result;
        }
    }

    public static IAsyncEnumerable<{{className}}> SelectAsync(
        NpgsqlConnection connection,
        DbQueryType selectColumns,
        IDbCondition? where = null,
        IDbCondition? having = null,
        IDbOrder? order = null,
        long limit = 0,
        long offset = 0,
        CancellationToken cancellationToken = default)
    {
        return SelectAsync(
            connection,
            DbColumnType.None,
            selectColumns,
            where,
            having,
            order,
            limit,
            offset,
            cancellationToken);
    }
""";

        public static string CreateDbCondition()
            => """
    public interface IDbCondition
    {
        string CreateQueryAndParameter(ref List<NpgsqlParameter> parameterList, ref int ordinal);
    }
    public class DbConditions : IDbCondition
    {
        public DbLogicOperator Logic { get; set; } = DbLogicOperator.And;
        public IEnumerable<IDbCondition> Conditions { get; set; } = Array.Empty<IDbCondition>();

        public DbConditions()
        {
        }
        public DbConditions(DbLogicOperator logicOperator,IEnumerable<IDbCondition> conditions)
        {
            Logic = logicOperator;
            Conditions = conditions;
        }
        public DbConditions(DbLogicOperator logicOperator,params IDbCondition[] conditions)
        {
            Logic = logicOperator;
            Conditions = conditions;
        }
        public string CreateQueryAndParameter(ref List<NpgsqlParameter> parameterList, ref int ordinal)
        {
            var conditionArray = Conditions.ToArray();
            var queryList = new List<string>(conditionArray.Length);
            foreach (var condition in conditionArray)
            {
                queryList.Add(condition.CreateQueryAndParameter(ref parameterList, ref ordinal));
            }
            return $"({string.Join(Logic.ToQuery(), queryList)})";
        }
    }
    public class DbCondition : IDbCondition
    {
        public DbCompareOperator Operator { get; set; } = DbCompareOperator.Equals;
        public IDbParam? Param { get; set; } = default;

        public DbCondition()
        {
        }
        public DbCondition(DbCompareOperator compareOperator ,IDbParam? param)
        {
            Operator = compareOperator;
            Param = param;
        }
        public string CreateQueryAndParameter(ref List<NpgsqlParameter> parameterList, ref int ordinal)
        {
            var paramName = $"@{Param.DbQuery}{ordinal++}";
            parameterList.Add(Param.CreateParameter(paramName));
            return $"({Param.DbQuery} {Operator.ToQuery()} {paramName})";
        }
    }
""";
        public static string CreateDbOrder()
            => """
    public interface IDbOrder
    {
        string CreateQuery();
    }
    public class DbOrders : IDbOrder
    {
        public IEnumerable<DbOrder> Orders { get; set; } = Array.Empty<DbOrder>();
        public DbOrders()
        {
        }
        public DbOrders(IEnumerable<DbOrder> orders)
        {
            Orders = orders;
        }
        public DbOrders(params DbOrder[] orders)
        {
            Orders = orders;
        }
        public string CreateQuery()
            => string.Join(',', Orders.Select(x => x.CreateQuery()));
    }
    public class DbOrder : IDbOrder
    {
        public DbOrderType Order { get; set; } = DbOrderType.Asc;
        public DbQueryType QueryType { get; set; }
        
        public DbOrder()
        {
        }
        public DbOrder(DbOrderType orderType ,DbQueryType queryType)
        {
            Order = orderType;
            QueryType = queryType;
        }
        public string CreateQuery()
            => $"{GetDbQuery(QueryType)} {Order.ToQuery()}";
    }
""";
    }
}
