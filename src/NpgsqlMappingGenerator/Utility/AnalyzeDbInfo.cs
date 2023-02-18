using CodeAnalyzeUtility;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace NpgsqlMappingGenerator.Utility
{

    internal class AnalyzeDbView
    {
        public AnalyzeClassInfo ClassInfo { get; init; }
        public AnalyzeDbTable BaseTable { get; set; }
        public AnalyzeDbColumn[] DbColumns { get; private set; } = Array.Empty<AnalyzeDbColumn>();
        public AnalyzeDbColumn[] DbQueries { get; private set; } = Array.Empty<AnalyzeDbColumn>();
        public Dictionary<string, AnalyzeDbTable> DbTables { get; private set; } = new Dictionary<string, AnalyzeDbTable>();
        public string JoinQuery = string.Empty;

        public AnalyzeDbView(AnalyzeClassInfo analyzeClassInfo, AnalyzeDbTable baseTable)
        {
            ClassInfo = analyzeClassInfo;
            BaseTable = baseTable;
        }

        public static AnalyzeDbView Analyze(AnalyzeClassInfo analyzeClassInfo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // DbTables
            var dbTables = new Dictionary<string, AnalyzeDbTable>();
            var viewAttributes = analyzeClassInfo.Attributes.Where(x => x.Type.FullName == CommonDefine.DbViewAttributeFullName || CommonDefine.DbViewJoinAttributeFullNames.Contains(x.Type.FullName)).ToArray();
            // Cache
            foreach (var attribute in viewAttributes)
            {
                foreach (var genericType in attribute.GenericTypes)
                {
                    if (!dbTables.ContainsKey(genericType.FullNameWithGenerics))
                    {
                        var classSymbol = genericType.Symbol as INamedTypeSymbol;
                        if (classSymbol == default)
                        {
                            continue;
                        }
                        var classInfo = AnalyzeClassInfo.Analyze(classSymbol, cancellationToken);
                        dbTables[genericType.FullNameWithGenerics] = AnalyzeDbTable.Analyze(classInfo, cancellationToken);
                    }
                }
            }
            // Join
            var joinQueryBuilder = new StringBuilder();
            foreach (var attribute in viewAttributes)
            {
                var attributeName = attribute.Type.FullName;
                if (CommonDefine.DbViewInnerOrOuterJoinAttributeFullNames.Contains(attributeName))
                {
                    var tables = attribute.GenericTypes.Zip(attribute.ArgumentObjects, (tableTypeInfo, columnName) => (tableTypeInfo, columnName)).Take(2).ToArray();
                    if (tables.Length == 2 &&
                        dbTables.TryGetValue(tables[0].tableTypeInfo.FullNameWithGenerics, out var joinTable) && tables[0].columnName is string joinColumnName &&
                        dbTables.TryGetValue(tables[1].tableTypeInfo.FullNameWithGenerics, out var compTable) && tables[1].columnName is string compColumnName)
                    {
                        if (joinTable.DbColumns.TryGetValue(joinColumnName, out var joinColumn) &&
                            compTable.DbColumns.TryGetValue(compColumnName, out var compColumn))
                        {
                            joinQueryBuilder.Append($" {attribute.GetDbTableJoinPrefixQuery()} {joinTable.DbTableName} ON {joinTable.DbTableName}.{joinColumn.DbColumnName} = {compTable.DbTableName}.{compColumn.DbColumnName}");
                        }
                    }
                }
                else if (CommonDefine.DbViewCrossJoinAttributeFullName == attributeName)
                {
                    var tableType = attribute.GenericTypes.FirstOrDefault();
                    if (tableType != default &&
                         dbTables.TryGetValue(tableType.FullNameWithGenerics, out var joinTable))
                    {
                        joinQueryBuilder.Append($" {attribute.GetDbTableJoinPrefixQuery()} {joinTable.DbTableName}");
                    }
                }
            }

            // BaseTable
            var viewTableAttribute = analyzeClassInfo.Attributes.FirstOrDefault(x => x.Type.FullName == CommonDefine.DbViewTableAttributeFullName);
            if (viewTableAttribute == default || viewTableAttribute.GenericTypes.Length <= 0)
            {
                throw new InvalidOperationException();
            }
            if (!dbTables.ContainsKey(viewTableAttribute.GenericTypes[0].FullNameWithGenerics))
            {
                throw new InvalidOperationException();
            }
            var baseTableClass = dbTables[viewTableAttribute.GenericTypes[0].FullNameWithGenerics];

            // Column
            var dbColumnList = new List<AnalyzeDbColumn>();
            var dbAggregateList = new List<AnalyzeDbColumn>();
            foreach (var viewColumnAttribute in analyzeClassInfo.Attributes.Where(x => x.Type.FullName == CommonDefine.DbViewColumnAttributeFullName))
            {
                var tableType = viewColumnAttribute.GenericTypes.FirstOrDefault();
                if (tableType != default &&
                    viewColumnAttribute.ArgumentObjects.Length == 2 &&
                    viewColumnAttribute.ArgumentObjects[0] is string columnName &&
                    viewColumnAttribute.ArgumentObjects[1] is int aggregateNum)
                {
                    if (dbTables.TryGetValue(tableType.FullNameWithGenerics, out var tableInfo) &&
                        tableInfo.DbColumns.TryGetValue(columnName, out var columnInfo))
                    {
                        dbColumnList.Add(columnInfo);
                        dbAggregateList.AddRange(columnInfo.CreateAggregate(aggregateNum));

                    }
                }
            }
            var dbQueryList = dbColumnList.Concat(dbAggregateList).ToArray();

            // Result
            var result = new AnalyzeDbView(analyzeClassInfo, baseTableClass);
            result.DbTables = dbTables;
            result.DbColumns = 0 < dbColumnList.Count ? dbColumnList.ToArray() : dbTables.SelectMany(x => x.Value.DbColumns.Values).ToArray();
            result.DbQueries = 0 < dbQueryList.Length ? dbQueryList : dbTables.SelectMany(x => x.Value.DbColumns.Values).ToArray();
            result.JoinQuery = joinQueryBuilder.ToString();
            return result;
        }

        public static string GetDbTableJoinPrefixQuery(string attributeName)
        {
            if (attributeName == CommonDefine.DbViewInnerJoinAttributeFullName)
            {
                return $"INNER JOIN";
            }
            else if (attributeName == CommonDefine.DbViewLeftOuterJoinAttributeFullName)
            {
                return $"LEFT OUTER JOIN";
            }
            else if (attributeName == CommonDefine.DbViewRightOuterJoinAttributeFullName)
            {
                return $"RIGHT OUTER JOIN";
            }
            else if (attributeName == CommonDefine.DbViewFullOuterJoinAttributeFullName)
            {
                return $"FULL OUTER JOIN";
            }
            else if (attributeName == CommonDefine.DbViewCrossJoinAttributeFullName)
            {
                return $"CROSS JOIN";
            }
            throw new InvalidOperationException();
        }


    }

    internal class AnalyzeDbTable
    {
        public AnalyzeClassInfo ClassInfo { get; init; }
        public string DbTableName { get; private set; } = string.Empty;
        public Dictionary<string, AnalyzeDbColumn> DbColumns { get; private set; } = new Dictionary<string, AnalyzeDbColumn>();

        public AnalyzeDbTable(AnalyzeClassInfo classInfo)
        {
            ClassInfo = classInfo;
        }

        public static AnalyzeDbTable Analyze(AnalyzeClassInfo analyzeClassInfo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = new AnalyzeDbTable(analyzeClassInfo);
            {
                // DbTableName
                var dbTableAttribute = analyzeClassInfo.Attributes.FirstOrDefault(x => x.Type.FullName == CommonDefine.DbTableAttributeFullName);
                if (dbTableAttribute != default &&
                    0 < dbTableAttribute.ArgumentObjects.Length &&
                    dbTableAttribute.ArgumentObjects[0] is string dbTableString)
                {
                    result.DbTableName = dbTableString;
                }
                // DbColumn
                foreach (var analyzePropertyInfo in analyzeClassInfo.Properties)
                {
                    result.DbColumns.Add(analyzePropertyInfo.Name, AnalyzeDbColumn.Analyze(result, analyzePropertyInfo, cancellationToken));
                }
            }
            return result;
        }
    }
    internal class AnalyzeDbColumn
    {
        public AnalyzeDbTable DbTableInfo { get; init; }
        public AnalyzePropertyInfo PropertyInfo { get; init; }
        public string PropertyType { get; private set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string PropertyDefaultValue { get; set; } = string.Empty;
        public string ConverterType { get; private set; } = string.Empty;

        public string DbColumnName { get; set; } = string.Empty;
        public string DbAggregate { get; private set; } = string.Empty;
        public string DbQuery
            => string.IsNullOrEmpty(DbAggregate) ? DbColumnName : $"{DbAggregate}({DbColumnName})";

        public string InsertDefault { get; private set; } = string.Empty;
        public string UpdateDefault { get; private set; } = string.Empty;

        public AnalyzeDbColumn[] AggregateColumns { get; private set; } = Array.Empty<AnalyzeDbColumn>();

        public AnalyzeDbColumn(AnalyzeDbTable dbTableInfo, AnalyzePropertyInfo analyzePropertyInfo)
        {
            DbTableInfo = dbTableInfo;
            PropertyInfo = analyzePropertyInfo;
        }

        public static AnalyzeDbColumn Analyze(AnalyzeDbTable dbTableInfo, AnalyzePropertyInfo analyzePropertyInfo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = new AnalyzeDbColumn(dbTableInfo, analyzePropertyInfo);
            {
                // Property
                result.PropertyType = analyzePropertyInfo.Type.FullNameWithGenerics;
                result.PropertyName = analyzePropertyInfo.Name;
                result.PropertyDefaultValue = analyzePropertyInfo.HasDefaultValue ? analyzePropertyInfo.DefaultValue : string.Empty;
                // DbColumnName
                var dbColumnAttribute = analyzePropertyInfo.Attributes.FirstOrDefault(x => x.Type.FullName == CommonDefine.DbColumnAttributeFullName);
                if (dbColumnAttribute != default &&
                    0 < dbColumnAttribute.GenericTypes.Length &&
                    0 < dbColumnAttribute.ArgumentObjects.Length &&
                    dbColumnAttribute.ArgumentObjects[0] is string dbColumnString)
                {
                    result.ConverterType = dbColumnAttribute.GenericTypes[0].FullNameWithGenerics;
                    result.DbColumnName = dbColumnString;

                    // Aggregate
                    var aggregateAttribute = analyzePropertyInfo.Attributes.FirstOrDefault(x => x.Type.FullName == CommonDefine.DbAggregateAttributeFullName);
                    if (aggregateAttribute != default &&
                        aggregateAttribute.ArgumentObjects[0] is int aggregateNum)
                    {
                        result.AggregateColumns = result.CreateAggregate(aggregateNum);
                    }
                }
                // AutoCreate
                var autoCreateAttribute = analyzePropertyInfo.Attributes.FirstOrDefault(x => x.Type.FullName == CommonDefine.DbAutoCreateAttributeFullName);
                if (autoCreateAttribute != default &&
                    0 < autoCreateAttribute.GenericTypes.Length &&
                    0 < autoCreateAttribute.ArgumentObjects.Length &&
                    autoCreateAttribute.ArgumentObjects[0] is int autoCreateNum)
                {
                    var autoCreateClass = autoCreateAttribute.GenericTypes[0].FullNameWithGenerics;
                    var autoCreateArgument = autoCreateAttribute.ArgumentStrings.FirstOrDefault();
                    if ((autoCreateNum & CommonDefine.DbAutoCreateValue_Insert) != 0)
                    {
                        result.InsertDefault = $"{autoCreateClass}.CreateInsertValue()";
                    }
                    if ((autoCreateNum & CommonDefine.DbAutoCreateValue_Update) != 0)
                    {
                        result.UpdateDefault = $"{autoCreateClass}.CreateUpdateValue()";
                    }
                }
            }
            return result;
        }


        public AnalyzeDbColumn[] CreateAggregate(int aggregateNum)
        {
            var aggregateList = new List<AnalyzeDbColumn>();
            if ((aggregateNum & CommonDefine.DbAggregateValue_Count) != 0)
            {
                aggregateList.Add(new AnalyzeDbColumn(DbTableInfo, PropertyInfo)
                {
                    PropertyType = "long",
                    PropertyName = $"{PropertyName}Count",
                    DbAggregate = "count",
                    ConverterType = $"{CommonDefine.Namespace}.DbParamLong",
                    DbColumnName = DbColumnName
                });
            }
            if ((aggregateNum & CommonDefine.DbAggregateValue_Avg) != 0)
            {
                aggregateList.Add(new AnalyzeDbColumn(DbTableInfo, PropertyInfo)
                {
                    PropertyType = "double",
                    PropertyName = $"{PropertyName}Avg",
                    DbAggregate = "avg",
                    ConverterType = $"{CommonDefine.Namespace}.DbParamDouble",
                    DbColumnName = DbColumnName
                });
            }
            if ((aggregateNum & CommonDefine.DbAggregateValue_Max) != 0)
            {
                aggregateList.Add(new AnalyzeDbColumn(DbTableInfo, PropertyInfo)
                {
                    PropertyType = PropertyType,
                    PropertyName = $"{PropertyName}Max",
                    DbAggregate = "max",
                    ConverterType = ConverterType,
                    DbColumnName = DbColumnName
                });
            }
            if ((aggregateNum & CommonDefine.DbAggregateValue_Min) != 0)
            {
                aggregateList.Add(new AnalyzeDbColumn(DbTableInfo, PropertyInfo)
                {
                    PropertyType = PropertyType,
                    PropertyName = $"{PropertyName}Min",
                    DbAggregate = "min",
                    ConverterType = ConverterType,
                    DbColumnName = DbColumnName
                });
            }
            return aggregateList.ToArray();
        }
    }
}
