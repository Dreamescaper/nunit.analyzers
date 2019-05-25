using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Analyzers.Constants;
using NUnit.Analyzers.Helpers;

namespace NUnit.Analyzers.MatchingExpectedActualTypes
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MatchingActualExpectedTypesAnalyzer : BaseAssertionAnalyzer
    {
        private static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor(
            AnalyzerIdentifiers.IgnoreCaseUsage,
            IgnoreCaseUsageAnalyzerConstants.Title,
            IgnoreCaseUsageAnalyzerConstants.Message,
            Categories.Usage,
            DiagnosticSeverity.Warning,
            true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor);

        private static readonly List<string> SupportedContstraintMethods = new List<string>
        {

        };

        protected override void AnalyzeAssertInvocation(SyntaxNodeAnalysisContext context,
            InvocationExpressionSyntax invocationSyntax, IMethodSymbol methodSymbol)
        {
            if (!AssertExpressionHelper.TryGetActualAndConstraintExpressions(invocationSyntax,
                out var actualExpression, out var constraintExpression))
            {
                return;
            }

            var actualType = context.SemanticModel.GetTypeInfo(actualExpression).Type;

            if (actualType == null)
                return;

            var expectedArguments = AssertExpressionHelper
                .GetExpectedArguments(constraintExpression, context.SemanticModel)
                .Where(e => SupportedContstraintMethods.Contains(e.constraintMethod.Name));

            foreach (var (expectedArgument, constraintMethod) in expectedArguments)
            {
                var expectedType = context.SemanticModel.GetTypeInfo(actualExpression).Type;

                if (expectedType != null && !this.CanCompareTypes(actualType, expectedType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        descriptor,
                        expectedArgument.GetLocation()));
                }
            }
        }

        private bool CanCompareTypes(ITypeSymbol actualType, ITypeSymbol expectedType)
        {
            return true;
        }
    }
}
