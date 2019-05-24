using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Analyzers.Constants;
using NUnit.Analyzers.Helpers;

namespace NUnit.Analyzers.ConstActualValueUsage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConstActualValueUsageAnalyzer : BaseAssertionAnalyzer
    {
        private static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor(
            AnalyzerIdentifiers.ConstActualValueUsage,
            ConstActualValueUsageAnalyzerConstants.Title,
            ConstActualValueUsageAnalyzerConstants.Message,
            Categories.Usage,
            DiagnosticSeverity.Warning,
            true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor);

        protected override void AnalyzeAssertInvocation(SyntaxNodeAnalysisContext context,
            InvocationExpressionSyntax invocationSyntax, IMethodSymbol methodSymbol)
        {
            var actualExpression = AssertExpressionHelper.TryGetActualValue(invocationSyntax, methodSymbol);

            if (actualExpression == null)
                return;

            var argumentSymbol = context.SemanticModel.GetSymbolInfo(actualExpression).Symbol;

            if (actualExpression is LiteralExpressionSyntax
                || (argumentSymbol is ILocalSymbol localSymbol && localSymbol.IsConst)
                || (argumentSymbol is IFieldSymbol fieldSymbol && fieldSymbol.IsConst))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    descriptor,
                    actualExpression.GetLocation()));
            }
        }
    }
}
