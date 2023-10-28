using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace NpgsqlMappingGenerator
{
    public static class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor NotFoundAttributeDescriptor = new (
            id: "NMG1000",
            title: "NotFound Attribute",
            messageFormat: "NotFound Attribute.",
            category: CommonDefine.Namespace,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor NotFoundTypeDescriptor = new(
            id: "NMG1001",
            title: "NotFound Type",
            messageFormat: "NotFound Type.",
            category: CommonDefine.Namespace,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor NotFoundParamDescriptor = new(
            id: "NMG1002",
            title: "NotFound Param",
            messageFormat: "NotFound Param.",
            category: CommonDefine.Namespace,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor EmptyParamDescriptor = new(
            id: "NMG2000",
            title: "Empty Param",
            messageFormat: "Empty Param.",
            category: CommonDefine.Namespace,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ErrorParamDescriptor = new(
            id: "NMG3000",
            title: "Error Param",
            messageFormat: "Error Param.",
            category: CommonDefine.Namespace,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}
