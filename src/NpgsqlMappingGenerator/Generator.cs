using CodeAnalyzeUtility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;

namespace NpgsqlMappingGenerator;

[Generator(LanguageNames.CSharp)]
public partial class Generator : IIncrementalGenerator
{
    private static readonly string GeneratorNamespace = "NpgsqlMappingGenerator";
    private static readonly string DbTableAttribute = $"{GeneratorNamespace}.DbTableGeneratorAttribute";

    private static readonly string DbParamAttribute = $"{GeneratorNamespace}.DbParamAttribute";


    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // SourceGenerator用のAttribute生成
        context.RegisterPostInitializationOutput(static context =>
        {
            Common.GenerateDbBase(context);
            Common.GenerateDbParam(context);
            DbTable.GenerateAttribute(context);
            DbView.GenerateAttribute(context);
        });

        // DbTable Hook
        var dbTableSource = context.SyntaxProvider.ForAttributeWithMetadataName(
        $"{Common.Namespace}.{Common.DbTableAttributeName}",
        static (node, token) => true,
        static (context, token) => context);
        context.RegisterSourceOutput(dbTableSource, DbTable.GenerateSource);

        // DbView Hook
        var dbViewSource = context.SyntaxProvider.ForAttributeWithMetadataName(
        $"{Common.Namespace}.{Common.DbViewAttributeName}",
        static (node, token) => true,
        static (context, token) => context);
        context.RegisterSourceOutput(dbViewSource, DbView.GenerateSource);
    }
}