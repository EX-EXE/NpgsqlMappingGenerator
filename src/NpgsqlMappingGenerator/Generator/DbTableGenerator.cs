﻿using CodeAnalyzeUtility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NpgsqlMappingGenerator.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace NpgsqlMappingGenerator.Generator;

public static class DbTableGenerator
{
    /// <summary>
    /// Attribute
    /// </summary>
    /// <param name="context"></param>
    public static void GenerateAttribute(IncrementalGeneratorPostInitializationContext context)
    {
        // DbTable
        context.AddSource($"{CommonDefine.Namespace}.{CommonDefine.DbTableAttributeName}.cs", $$"""
namespace {{CommonDefine.Namespace}};
using System;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal sealed class {{CommonDefine.DbTableAttributeName}} : Attribute
{
    public {{CommonDefine.DbTableAttributeName}}(string tableName)
    {
    }
}
""");
        // DbColumn
        context.AddSource($"{CommonDefine.Namespace}.{CommonDefine.DbColumnAttributeName}.cs", $$"""
namespace {{CommonDefine.Namespace}};
using System;
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal sealed class {{CommonDefine.DbColumnAttributeName}}<T> : Attribute
{
    public {{CommonDefine.DbColumnAttributeName}}(string paramName)
    {
    }
}
""");
    }

    /// <summary>
    /// Generate
    /// </summary>
    /// <param name="context"></param>
    /// <param name="source"></param>
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
        var dbTable = AnalyzeDbTable.Analyze(classInfo, cancellationToken);
        var dbColumns = dbTable.DbColumns.Values.ToArray();
        var dbAggregates = dbColumns.SelectMany(x => x.AggregateColumns).ToArray();
        var dbQueries = dbColumns.Concat(dbAggregates).ToArray();

        // Source
        var sourceCode = $$$"""
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
using {{{CommonDefine.Namespace}}};

{{{classInfo.Type.GetNamespaceDefine()}}}

partial class {{{classInfo.Type.ShortName}}}
{
{{{OutputSourceUtility.CreateDbTableProperty(dbTable)}}}
{{{OutputSourceUtility.CreateColumnProperty(dbAggregates)}}}

{{{OutputSourceUtility.CreateDbType(dbColumns,dbQueries,Array.Empty<(string,string)>())}}}

{{{OutputSourceUtility.CreateDbParam(dbColumns)}}}

{{{OutputSourceUtility.CreateDbCondition()}}}
{{{OutputSourceUtility.CreateDbOrder()}}}

{{{OutputSourceUtility.CreateDbSelect(classInfo.Type.ShortName, dbColumns, string.Empty)}}}

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
{{{dbColumns.Where(x => !string.IsNullOrEmpty(x.InsertDefault)).ForEachLines(x => $"!dbParamsList.Where(x => x.QueryType == DbColumnQueryType.{x.PropertyName}).Any()".OutputIfStatement($"dbParamsList.Add(new DbParam{x.PropertyName}({x.InsertDefault}));").OutputLine(2)).OutputLine()}}}
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
{{{dbColumns.Where(x => !string.IsNullOrEmpty(x.UpdateDefault)).ForEachLines(x => $"!dbParamsList.Where(x => x.QueryType == DbColumnQueryType.{x.PropertyName}).Any()".OutputIfStatement($"dbParamsList.Add(new DbParam{x.PropertyName}({x.UpdateDefault}));").OutputLine(2)).OutputLine()}}}
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

    public static async ValueTask<int> UpsertAsync(
        NpgsqlConnection connection,
        DbColumnType conflictColumns,
        IEnumerable<IDbParam> insertParams,
        IEnumerable<IDbParam> updateParams,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Query
        int ordinal = 0;
        var parameters = new List<NpgsqlParameter>();
        
        // Conflict
        var conflictColumnQueries = DbColumnTypes.Where(x => x.HasFlag(conflictColumns)).Select(x =>GetDbQuery((DbQueryType)x)).ToArray();

        // Insert
        var insertColumnNames = new List<string>();
        var insertParameterNames = new List<string>();
        var insertParameterList = insertParams.ToList();
{{{dbColumns.Where(x => !string.IsNullOrEmpty(x.InsertDefault)).ForEachLines(x => $"!insertParams.Where(x => x.QueryType == DbColumnQueryType.{x.PropertyName}).Any()".OutputIfStatement($"insertParameterList.Add(new DbParam{x.PropertyName}({x.InsertDefault}));").OutputLine(2)).OutputLine()}}}
        foreach (var insertParameter in insertParameterList)
        {
            var columnName = insertParameter.DbQuery;
            insertColumnNames.Add(columnName);
            var insertParameterName = $"@{columnName}{ordinal++}";
            insertParameterNames.Add(insertParameterName);
            parameters.Add(insertParameter.CreateParameter(insertParameterName));
        }

        // Update
        var updateParameterQueries = new List<string>();
        var updateParameterList = updateParams.ToList();
{{{dbColumns.Where(x => !string.IsNullOrEmpty(x.UpdateDefault)).ForEachLines(x => $"!updateParams.Where(x => x.QueryType == DbColumnQueryType.{x.PropertyName}).Any()".OutputIfStatement($"updateParameterList.Add(new DbParam{x.PropertyName}({x.UpdateDefault}));").OutputLine(2)).OutputLine()}}}
        foreach (var updateParameter in updateParameterList)
        {
            var columnName = updateParameter.DbQuery;
            var updateParameterName = $"@{columnName}{ordinal++}";
            updateParameterQueries.Add($"{columnName} = {updateParameterName}");
            parameters.Add(updateParameter.CreateParameter(updateParameterName));
        }

        var sql = $"INSERT INTO {DbTableName} ({string.Join(",",insertColumnNames)}) VALUES ({string.Join(",",insertParameterNames)}) ON CONFLICT ({string.Join(",",conflictColumnQueries)}) DO UPDATE SET {string.Join(",",updateParameterQueries)}";
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }
        await command.PrepareAsync(cancellationToken).ConfigureAwait(false);
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public static ValueTask<int> UpsertAsync(
        NpgsqlConnection connection,
        DbColumnType conflictColumns,
        IEnumerable<IDbParam> upsertParams,
        CancellationToken cancellationToken = default)
    {
        return UpsertAsync(
            connection,
            conflictColumns,
            upsertParams,
            upsertParams,
            cancellationToken);
    }
}

""";
        // AddSourceで出力
        context.AddSource($"{CommonDefine.Namespace}.{CommonDefine.DbTableAttributeName}.{classInfo.Type.FullName}.g.cs", sourceCode);
    }
}