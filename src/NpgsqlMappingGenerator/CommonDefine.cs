﻿using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NpgsqlMappingGenerator
{
    internal static class CommonDefine
    {
        //! [Generator Namespace]
        public static readonly string Namespace = "NpgsqlMappingGenerator";

        //! [Generator Hook]
        //! Table
        public static readonly string DbTableAttributeName = "DbTableGeneratorAttribute";
        public static readonly string DbTableAttributeFullName = $"{Namespace}.{DbTableAttributeName}";
        //! View
        public static readonly string DbViewAttributeName = "DbViewGeneratorAttribute";
        public static readonly string DbViewAttributeFullName = $"{Namespace}.{DbViewAttributeName}";


        //! [Table Column]
        public static readonly string DbColumnAttributeName = "DbColumnAttribute";
        public static readonly string DbColumnAttributeFullName = $"{Namespace}.{DbColumnAttributeName}";

        //! [View Table]
        public static readonly string DbViewTableAttributeName = "DbViewTableAttribute";
        public static readonly string DbViewTableAttributeFullName = $"{Namespace}.{DbViewTableAttributeName}";
        //! [View Column]
        public static readonly string DbViewColumnAttributeName = "DbViewColumnAttribute";
        public static readonly string DbViewColumnAttributeFullName = $"{Namespace}.{DbViewColumnAttributeName}";

        //! [View Join]
        //! InnerJoin
        public static readonly string DbViewInnerJoinAttributeName = "DbViewInnerJoinAttribute";
        public static readonly string DbViewInnerJoinAttributeFullName = $"{Namespace}.{DbViewInnerJoinAttributeName}";
        //! LeftOuterJoin
        public static readonly string DbViewLeftOuterJoinAttributeName = "DbViewLeftOuterJoinAttributeName";
        public static readonly string DbViewLeftOuterJoinAttributeFullName = $"{Namespace}.{DbViewLeftOuterJoinAttributeName}";
        //! RightOuterJoin
        public static readonly string DbViewRightOuterJoinAttributeName = "DbViewRightOuterJoinAttributeName";
        public static readonly string DbViewRightOuterJoinAttributeFullName = $"{Namespace}.{DbViewRightOuterJoinAttributeName}";
        //! FullOuterJoin
        public static readonly string DbViewFullOuterJoinAttributeName = "DbViewFullOuterJoinAttributeName";
        public static readonly string DbViewFullOuterJoinAttributeFullName = $"{Namespace}.{DbViewFullOuterJoinAttributeName}";
        //! CrossJoin
        public static readonly string DbViewCrossJoinAttributeName = "DbViewCrossJoinAttributeName";
        public static readonly string DbViewCrossJoinAttributeFullName = $"{Namespace}.{DbViewCrossJoinAttributeName}";
        //! Join ~ ON ~
        public static readonly string[] DbViewJoinAttributeFullNames = new[]
            {
                DbViewInnerJoinAttributeFullName,
                DbViewLeftOuterJoinAttributeFullName ,
                DbViewRightOuterJoinAttributeFullName,
                DbViewFullOuterJoinAttributeFullName ,
                DbViewCrossJoinAttributeFullName ,
            };
        public static readonly string[] DbViewInnerOrOuterJoinAttributeNames = new[]
            {
                DbViewInnerJoinAttributeName,
                DbViewLeftOuterJoinAttributeName ,
                DbViewRightOuterJoinAttributeName,
                DbViewFullOuterJoinAttributeName ,
            };
        public static readonly string[] DbViewInnerOrOuterJoinAttributeFullNames = new[]
            {
                DbViewInnerJoinAttributeFullName,
                DbViewLeftOuterJoinAttributeFullName ,
                DbViewRightOuterJoinAttributeFullName,
                DbViewFullOuterJoinAttributeFullName ,
            };

        //! [Aggregate]
        public static readonly string DbAggregateAttributeName = "DbAggregateAttribute";
        public static readonly string DbAggregateAttributeFullName = $"{Namespace}.{DbAggregateAttributeName}";

        public static readonly string DbAggregateName_None = "None";
        public static readonly string DbAggregateName_Avg = "Avg";
        public static readonly string DbAggregateName_Count = "Count";
        public static readonly string DbAggregateName_Max = "Max";
        public static readonly string DbAggregateName_Min = "Min";
        public static readonly int DbAggregateValue_None = 0;
        public static readonly int DbAggregateValue_Avg = 1 << 0;
        public static readonly int DbAggregateValue_Count = 1 << 1;
        public static readonly int DbAggregateValue_Max = 1 << 2;
        public static readonly int DbAggregateValue_Min = 1 << 3;

        //! [AutoCreate]
        public static readonly string DbAutoCreateAttributeName = "DbAutoCreateAttribute";
        public static readonly string DbAutoCreateAttributeFullName = $"{Namespace}.{DbAutoCreateAttributeName}";
        public static readonly string DbAutoCreateName_None = "None";
        public static readonly string DbAutoCreateName_Insert = "Insert";
        public static readonly string DbAutoCreateName_Update = "Update";
        public static readonly int DbAutoCreateValue_None = 0;
        public static readonly int DbAutoCreateValue_Insert = 1 << 0;
        public static readonly int DbAutoCreateValue_Update = 1 << 1;

        public static void GenerateDbBase(IncrementalGeneratorPostInitializationContext context)
        {
            // DbCompareOperator
            context.AddSource($"{Namespace}.DbCompareOperator.cs", $$"""
namespace NpgsqlMappingGenerator;

/// <summary>
/// Compare Operator
/// </summary>
public enum DbCompareOperator
{
    /// <summary>
    /// Equals(=)
    /// </summary>
    Equals,
    /// <summary>
    /// Not Equals(!=)
    /// </summary>
    NotEquals,
    /// <summary>
    /// Less Than(<)
    /// </summary>
    LessThan,
    /// <summary>
    /// Less Than Equal(<=)
    /// </summary>
    LessThanEqual,
    /// <summary>
    /// Greater Than(>)
    /// </summary>
    GreaterThan,
    /// <summary>
    /// Greater Than Equal(>=)
    /// </summary>
    GreaterThanEqual,

    /// <summary>
    /// LIKE(Pattern Matching)
    /// </summary>
    Like,
    /// <summary>
    /// NotLIKE(Pattern Matching)
    /// </summary>
    NotLike,
    
    /// <summary>
    /// Match Regex(POSIX Regular Expression)
    /// Case Sensitive
    /// </summary>
    MatchRegexSensitive,
    /// <summary>
    /// Match Regex(POSIX Regular Expression)
    /// Case Insensitive
    /// </summary>
    MatchRegexInsensitive,
    /// <summary>
    /// Not Match Regex(POSIX Regular Expression)
    /// Case Sensitive
    /// </summary>
    NotMatchRegexSensitive,
    /// <summary>
    /// Not Match Regex(POSIX Regular Expression)
    /// Case Insensitive
    /// </summary>
    NotMatchRegexInsensitive,
}
public static class DbCompareOperatorExtensins
{
    public static string ToQuery(this DbCompareOperator compareOperator)
        => compareOperator switch
        {
            DbCompareOperator.Equals => "=",
            DbCompareOperator.NotEquals => "!=",
            DbCompareOperator.LessThan => "<",
            DbCompareOperator.LessThanEqual => "<=",
            DbCompareOperator.GreaterThan => ">",
            DbCompareOperator.GreaterThanEqual => ">=",

            DbCompareOperator.Like => "LIKE",
            DbCompareOperator.NotLike => "NOT LIKE",

            DbCompareOperator.MatchRegexSensitive => "~",
            DbCompareOperator.MatchRegexInsensitive => "~*",
            DbCompareOperator.NotMatchRegexSensitive => "!~",
            DbCompareOperator.NotMatchRegexInsensitive => "!~*",
            _ => throw new NotImplementedException(),
        };
}
""");
            // DbLogicOperator
            context.AddSource($"{Namespace}.DbLogicOperator.cs", $$"""
namespace NpgsqlMappingGenerator;
public enum DbLogicOperator
{
    And,
    Or,
}
public static class DbLogicOperatorExtensins
{
    public static string ToQuery(this DbLogicOperator logicOperator)
        => logicOperator switch
        {
            DbLogicOperator.And => "AND",
            DbLogicOperator.Or => "OR",
            _ => throw new NotImplementedException(),
        };
}
""");
            // DbOrder
            context.AddSource($"{Namespace}.DbOrder.cs", $$"""
namespace NpgsqlMappingGenerator;
public enum DbOrderType
{
    Asc,
    Desc,
}
public static class DbOrderTypeExtensins
{
    public static string ToQuery(this DbOrderType orderType)
        => orderType switch
        {
            DbOrderType.Asc => "ASC",
            DbOrderType.Desc => "DESC",
            _ => throw new NotImplementedException(),
        };
}
""");
            // DbAggregate
            context.AddSource($"{Namespace}.DbAggregate.cs", $$"""
namespace NpgsqlMappingGenerator;
using System;
[Flags]
public enum DbAggregateType
{
    {{DbAggregateName_None}}    = {{DbAggregateValue_None}},
    {{DbAggregateName_Avg}}     = {{DbAggregateValue_Avg}},
    {{DbAggregateName_Count}}   = {{DbAggregateValue_Count}},
    {{DbAggregateName_Max}}     = {{DbAggregateValue_Max}},
    {{DbAggregateName_Min}}     = {{DbAggregateValue_Min}},
}
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal sealed class {{CommonDefine.DbAggregateAttributeName}} : Attribute
{
    public {{CommonDefine.DbAggregateAttributeName}}(DbAggregateType types)
    {
    }
}
""");
            // DbAutoCreateAttribute
            context.AddSource($"{Namespace}.{CommonDefine.DbAutoCreateAttributeName}.cs", $$"""
namespace NpgsqlMappingGenerator;
using System;
[Flags]
public enum DbAutoCreateType
{
    {{DbAutoCreateName_None}}   = {{DbAutoCreateValue_None}},
    {{DbAutoCreateName_Insert}} = {{DbAutoCreateValue_Insert}},
    {{DbAutoCreateName_Update}} = {{DbAutoCreateValue_Update}}
}
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal sealed class {{CommonDefine.DbAutoCreateAttributeName}}<T> : Attribute
{
    public {{CommonDefine.DbAutoCreateAttributeName}}(DbAutoCreateType types)
    {
    }
}

public class DbAutoCreateDateTimeNow
{
    public static DateTime CreateInsertValue()
        => DateTime.Now;
    public static DateTime CreateUpdateValue()
        => DateTime.Now;
}
public class DbAutoCreateDateTimeUtcNow
{
    public static DateTime CreateInsertValue()
        => DateTime.UtcNow;
    public static DateTime CreateUpdateValue()
        => DateTime.UtcNow;
}
""");
        }

        private record DbParamTypeInfo(string ClassSuffixName, string TypeName, string ReaderFuncName);
        public static void GenerateDbParam(IncrementalGeneratorPostInitializationContext context)
        {
            foreach (var typeInfo in new[] {
                new DbParamTypeInfo("Boolean", "bool", "GetBoolean") ,
                new DbParamTypeInfo("Byte", "byte", "GetByte") ,
                new DbParamTypeInfo("Char", "char", "GetChar") ,
                new DbParamTypeInfo("Short", "short", "GetInt16") ,
                new DbParamTypeInfo("Integer", "int", "GetInt32") ,
                new DbParamTypeInfo("Long", "long", "GetInt64") ,
                new DbParamTypeInfo("DateTime", "DateTime", "GetDateTime") ,
                new DbParamTypeInfo("Decimal", "decimal", "GetDecimal") ,
                new DbParamTypeInfo("Double", "double", "GetDouble") ,
                new DbParamTypeInfo("Float", "float", "GetFloat") ,
                new DbParamTypeInfo("Guid", "Guid", "GetGuid") ,
                })
            {
                // DbCompareOperator
                context.AddSource($"{Namespace}.DbParam{typeInfo.ClassSuffixName}.cs", $$"""
using Npgsql;
namespace {{Namespace}};
public class DbParamNullable{{typeInfo.ClassSuffixName}}
{
    public static {{typeInfo.TypeName}}? ReadData(NpgsqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.{{typeInfo.ReaderFuncName}}(ordinal); 

    public static NpgsqlParameter CreateParameter(string name, {{typeInfo.TypeName}}? value)
    {
        if(value.HasValue)
        {
            return new NpgsqlParameter<{{typeInfo.TypeName}}>(name, value.Value);
        }
        else
        {
            return new NpgsqlParameter(name, DBNull.Value);
        }
    }
}
public class DbParam{{typeInfo.ClassSuffixName}}
{
    public static {{typeInfo.TypeName}} ReadData(NpgsqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? throw new ArgumentException() : reader.{{typeInfo.ReaderFuncName}}(ordinal); 

    public static NpgsqlParameter CreateParameter(string name, {{typeInfo.TypeName}} value)
    {
        return new NpgsqlParameter<{{typeInfo.TypeName}}>(name, value);
    }
}
""");
            }
            // string
            context.AddSource($"{Namespace}.DbParamString.cs", $$"""
using Npgsql;
namespace {{Namespace}};
public class DbParamNullableString
{
    public static string? ReadData(NpgsqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal); 

    public static NpgsqlParameter CreateParameter(string name, string? value)
    {
        if(!string.IsNullOrEmpty(value))
        {
            return new NpgsqlParameter<string>(name, value);
        }
        else
        {
            return new NpgsqlParameter(name, DBNull.Value);
        }
    }
}
public class DbParamString
{
    public static string ReadData(NpgsqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? throw new ArgumentException() : reader.GetString(ordinal); 

    public static NpgsqlParameter CreateParameter(string name, string value)
    {
        return new NpgsqlParameter<string>(name, value);
    }
}
""");
        }


    }
}
