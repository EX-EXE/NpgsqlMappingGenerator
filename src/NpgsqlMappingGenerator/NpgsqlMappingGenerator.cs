using CodeAnalyzeUtility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NpgsqlMappingGenerator.Generator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;

namespace NpgsqlMappingGenerator;

[Generator(LanguageNames.CSharp)]
public partial class NpgsqlMappingGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // SourceGenerator Attribute
        context.RegisterPostInitializationOutput(static context =>
        {
            CommonDefine.GenerateDbBase(context);
            CommonDefine.GenerateDbParam(context);
            DbTableGenerator.GenerateAttribute(context);
            DbViewGenerator.GenerateAttribute(context);
        });

        // DbTable Hook
        var dbTableSource = context.SyntaxProvider.ForAttributeWithMetadataName(
        CommonDefine.DbTableAttributeFullName,
        static (node, token) => true,
        static (context, token) => context);
        context.RegisterSourceOutput(dbTableSource, DbTableGenerator.GenerateSource);

        // DbView Hook
        var dbViewSource = context.SyntaxProvider.ForAttributeWithMetadataName(
        CommonDefine.DbViewAttributeFullName,
        static (node, token) => true,
        static (context, token) => context);
        context.RegisterSourceOutput(dbViewSource, DbViewGenerator.GenerateSource);
    }
}