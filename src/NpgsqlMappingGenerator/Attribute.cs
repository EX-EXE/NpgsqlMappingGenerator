using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using CodeAnalyzeUtility;
using System.Linq;
using System.Reflection;

namespace NpgsqlMappingGenerator
{
    internal record class DbTableJoinInfo(string DbTableName, string DbColumnName);
    internal record DbColumnInfo(string PropertyType, string PropertyName, string Query, string ConverterType);
    internal record DbAutoCreateInfo(string PropertyName, string InsertFunc, string UpdateFunc);
    internal static class Attribute
    {
        //public static string GetDbTableName(this AnalyzeClassInfo analyzeClassInfo)
        //{
        //    return analyzeClassInfo.Attributes.Where(x => x.Type.FullName == $"{Common.Namespace}.{Common.DbTableAttributeName}").First().ArgumentObjects.OfType<string>().First();
        //}

        public static DbColumnInfo? GetDbColumnInfo(this AnalyzePropertyInfo analyzePropertyInfo,string dbTableName = "")
        {
            var columnAttribute = analyzePropertyInfo.Attributes.FirstOrDefault(x => x.Type.FullName == $"{Common.Namespace}.{Common.DbColumnAttributeName}");
            if (columnAttribute != default)
            {
                var propertyType = analyzePropertyInfo.Type.FullNameWithGenerics;
                var propertyName = analyzePropertyInfo.Name;
                var columnName = columnAttribute.ArgumentObjects.OfType<string>().First();
                var columnFullName = string.IsNullOrEmpty(dbTableName) ? columnName : $"{dbTableName}.{columnName}";
                var converterType = columnAttribute.GenericTypes.First().FullNameWithGenerics;
                return new DbColumnInfo(propertyType, propertyName, columnFullName, converterType);
            }
            return null;
        }
        public static DbAutoCreateInfo? GetDbAutoCreateInfos(this AnalyzePropertyInfo analyzePropertyInfo)
        {
            var insertDefault = string.Empty;
            var updateDefault = string.Empty;
            var autoCreateAttribute = analyzePropertyInfo.Attributes.FirstOrDefault(x => x.Type.FullName == $"{Common.Namespace}.{Common.DbAutoCreateAttribute}");
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
            if (!string.IsNullOrEmpty(insertDefault) || !string.IsNullOrEmpty(updateDefault))
            {
                return new DbAutoCreateInfo(analyzePropertyInfo.Name, insertDefault, updateDefault);
            }
            return null;
        }
        public static DbColumnInfo[] GetDbAggregateInfos(this DbColumnInfo columnInfo, int aggregateNum)
        {
            var result = new List<DbColumnInfo>();
            if ((aggregateNum & Common.DbAggregateValue_Count) != 0)
            {
                var info = new DbColumnInfo("long", $"{columnInfo.PropertyName}Count", $"count({columnInfo.Query})", $"{Common.Namespace}.DbParamLong");
                result.Add(info);
            }
            if ((aggregateNum & Common.DbAggregateValue_Avg) != 0)
            {
                var info = new DbColumnInfo("double", $"{columnInfo.PropertyName}Avg", $"avg({columnInfo.Query})", $"{Common.Namespace}.DbParamDouble");
                result.Add(info);
            }
            if ((aggregateNum & Common.DbAggregateValue_Max) != 0)
            {
                var info = new DbColumnInfo(columnInfo.PropertyType, $"{columnInfo.PropertyName}Max", $"max({columnInfo.Query})", columnInfo.ConverterType);
                result.Add(info);
            }
            if ((aggregateNum & Common.DbAggregateValue_Min) != 0)
            {
                var info = new DbColumnInfo(columnInfo.PropertyType, $"{columnInfo.PropertyName}Min", $"min({columnInfo.Query})", columnInfo.ConverterType);
                result.Add(info);
            }
            return result.ToArray();
        }
        public static DbColumnInfo[] GetDbAggregateInfos(this AnalyzePropertyInfo analyzePropertyInfo, DbColumnInfo columnInfo)
        {
            var aggregateAttribute = analyzePropertyInfo.Attributes.FirstOrDefault(x => x.Type.FullName == $"{Common.Namespace}.{Common.DbAggregateAttributeName}");
            if (aggregateAttribute != default)
            {
                var aggregateArgument = aggregateAttribute.ArgumentObjects.FirstOrDefault();
                if (aggregateArgument is int aggregateNum)
                {
                    return GetDbAggregateInfos(columnInfo, aggregateNum);
                }
            }
            return Array.Empty<DbColumnInfo>();
        }

        public static string GetDbTableName(this AnalyzeClassInfo analyzeClassInfo)
        {
            var dbTableAttribute = analyzeClassInfo.Attributes.First(x => x.Type.FullName == $"{Common.Namespace}.{Common.DbTableAttributeName}");
            return dbTableAttribute.ArgumentObjects.OfType<string>().First();
        }
        public static string GetDbViewTableName(this AnalyzeClassInfo analyzeClassInfo, Dictionary<string, AnalyzeClassInfo> cacheClassInfoDict)
        {
            var viewTableAttribute = analyzeClassInfo.Attributes.First(x => x.Type.FullName == $"{Common.Namespace}.{Common.DbViewTableAttributeName}");
            var viewTableType = viewTableAttribute.GenericTypes.First();
            if (cacheClassInfoDict.TryGetValue(viewTableType.FullNameWithGenerics, out var classInfo))
            {
                return classInfo.GetDbTableName();
            }
            throw new InvalidOperationException();

        }
        public static string GetDbColumnName(this AnalyzePropertyInfo analyzePropertyInfo)
        {
            var dbTableAttribute = analyzePropertyInfo.Attributes.First(x => x.Type.FullName == $"{Common.Namespace}.{Common.DbColumnAttributeName}");
            return dbTableAttribute.ArgumentObjects.OfType<string>().First();
        }

        public static string GetDbTableJoinQuery(this AnalyzeClassInfo analyzeClassInfo, Dictionary<string, AnalyzeClassInfo> cacheClassInfoDict)
        {
            var result = new List<string>();
            foreach (var attribute in analyzeClassInfo.Attributes)
            {
                if (Common.DbViewInnerOrOuterJoinAttributeFullNames.Contains(attribute.Type.FullName))
                {
                    var joins = attribute.GetDbTableJoinInfosFromwInnerOrOuterJoinAttribute(cacheClassInfoDict);
                    if (joins.Length < 2)
                    {
                        throw new InvalidOperationException();
                    }
                    var query = $"{attribute.GetDbTableJoinPrefixQuery()} {joins[0].DbTableName} ON {joins[0].DbTableName}.{joins[0].DbColumnName} = {joins[1].DbTableName}.{joins[1].DbColumnName}";
                    result.Add(query);
                }
                else if (Common.DbViewCrossJoinAttributeFulleName == attribute.Type.FullName)
                {
                    var query = $"{attribute.GetDbTableJoinPrefixQuery()} {attribute.GetDbTableNameFromCrossJoinAttribute(cacheClassInfoDict)}";
                    result.Add(query);
                }
            }
            return string.Join(" ", result);
        }

        public static string GetDbTableNameFromCrossJoinAttribute(this CodeAnalyzeUtility.AnalyzeAttributeInfo analyzeAttributeInfo, Dictionary<string, AnalyzeClassInfo> cacheClassInfoDict)
        {
            if (0 < analyzeAttributeInfo.GenericTypes.Length &&
                cacheClassInfoDict.TryGetValue(analyzeAttributeInfo.GenericTypes[0].FullNameWithGenerics, out var analyzeClassInfo))
            {
                return analyzeClassInfo.GetDbTableName();
            }
            throw new InvalidOperationException();
        }


        public static DbTableJoinInfo[] GetDbTableJoinInfosFromwInnerOrOuterJoinAttribute(this CodeAnalyzeUtility.AnalyzeAttributeInfo analyzeAttributeInfo, Dictionary<string, AnalyzeClassInfo> cacheClassInfoDict)
        {
            var result = new List<DbTableJoinInfo>();
            foreach (var (genericType, argObj) in analyzeAttributeInfo.GenericTypes.Zip(analyzeAttributeInfo.ArgumentObjects, (x, y) => (x, y)))
            {
                if (argObj is string argStr && cacheClassInfoDict.TryGetValue(genericType.FullNameWithGenerics, out var analyzeClassInfo))
                {
                    var property = analyzeClassInfo.Properties.Where(x => x.Name == argStr).First();
                    var table = analyzeClassInfo.GetDbTableName();
                    var column = property.GetDbColumnName();
                    result.Add(new DbTableJoinInfo(table, column));
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            return result.ToArray();
        }
        public static string GetDbTableJoinPrefixQuery(this CodeAnalyzeUtility.AnalyzeAttributeInfo analyzeAttributeInfo)
        {
            if (analyzeAttributeInfo.Type.FullName == $"{Common.Namespace}.{Common.DbViewInnerJoinAttributeName}")
            {
                return $"INNER JOIN";
            }
            else if (analyzeAttributeInfo.Type.FullName == $"{Common.Namespace}.{Common.DbViewLeftOuterJoinAttributeName}")
            {
                return $"LEFT OUTER JOIN";
            }
            else if (analyzeAttributeInfo.Type.FullName == $"{Common.Namespace}.{Common.DbViewRightOuterJoinAttributeName}")
            {
                return $"RIGHT OUTER JOIN";
            }
            else if (analyzeAttributeInfo.Type.FullName == $"{Common.Namespace}.{Common.DbViewFullOuterJoinAttributeName}")
            {
                return $"FULL OUTER JOIN";
            }
            else if (analyzeAttributeInfo.Type.FullName == $"{Common.Namespace}.{Common.DbViewCrossJoinAttributeName}")
            {
                return $"CROSS JOIN";
            }
            throw new InvalidOperationException();
        }
    }
}
