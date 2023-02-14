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

public static class DbView
{
    private static readonly string[] DbViewAttributeNames = new[]
    {
        $"{Common.Namespace}.{Common.DbTableAttributeName}",
        $"{Common.Namespace}.{Common.DbViewInnerJoinAttributeName}",
        $"{Common.Namespace}.{Common.DbViewLeftOuterJoinAttributeName}",
        $"{Common.Namespace}.{Common.DbViewRightOuterJoinAttributeName}",
        $"{Common.Namespace}.{Common.DbViewFullOuterJoinAttributeName}",
        $"{Common.Namespace}.{Common.DbViewCrossJoinAttributeName}",
    };

    public static void GenerateAttribute(IncrementalGeneratorPostInitializationContext context)
    {
        // DbView
        context.AddSource($"{Common.Namespace}.{Common.DbViewAttributeName}.cs", $$"""
namespace {{Common.Namespace}};
using System;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal sealed class {{Common.DbViewAttributeName}} : Attribute
{
    public {{Common.DbViewAttributeName}}()
    {
    }
}
""");
        context.AddSource($"{Common.Namespace}.{Common.DbViewTableAttributeName}.cs", $$"""
namespace {{Common.Namespace}};
using System;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal sealed class {{Common.DbViewTableAttributeName}}<TableClass> : Attribute
{
    public {{Common.DbViewTableAttributeName}}()
    {
    }
}
""");
        context.AddSource($"{Common.Namespace}.{Common.DbViewColumnAttributeName}.cs", $$"""
namespace {{Common.Namespace}};
using System;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
internal sealed class {{Common.DbViewColumnAttributeName}}<TableClass> : Attribute
{
    public {{Common.DbViewColumnAttributeName}}(string columnProperty,DbAggregateType aggregateType = DbAggregateType.None)
    {
    }
}
""");

        // Join
        foreach (var joinAttributeName in new[] {
            Common.DbViewInnerJoinAttributeName ,
            Common.DbViewLeftOuterJoinAttributeName ,
            Common.DbViewRightOuterJoinAttributeName ,
            Common.DbViewFullOuterJoinAttributeName })
        {
            context.AddSource($"{Common.Namespace}.{joinAttributeName}.cs", $$"""
namespace {{Common.Namespace}};
using System;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
internal sealed class {{joinAttributeName}}<JoinTableClass,CompTableClass> : Attribute
{
    public {{joinAttributeName}}(string joinTableColumnProperty,string compTableColumnProperty)
    {
    }
}
""");
        }
        context.AddSource($"{Common.Namespace}.{Common.DbViewCrossJoinAttributeName}.cs", $$"""
namespace {{Common.Namespace}};
using System;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
internal sealed class {{Common.DbViewCrossJoinAttributeName}}<JoinTableClass> : Attribute
{
    public {{Common.DbViewCrossJoinAttributeName}}()
    {
    }
}
""");
    }

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
        foreach (var attributeInfo in viewClassInfo.Attributes.Where(x => x.Type.FullName == $"{Common.Namespace}.{Common.DbViewColumnAttributeName}"))
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
        var viewTableAttribute = viewClassInfo.Attributes.FirstOrDefault(x => x.Type.FullName == $"{Common.Namespace}.{Common.DbViewTableAttributeName}");
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
using {{Common.Namespace}};

{{viewClassInfo.Type.GetNamespaceDefine()}}

partial class {{viewClassInfo.Type.ShortName}}
{
{{OutputSource.CreateDbTableProperty(dbTableClass)}}
{{OutputSource.CreateProperty(dbQueryInfos)}}
{{OutputSource.CreateDbColumnType(dbColumnInfos, dbQueryInfos)}}
{{OutputSource.CreateDbParam(dbQueryInfos)}}

{{OutputSource.CreateDbCondition()}}
{{OutputSource.CreateDbOrder()}}

{{OutputSource.CreateDbSelect(viewClassInfo.Type.ShortName, dbQueryInfos, joinQuery)}}
}

""";
        // AddSourceで出力
        context.AddSource($"{Common.Namespace}.{Common.DbTableAttributeName}.{viewClassInfo.Type.FullName}.g.cs", sourceCode);
    }
}