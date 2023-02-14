using System;
using System.Collections.Generic;
using System.Text;
using CodeAnalyzeUtility;

namespace NpgsqlMappingGenerator.Utility
{
    internal static class OutputSourceUtility
    {
        public static string CreateDbTableProperty(AnalyzeClassInfo analyzeClassInfo)
            => $$"""
    public static readonly string DbTableName = "{{analyzeClassInfo.GetDbTableName()}}";
""";

        public static string CreateProperty(IEnumerable<DbColumnInfo> dbColumns)
        {
            return dbColumns.ForEachIndexLines((i, x) => $$"""public {{x.PropertyType}} {{x.PropertyName}} { get; set; }""").OutputLine(1);
        }

        public static string CreateDbColumnType(IEnumerable<DbColumnInfo> dbColumnInfos, IEnumerable<DbColumnInfo> dbQueryInfos)
            => $$"""
    [Flags]
    public enum DbColumnQueryType
    {
{{dbQueryInfos.ForEachIndexLines((i, x) => $"{x.PropertyName} = 1 << {i},").OutputLine(2)}}
        None = 0,
        AllColumn = {{dbColumnInfos.ForEachLines(x => x.PropertyName).OutputLine("|")}},
        All = {{dbQueryInfos.ForEachLines(x => x.PropertyName).OutputLine("|")}}
    }
    [Flags]
    public enum DbColumnType
    {
{{dbColumnInfos.ForEachIndexLines((i, x) => $"{x.PropertyName} = DbColumnQueryType.{x.PropertyName},").OutputLine(2)}}
        None = 0,
        All = {{dbColumnInfos.ForEachLines(x => x.PropertyName).OutputLine("|")}}
    }
    public static readonly DbColumnQueryType[] DbColumnQueryTypes = {
{{dbQueryInfos.ForEachLines(x => $"DbColumnQueryType.{x.PropertyName},").OutputLine(3)}}
        };
    public static string GetDbColumnQuery(DbColumnQueryType queryType)
        => queryType switch
        {
{{dbQueryInfos.ForEachLines(x => $"DbColumnQueryType.{x.PropertyName} => \"{x.Query}\",").OutputLine(3)}}
            _ => throw new NotImplementedException(),
        };
""";


        public static string CreateDbParam(IEnumerable<DbColumnInfo> dbColumns)
        {
            var dbParams = new StringBuilder();
            dbParams.AppendLine($$"""
    public interface IDbParam
    {
        DbColumnQueryType QueryType { get; }
        string DbTable { get; }
        string DbQuery { get; }
        NpgsqlParameter CreateParameter(string paramName);
    }
""");
            foreach (var dbQueryInfo in dbColumns)
            {
                var defaultValue = dbQueryInfo.PropertyType == "string" || dbQueryInfo.PropertyType == "System.String"
                    ? "string.Empty"
                    : "default";
                dbParams.AppendLine($$"""
    public class DbParam{{dbQueryInfo.PropertyName}} : IDbParam
    {
        public DbColumnQueryType QueryType => DbColumnQueryType.{{dbQueryInfo.PropertyName}};
        public string DbTable => DbTableName;
        public string DbQuery => GetDbColumnQuery(QueryType);
        public {{dbQueryInfo.PropertyType}} Value { get; private set; } = {{defaultValue}};

        public static DbParam{{dbQueryInfo.PropertyName}} Create({{dbQueryInfo.PropertyType}} value)
        {
            return new DbParam{{dbQueryInfo.PropertyName}}(value);
        }
        public DbParam{{dbQueryInfo.PropertyName}}()
        {
        }
        public DbParam{{dbQueryInfo.PropertyName}}({{dbQueryInfo.PropertyType}} value)
        {
            Value = value;
        }
        public NpgsqlParameter CreateParameter(string name)
        {
            return {{dbQueryInfo.ConverterType}}.CreateParameter(name, Value);
        }
    }
""");
            }
            return dbParams.ToString();
        }

        public static string CreateDbCondition()
            => """
    public interface IDbCondition
    {
        string CreateQueryAndParameter(ref List<NpgsqlParameter> parameterList, ref int ordinal);
    }
    public class DbConditions : IDbCondition
    {
        public DbLogicOperator Logic { get; set; } = DbLogicOperator.And;
        public IDbCondition[] Conditions { get; set; } = Array.Empty<IDbCondition>();

        public static DbConditions Create(DbLogicOperator logicOperator,params IDbCondition[] conditions)
        {
            return new DbConditions(logicOperator,conditions);
        }
        public DbConditions()
        {
        }
        public DbConditions(DbLogicOperator logicOperator,IEnumerable<IDbCondition> conditions)
        {
            Logic = logicOperator;
            Conditions = conditions.ToArray();
        }
        public string CreateQueryAndParameter(ref List<NpgsqlParameter> parameterList, ref int ordinal)
        {
            var queryList = new List<string>(Conditions.Length);
            foreach (var condition in Conditions)
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

        public static DbCondition Create(DbCompareOperator compareOperator ,IDbParam? param)
        {
            return new DbCondition(compareOperator,param);
        }
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
        public DbOrder[] Orders { get; set; } = Array.Empty<DbOrder>();
        public static DbOrders Create(params DbOrder[] orders)
        {
            return new DbOrders(orders);
        }
        public DbOrders()
        {
        }
        public DbOrders(IEnumerable<DbOrder> orders)
        {
            Orders = orders.ToArray();
        }
        public string CreateQuery()
            => string.Join(',', Orders.Select(x => x.CreateQuery()));
    }
    public class DbOrder : IDbOrder
    {
        public DbOrderType Order { get; set; } = DbOrderType.Asc;
        public DbColumnQueryType QueryType { get; set; }
        
        public static DbOrder Create(DbOrderType orderType ,DbColumnQueryType queryType)
        {
            return new DbOrder(orderType,queryType);
        }
        public DbOrder()
        {
        }
        public DbOrder(DbOrderType orderType ,DbColumnQueryType queryType)
        {
            Order = orderType;
            QueryType = queryType;
        }
        public string CreateQuery()
            => $"{GetDbColumnQuery(QueryType)} {Order.ToQuery()}";
    }
""";

        public static string CreateDbSelect(string className, IEnumerable<DbColumnInfo> dbColumns, string joinQuery = "")
            => $$"""
    public static async IAsyncEnumerable<{{className}}> SelectAsync(
        NpgsqlConnection connection,
        DbColumnType distinctColumns = DbColumnType.None,
        DbColumnQueryType selectColumns = DbColumnQueryType.None,
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
        var selectColumnQueries = new List<string>();
        var distinctQueryType = (DbColumnQueryType)distinctColumns;
        foreach (var columnQueryTypes in DbColumnQueryTypes)
        {
            if (distinctQueryType.HasFlag(columnQueryTypes))
            {
                distinctColumnQueries.Add(GetDbColumnQuery(columnQueryTypes));
            }
            if (selectColumns.HasFlag(columnQueryTypes))
            {
                selectColumnQueries.Add(GetDbColumnQuery(columnQueryTypes));
            }
        }
        if (0 < distinctColumnQueries.Count)
        {
            sqlBuilder.Append($" DISTINCT ON ({string.Join(",", distinctColumnQueries)})");
        }
        if (0 < selectColumnQueries.Count)
        {
            sqlBuilder.Append($" {string.Join(",", selectColumnQueries)}");
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
{{dbColumns.ForEachLines(x => $"selectColumns.HasFlag(DbColumnQueryType.{x.PropertyName})".OutputIfStatement($"result.{x.PropertyName} = {x.ConverterType}.ReadData(reader ,readerOrdinal++);").OutputLine(3)).OutputLine()}}
            yield return result;
        }
    }

    public static IAsyncEnumerable<{{className}}> SelectAsync(
        NpgsqlConnection connection,
        DbColumnQueryType selectColumns,
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


    }
}
