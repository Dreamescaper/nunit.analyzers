using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Analyzers.Constants;
using NUnit.Analyzers.Extensions;

namespace NUnit.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NonTestPublicMethodAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor descriptor = DiagnosticDescriptorCreator.Create(
            id: AnalyzerIdentifiers.NonTestPublicMethod,
            title: IgnoreCaseUsageAnalyzerConstants.Title,
            messageFormat: IgnoreCaseUsageAnalyzerConstants.Message,
            category: Categories.Assertion,
            defaultSeverity: DiagnosticSeverity.Info,
            description: IgnoreCaseUsageAnalyzerConstants.Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(this.AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is MethodDeclarationSyntax methodDeclaration))
                return;

            var isPublic = methodDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword);

            if (!isPublic || IsTestMethod(methodDeclaration, context.SemanticModel))
                return;

            var siblingMethods = methodDeclaration.Parent.ChildNodes()
                .Where(node => !Equals(methodDeclaration, node))
                .OfType<MethodDeclarationSyntax>();

            var hasTestMethods = siblingMethods.Any(m => IsTestMethod(m, context.SemanticModel));

            if (hasTestMethods)
            {
                context.ReportDiagnostic(Diagnostic.Create(descriptor, methodDeclaration.GetLocation()));
            }
        }

        private static bool IsTestMethod(MethodDeclarationSyntax methodDeclaration, SemanticModel semanticModel)
        {
            return methodDeclaration.AttributeLists
                .SelectMany(attributeList => attributeList.Attributes)
                .Any(attributeSyntax =>
            {
                var attributeType = semanticModel.GetTypeInfo(attributeSyntax).Type;

                if (attributeType == null || attributeType is IErrorTypeSymbol)
                    return false;

                var fullTypeName = attributeType.GetFullMetadataName();

                return fullTypeName == NunitFrameworkConstants.FullNameOfTypeTestAttribute
                    || fullTypeName == NunitFrameworkConstants.FullNameOfTypeTestCaseAttribute;
            });
        }
    }
}
