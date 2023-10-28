using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace NpgsqlMappingGenerator
{
    internal class ReportDiagnosticException : Exception
    {
        private readonly DiagnosticDescriptor descriptor;
        private readonly Location location;
        private readonly object?[]? messageArgs;

        public ReportDiagnosticException(
            DiagnosticDescriptor descriptor,
            Location location,
            params object?[]? messageArgs)
            : base(descriptor.Description.ToString())
        {
            this.descriptor = descriptor;
            this.location = location;
            this.messageArgs = messageArgs;
        }

        public ReportDiagnosticException(
            DiagnosticDescriptor descriptor,
            ISymbol symbol,
            params object?[]? messageArgs)
            : this(descriptor, symbol.Locations[0], messageArgs)
        {
        }


        public void Report(SourceProductionContext context)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));
        }
    }
}
