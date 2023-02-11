using CodeAnalyzeUtility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace NpgsqlMappingGenerator;

public static class DbTable
{
    public static void GenerateAttribute(IncrementalGeneratorPostInitializationContext context)
    {
        // DbTable
        context.AddSource($"{Common.Namespace}.{Common.DbTableAttributeName}.cs", $$"""
namespace {{Common.Namespace}};
using System;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal sealed class {{Common.DbTableAttributeName}} : Attribute
{
    public {{Common.DbTableAttributeName}}(string tableName)
    {
    }
}
""");
        // DbColumn
        context.AddSource($"{Common.Namespace}.{Common.DbColumnAttributeName}.cs", $$"""
namespace {{Common.Namespace}};
using System;
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal sealed class {{Common.DbColumnAttributeName}}<T> : Attribute
{
    public {{Common.DbColumnAttributeName}}(string paramName)
    {
    }
}
""");
    }

    private record DbColumnInfo(string PropertyType, string PropertyName, string Query, string ConverterType, string InsertDefault, string UpdateDefault);
    public static void GenerateSource(SourceProductionContext context, GeneratorAttributeSyntaxContext source)
    {
        var cancellationToken = context.CancellationToken;
        var semanticModel = source.SemanticModel;
        var typeSymbol = source.TargetSymbol as INamedTypeSymbol;
        if (typeSymbol == null)
        {
            return;
        }
        var classInfo = AnalyzeClassInfo.Analyze(typeSymbol, cancellationToken);

        var dbQueryInfos = new List<DbColumnInfo>();
        var dbColumnInfos = new List<DbColumnInfo>();
        var dbAggregateInfos = new List<DbColumnInfo>();
        foreach (var propertyInfo in classInfo.Properties)
        {
            var columnAttribute = propertyInfo.Attributes.FirstOrDefault(x => x.Type.FullName == $"{Common.Namespace}.{Common.DbColumnAttributeName}");
            if (columnAttribute == default)
            {
                continue;
            }
            var propertyType = propertyInfo.Type.FullNameWithGenerics;
            var propertyName = propertyInfo.Name;
            var columnName = columnAttribute.ArgumentObjects.OfType<string>().First();
            var converterType = columnAttribute.GenericTypes.First().FullNameWithGenerics;
            var insertDefault = string.Empty;
            var updateDefault = string.Empty;

            var autoCreateAttribute = propertyInfo.Attributes.FirstOrDefault(x => x.Type.FullName == $"{Common.Namespace}.{Common.DbAutoCreateAttribute}");
            if (autoCreateAttribute != default)
            {
                var autoCreateClass = autoCreateAttribute.GenericTypes.First().FullNameWithGenerics;
                var autoCreateArgument = autoCreateAttribute.ArgumentStrings.FirstOrDefault();
                if (autoCreateArgument.Contains(Common.DbAutoCreateType_Insert))
                {
                    insertDefault = $"{autoCreateClass}.CreateInsertValue()";
                }
                if (autoCreateArgument.Contains(Common.DbAutoCreateType_Update))
                {
                    updateDefault = $"{autoCreateClass}.CreateUpdateValue()";
                }
            }

            // Column
            var columnInfo = new DbColumnInfo(propertyType, propertyName, columnName, converterType, insertDefault, updateDefault);
            dbQueryInfos.Add(columnInfo);
            dbColumnInfos.Add(columnInfo);

            // Aggregate
            var aggregateAttribute = propertyInfo.Attributes.FirstOrDefault(x => x.Type.FullName == $"{Common.Namespace}.{Common.DbAggregateAttributeName}");
            if (aggregateAttribute != default)
            {
                var aggregateArgument = aggregateAttribute.ArgumentStrings.FirstOrDefault();
                if (aggregateArgument.Contains(Common.DbAggregateType_Count))
                {
                    var info = new DbColumnInfo("long", $"{propertyName}Count", $"count({columnName})", $"{Common.Namespace}.DbParamLong", string.Empty, string.Empty);
                    dbQueryInfos.Add(info);
                    dbAggregateInfos.Add(info);
                }
                if (aggregateArgument.Contains(Common.DbAggregateType_Avg))
                {
                    var info = new DbColumnInfo("double", $"{propertyName}Avg", $"avg({columnName})", $"{Common.Namespace}.DbParamDouble", string.Empty, string.Empty);
                    dbQueryInfos.Add(info);
                    dbAggregateInfos.Add(info);
                }
                if (aggregateArgument.Contains(Common.DbAggregateType_Max))
                {
                    var info = new DbColumnInfo(propertyType, $"{propertyName}Max", $"max({columnName})", converterType, string.Empty, string.Empty);
                    dbQueryInfos.Add(info);
                    dbAggregateInfos.Add(info);
                }
                if (aggregateArgument.Contains(Common.DbAggregateType_Min))
                {
                    var info = new DbColumnInfo(propertyType, $"{propertyName}Min", $"min({columnName})", converterType, string.Empty, string.Empty);
                    dbQueryInfos.Add(info);
                    dbAggregateInfos.Add(info);
                }
            }
        }

        // DbParam
        var dbParams = new StringBuilder();
        foreach (var dbQueryInfo in dbQueryInfos)
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


        // Source
        var sourceCode = $$"""
// <auto-generated/>
#nullable enable
#pragma warning disable CS8600
#pragma warning disable CS8601
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8604

using System;
using System.Text;
using System.Runtime.CompilerServices;
using Npgsql;
using {{Common.Namespace}};

{{classInfo.Type.GetNamespaceDefine()}}

partial class {{classInfo.Type.ShortName}}
{
    public static readonly string DbTableName = "{{classInfo.Attributes.Where(x => x.Type.FullName == $"{Common.Namespace}.{Common.DbTableAttributeName}").First().ArgumentObjects.OfType<string>().First()}}";
{{dbAggregateInfos.ForEachIndexLines((i, x) => $$"""public {{x.PropertyType}} {{x.PropertyName}} { get; set; }""").OutputLine(1)}}

    [Flags]
    public enum DbColumnQueryType
    {
{{dbQueryInfos.ForEachIndexLines((i, x) => $"{x.PropertyName} = 1 << {i + 1},").OutputLine(2)}}
        None = 0,
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

    public interface IDbParam
    {
        DbColumnQueryType QueryType { get; }
        string DbTable { get; }
        string DbQuery { get; }
        NpgsqlParameter CreateParameter(string paramName);
    }
{{dbParams}}

    public static async IAsyncEnumerable<{{classInfo.Type.ShortName}}> SelectAsync(
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
        sqlBuilder.Append($" FROM {DbTableName}");
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
            var result = new {{classInfo.Type.ShortName}}();
{{dbQueryInfos.ForEachLines(x => $"selectColumns.HasFlag(DbColumnQueryType.{x.PropertyName})".OutputIfStatement($"result.{x.PropertyName} = {x.ConverterType}.ReadData(reader ,readerOrdinal++);").OutputLine(3)).OutputLine()}}
            yield return result;
        }
    }

    public static async ValueTask<int> InsertAsync(
        NpgsqlConnection connection,
        IEnumerable<IDbParam> dbParams,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        int ordinal = 0;
        var columnNames = new List<string>();
        var parameterNames = new List<string>();
        var parameters = new List<NpgsqlParameter>();
        var dbParamsList = dbParams.ToList();
{{dbColumnInfos.Where(x => !string.IsNullOrEmpty(x.InsertDefault)).ForEachLines(x => $"!dbParamsList.Where(x => x.QueryType == DbColumnQueryType.{x.PropertyName}).Any()".OutputIfStatement($"dbParamsList.Add(new DbParam{x.PropertyName}({x.InsertDefault}));").OutputLine(2)).OutputLine()}}
        foreach (var dbParam in dbParamsList)
        {
            var columnName = dbParam.DbQuery;
            columnNames.Add(columnName);
            var paramName = $"@{columnName}{ordinal++}";
            parameterNames.Add(paramName);
            parameters.Add(dbParam.CreateParameter(paramName));
        }
        var sqlBuilder = new StringBuilder($"INSERT INTO {DbTableName}");
        sqlBuilder.Append($" ({string.Join(",",columnNames)})");
        sqlBuilder.Append($" VALUES ({string.Join(",",parameterNames)})");

        await using var command = new NpgsqlCommand(sqlBuilder.ToString(), connection);
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }
        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask<int> UpdateAsync(
        NpgsqlConnection connection,
        IEnumerable<IDbParam> dbParams,
        IDbCondition? where = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        int ordinal = 0;
        var sets = new List<string>();
        var parameterNames = new List<string>();
        var parameters = new List<NpgsqlParameter>();
        var dbParamsList = dbParams.ToList();
{{dbColumnInfos.Where(x => !string.IsNullOrEmpty(x.UpdateDefault)).ForEachLines(x => $"!dbParamsList.Where(x => x.QueryType == DbColumnQueryType.{x.PropertyName}).Any()".OutputIfStatement($"dbParamsList.Add(new DbParam{x.PropertyName}({x.UpdateDefault}));").OutputLine(2)).OutputLine()}}
        foreach (var dbParam in dbParamsList)
        {
            var columnName = dbParam.DbQuery;
            var paramName = $"@{columnName}{ordinal++}";
            sets.Add($"{columnName} = {paramName}");
            parameters.Add(dbParam.CreateParameter(paramName));
        }

        var sqlBuilder = new StringBuilder($"UPDATE {DbTableName} SET {string.Join(",",sets)}");
        if (where != null)
        {
            sqlBuilder.Append($" WHERE {where.CreateQueryAndParameter(ref parameters, ref ordinal)}");
        }

        await using var command = new NpgsqlCommand(sqlBuilder.ToString(), connection);
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }
        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask<int> DeleteAsync(
        NpgsqlConnection connection,
        IDbCondition? where = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sqlBuilder = new StringBuilder($"DELETE FROM {DbTableName}");
        int conditionOrdinal = 0;
        var parameters = new List<NpgsqlParameter>();
        if (where != null)
        {
            sqlBuilder.Append($" WHERE {where.CreateQueryAndParameter(ref parameters, ref conditionOrdinal)}");
        }

        await using var command = new NpgsqlCommand(sqlBuilder.ToString(), connection);
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }
        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public static IAsyncEnumerable<{{classInfo.Type.ShortName}}> SelectAsync(
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
}

""";
        // AddSourceで出力
        context.AddSource($"{Common.Namespace}.{Common.DbTableAttributeName}.{classInfo.Type.FullName}.g.cs", sourceCode);
    }

    //public static class DiagnosticDescriptors
    //{
    //    public static readonly DiagnosticDescriptor NotAllowedStringType = new(
    //        id: "000",
    //        title: "String type is not allowed.",
    //        messageFormat: "String type is not allowed.",
    //        category: GeneratorNamespace,
    //        defaultSeverity: DiagnosticSeverity.Error,
    //        isEnabledByDefault: true);

    //    public static readonly DiagnosticDescriptor NotAllowedDuplicateType = new(
    //        id: "001",
    //        title: "Duplicate type is not allowed.",
    //        messageFormat: "Duplicate type is not allowed.",
    //        category: GeneratorNamespace,
    //        defaultSeverity: DiagnosticSeverity.Error,
    //        isEnabledByDefault: true);

    //    public static readonly DiagnosticDescriptor NotAllowedSameTypes = new(
    //        id: "002",
    //        title: "Same types is not allowed.",
    //        messageFormat: "Same types is not allowed.",
    //        category: GeneratorNamespace,
    //        defaultSeverity: DiagnosticSeverity.Error,
    //        isEnabledByDefault: true);

    //    public static readonly DiagnosticDescriptor NotAllowedStringOnly = new(
    //        id: "003",
    //        title: "String only is not allowed.",
    //        messageFormat: "String only is not allowed.",
    //        category: GeneratorNamespace,
    //        defaultSeverity: DiagnosticSeverity.Error,
    //        isEnabledByDefault: true);
    //}

}