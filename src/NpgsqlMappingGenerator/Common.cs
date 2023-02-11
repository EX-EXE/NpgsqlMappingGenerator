using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NpgsqlMappingGenerator
{
    internal static class Common
    {
        public static readonly string Namespace = "NpgsqlMappingGenerator";

        public static readonly string DbTableAttributeName = $"DbTableGeneratorAttribute";
        public static readonly string DbColumnAttributeName = $"DbColumnAttribute";

        public static readonly string DbAggregateAttributeName = $"DbAggregateAttribute";
        public static readonly string DbAggregateType_Avg = $"Avg";
        public static readonly string DbAggregateType_Count = $"Count";
        public static readonly string DbAggregateType_Max = $"Max";
        public static readonly string DbAggregateType_Min = $"Min";

        public static readonly string DbAutoCreateAttribute = $"DbAutoCreateAttribute";
        public static readonly string DbAutoCreateType_Insert = $"Insert";
        public static readonly string DbAutoCreateType_Update = $"Update";

        public static void GenerateDbBase(IncrementalGeneratorPostInitializationContext context)
        {
            // DbCompareOperator
            context.AddSource($"{Namespace}.DbCompareOperator.cs", $$"""
namespace NpgsqlMappingGenerator;
public enum DbCompareOperator
{
    Equals,
    NotEquals,
    LessThan,
    LessThanEqual,
    GreaterThan,
    GreaterThanOrEqual,
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
            DbCompareOperator.GreaterThanOrEqual => ">=",
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
    {{DbAggregateType_Avg}}     = 1 << 1,
    {{DbAggregateType_Count}}   = 1 << 2,
    {{DbAggregateType_Max}}     = 1 << 3,
    {{DbAggregateType_Min}}     = 1 << 4,
}
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal sealed class {{Common.DbAggregateAttributeName}} : Attribute
{
    public {{Common.DbAggregateAttributeName}}(DbAggregateType types)
    {
    }
}
""");
            // DbAutoCreateAttribute
            context.AddSource($"{Namespace}.{Common.DbAutoCreateAttribute}.cs", $$"""
namespace NpgsqlMappingGenerator;
using System;
[Flags]
public enum DbAutoCreateType
{
    {{DbAutoCreateType_Insert}}     = 1 << 1,
    {{DbAutoCreateType_Update}}   = 1 << 2,
}
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal sealed class {{Common.DbAutoCreateAttribute}}<T> : Attribute
{
    public {{Common.DbAutoCreateAttribute}}(DbAutoCreateType types)
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
