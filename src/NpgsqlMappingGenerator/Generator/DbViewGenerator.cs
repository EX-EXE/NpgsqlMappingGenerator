using CodeAnalyzeUtility;
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

public static class DbViewGenerator
{
    private static readonly string[] DbViewAttributeNames = new[]
    {
        $"{CommonDefine.Namespace}.{CommonDefine.DbTableAttributeName}",
        $"{CommonDefine.Namespace}.{CommonDefine.DbViewInnerJoinAttributeName}",
        $"{CommonDefine.Namespace}.{CommonDefine.DbViewLeftOuterJoinAttributeName}",
        $"{CommonDefine.Namespace}.{CommonDefine.DbViewRightOuterJoinAttributeName}",
        $"{CommonDefine.Namespace}.{CommonDefine.DbViewFullOuterJoinAttributeName}",
        $"{CommonDefine.Namespace}.{CommonDefine.DbViewCrossJoinAttributeName}",
    };

    /// <summary>
    /// Attribute
    /// </summary>
    /// <param name="context"></param>
    public static void GenerateAttribute(IncrementalGeneratorPostInitializationContext context)
    {
        // DbView
        context.AddSource($"{CommonDefine.DbViewAttributeFullName}.cs", $$"""
namespace {{CommonDefine.Namespace}};
using System;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal sealed class {{CommonDefine.DbViewAttributeName}} : Attribute
{
    public {{CommonDefine.DbViewAttributeName}}()
    {
    }
}
""");
        // DbViewTable
        context.AddSource($"{CommonDefine.DbViewTableAttributeFullName}.cs", $$"""
namespace {{CommonDefine.Namespace}};
using System;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal sealed class {{CommonDefine.DbViewTableAttributeName}}<TableClass> : Attribute
{
    public {{CommonDefine.DbViewTableAttributeName}}()
    {
    }
}
""");
        // DbViewColumn
        context.AddSource($"{CommonDefine.DbViewColumnAttributeFullName}.cs", $$"""
namespace {{CommonDefine.Namespace}};
using System;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
internal sealed class {{CommonDefine.DbViewColumnAttributeName}}<TableClass> : Attribute
{
    public {{CommonDefine.DbViewColumnAttributeName}}(string columnProperty,DbAggregateType aggregateType = DbAggregateType.None)
    {
    }
}
""");

        // Inner Join / Outer Join
        foreach (var tableJoinAttributeName in CommonDefine.DbViewInnerOrOuterJoinAttributeNames)
        {
            context.AddSource($"{CommonDefine.Namespace}.{tableJoinAttributeName}.cs", $$"""
namespace {{CommonDefine.Namespace}};
using System;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
internal sealed class {{tableJoinAttributeName}}<JoinTableClass,CompareTableClass> : Attribute
{
    public {{tableJoinAttributeName}}(string joinTableColumnProperty,string compareTableColumnProperty)
    {
    }
}
""");
        }
        // Cross Join
        context.AddSource($"{CommonDefine.DbViewCrossJoinAttributeFullName}.cs", $$"""
namespace {{CommonDefine.Namespace}};
using System;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
internal sealed class {{CommonDefine.DbViewCrossJoinAttributeName}}<JoinTableClass> : Attribute
{
    public {{CommonDefine.DbViewCrossJoinAttributeName}}()
    {
    }
}
""");
    }

    /// <summary>
    /// Source
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
        // ClassInfo
        var viewClassInfo = AnalyzeClassInfo.Analyze(typeSymbol, cancellationToken);
        var classInfoDict = new Dictionary<string, AnalyzeClassInfo>();
        foreach (var attribute in viewClassInfo.Attributes.Where(x => DbViewAttributeNames.Contains(x.Type.FullName)))
        {
            foreach (var genericType in attribute.GenericTypes)
            {
                if (!classInfoDict.ContainsKey(genericType.FullNameWithGenerics))
                {
                    var classSymbol = genericType.Symbol as INamedTypeSymbol;
                    if (classSymbol == default)
                    {
                        return;
                    }
                    classInfoDict[genericType.FullNameWithGenerics] = AnalyzeClassInfo.Analyze(classSymbol, cancellationToken);
                }
            }
        }

        // Attribute
        var fromQuery = $"FROM {viewClassInfo.GetDbViewTableName(classInfoDict)}";
        var joinQuery = viewClassInfo.GetDbTableJoinQuery(classInfoDict);

        // DbColumns
        var dbQueryInfos = new List<DbColumnInfo>();
        var dbColumnInfos = new List<DbColumnInfo>();
        var createPropertyInfos = new List<DbColumnInfo>();
        var dbAutoCreateInfos = new List<DbAutoCreateInfo>();
        foreach (var attributeInfo in viewClassInfo.Attributes.Where(x => x.Type.FullName == CommonDefine.DbViewColumnAttributeFullName))
        {
            if (attributeInfo.GenericTypes.Length <= 0)
            {
                continue;
            }
            if (attributeInfo.ArgumentObjects.Length <= 1)
            {
                continue;
            }
            var genericType = attributeInfo.GenericTypes[0];
            var propertyObj = attributeInfo.ArgumentObjects[0];
            var aggregateObj = attributeInfo.ArgumentObjects[1];
            if (classInfoDict.TryGetValue(genericType.FullNameWithGenerics, out var analyzeClassInfo) &&
                propertyObj is string propertyStr &&
                aggregateObj is int propertyNum)
            {
                var property = analyzeClassInfo.Properties.Where(x => x.Name == propertyStr).First();
                var columnInfo = property.GetDbColumnInfo(analyzeClassInfo.GetDbTableName());
                if (columnInfo != null)
                {
                    var table = analyzeClassInfo.GetDbTableName();
                    var column = property.GetDbColumnName();
                    // Column
                    dbQueryInfos.Add(columnInfo);
                    dbColumnInfos.Add(columnInfo);
                    createPropertyInfos.Add(columnInfo);
                    // Aggregate
                    var aggregateInfos = columnInfo.GetDbAggregateInfos(propertyNum);
                    dbQueryInfos.AddRange(aggregateInfos);
                    createPropertyInfos.AddRange(aggregateInfos);
                }
                else
                {
                    continue;
                }
            }
            else
            {
                continue;
            }

        }
        var viewTableAttribute = viewClassInfo.Attributes.FirstOrDefault(x => x.Type.FullName == CommonDefine.DbViewTableAttributeFullName);
        if (viewTableAttribute == default || viewTableAttribute.GenericTypes.Length <= 0)
        {
            return;
        }
        var dbTableClass = classInfoDict[viewTableAttribute.GenericTypes[0].FullNameWithGenerics];

        // Source
        var sourceCode = $$"""
// <auto-generated/>
#nullable enable
#pragma warning disable CS8600
#pragma warning disable CS8601
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8604
#pragma warning disable CS8618

using System;
using System.Text;
using System.Runtime.CompilerServices;
using Npgsql;
using {{CommonDefine.Namespace}};

{{viewClassInfo.Type.GetNamespaceDefine()}}

partial class {{viewClassInfo.Type.ShortName}}
{
{{OutputSourceUtility.CreateDbTableProperty(dbTableClass)}}
{{OutputSourceUtility.CreateProperty(dbQueryInfos)}}
{{OutputSourceUtility.CreateDbColumnType(dbColumnInfos, dbQueryInfos)}}
{{OutputSourceUtility.CreateDbParam(dbQueryInfos)}}

{{OutputSourceUtility.CreateDbCondition()}}
{{OutputSourceUtility.CreateDbOrder()}}

{{OutputSourceUtility.CreateDbSelect(viewClassInfo.Type.ShortName, dbQueryInfos, joinQuery)}}
}

""";
        // AddSourceで出力
        context.AddSource($"{CommonDefine.DbTableAttributeFullName}.{viewClassInfo.Type.FullName}.g.cs", sourceCode);
    }
}