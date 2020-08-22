using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Analyzers.Constants;
using NUnit.Analyzers.Extensions;

namespace NUnit.Analyzers.TestCaseUsage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TestCaseUsageAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor notEnoughArguments = DiagnosticDescriptorCreator.Create(
            id: AnalyzerIdentifiers.TestCaseNotEnoughArgumentsUsage,
            title: TestCaseUsageAnalyzerConstants.NotEnoughArgumentsTitle,
            messageFormat: TestCaseUsageAnalyzerConstants.NotEnoughArgumentsMessage,
            category: Categories.Structure,
            defaultSeverity: DiagnosticSeverity.Error,
            description: TestCaseUsageAnalyzerConstants.NotEnoughArgumentsDescription);

        private static readonly DiagnosticDescriptor parameterTypeMismatch = DiagnosticDescriptorCreator.Create(
            id: AnalyzerIdentifiers.TestCaseParameterTypeMismatchUsage,
            title: TestCaseUsageAnalyzerConstants.ParameterTypeMismatchTitle,
            messageFormat: TestCaseUsageAnalyzerConstants.ParameterTypeMismatchMessage,
            category: Categories.Structure,
            defaultSeverity: DiagnosticSeverity.Error,
            description: TestCaseUsageAnalyzerConstants.ParameterTypeMismatchDescription);

        private static readonly DiagnosticDescriptor tooManyArguments = DiagnosticDescriptorCreator.Create(
            id: AnalyzerIdentifiers.TestCaseTooManyArgumentsUsage,
            title: TestCaseUsageAnalyzerConstants.TooManyArgumentsTitle,
            messageFormat: TestCaseUsageAnalyzerConstants.TooManyArgumentsMessage,
            category: Categories.Structure,
            defaultSeverity: DiagnosticSeverity.Error,
            description: TestCaseUsageAnalyzerConstants.TooManyArgumentsDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            notEnoughArguments,
            parameterTypeMismatch,
            tooManyArguments);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(TestCaseUsageAnalyzer.AnalyzeAttribute, SyntaxKind.Attribute);
        }

        private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
        {
            var methodNode = context.Node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();

            if (methodNode != null)
            {
                if (!methodNode.ContainsDiagnostics)
                {
                    var testCaseType = context.SemanticModel.Compilation.GetTypeByMetadataName(NunitFrameworkConstants.FullNameOfTypeTestCaseAttribute);
                    if (testCaseType == null)
                        return;

                    var attributeNode = (AttributeSyntax)context.Node;
                    var attributeSymbol = context.SemanticModel.GetSymbolInfo(attributeNode).Symbol;

                    if (testCaseType.ContainingAssembly.Identity == attributeSymbol?.ContainingAssembly.Identity &&
                        attributeSymbol?.ContainingType.Name == NunitFrameworkConstants.NameOfTestCaseAttribute)
                    {
                        context.CancellationToken.ThrowIfCancellationRequested();

                        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodNode);
                        var (methodRequiredParameters, methodOptionalParameters, methodParamsParameters) =
                            methodSymbol.GetParameterCounts();

                        var (attributePositionalArguments, _) = attributeNode.GetArguments();

                        // From NUnit.Framework.TestCaseAttribute.GetParametersForTestCase
                        // Special handling when sole method parameter is an object[].
                        // If more than one argument - or one of a different type - is provided
                        // then the argument is wrapped within a object[], as the only element,
                        // hence the resulting code will always be valid.
                        if (IsSoleParameterAnObjectArray(methodSymbol))
                        {
                            if (attributePositionalArguments.Length > 1 ||
                                (attributePositionalArguments.Length == 1 &&
                                IsExpressionNotAnObjectArray(context, attributePositionalArguments[0].Expression)))
                            {
                                return;
                            }
                        }

                        if (attributePositionalArguments.Length < methodRequiredParameters)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                notEnoughArguments,
                                attributeNode.GetLocation(),
                                methodRequiredParameters,
                                attributePositionalArguments.Length));
                        }
                        else if (methodParamsParameters == 0 &&
                            attributePositionalArguments.Length > methodRequiredParameters + methodOptionalParameters)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                tooManyArguments,
                                attributeNode.GetLocation(),
                                methodRequiredParameters + methodOptionalParameters,
                                attributePositionalArguments.Length));
                        }
                        else
                        {
                            context.CancellationToken.ThrowIfCancellationRequested();
                            TestCaseUsageAnalyzer.AnalyzePositionalArgumentsAndParameters(context,
                                attributePositionalArguments, methodSymbol.Parameters);
                        }
                    }
                }
            }
        }

        private static bool IsSoleParameterAnObjectArray(IMethodSymbol methodSymbol)
        {
            if (methodSymbol.Parameters.Length != 1)
                return false;

            var parameterType = methodSymbol.Parameters[0].Type;
            return IsTypeAnObjectArray(parameterType);
        }

        private static bool IsExpressionNotAnObjectArray(SyntaxNodeAnalysisContext context, ExpressionSyntax expressionSyntax)
        {
            TypeInfo typeInfo = context.SemanticModel.GetTypeInfo(expressionSyntax);
            return !IsTypeAnObjectArray(typeInfo.Type);
        }

        private static bool IsTypeAnObjectArray(ITypeSymbol typeSymbol)
        {
            return typeSymbol.TypeKind == TypeKind.Array &&
                ((IArrayTypeSymbol)typeSymbol).ElementType.SpecialType == SpecialType.System_Object;
        }

        private static (ITypeSymbol type, string name, ITypeSymbol? paramsType) GetParameterType(
            ImmutableArray<IParameterSymbol> methodParameter,
            int position)
        {
            var symbol = position >= methodParameter.Length ?
                methodParameter[methodParameter.Length - 1] : methodParameter[position];

            ITypeSymbol type = symbol.Type;
            ITypeSymbol? paramsType = null;

            if (symbol.IsParams)
            {
                paramsType = ((IArrayTypeSymbol)symbol.Type).ElementType;
            }

            return (type, symbol.Name, paramsType);
        }

        private static void AnalyzePositionalArgumentsAndParameters(SyntaxNodeAnalysisContext context,
            ImmutableArray<AttributeArgumentSyntax> attributePositionalArguments,
            ImmutableArray<IParameterSymbol> methodParameters)
        {
            var model = context.SemanticModel;

            for (var i = 0; i < attributePositionalArguments.Length; i++)
            {
                var attributeArgument = attributePositionalArguments[i];
                var (methodParameterType, methodParameterName, methodParameterParamsType) =
                    TestCaseUsageAnalyzer.GetParameterType(methodParameters, i);

                if (methodParameterType.IsTypeParameterAndDeclaredOnMethod())
                    continue;

                TypeInfo sourceTypeInfo = model.GetTypeInfo(attributeArgument.Expression);
                ITypeSymbol argumentType = sourceTypeInfo.Type;

                var argumentTypeMatchesParameterType = attributeArgument.CanAssignTo(
                    methodParameterType,
                    model,
                    allowImplicitConversion: true,
                    allowEnumToUnderlyingTypeConversion: true);

                if (methodParameterParamsType == null && argumentTypeMatchesParameterType)
                    continue;

                if (methodParameterParamsType != null)
                {
                    var argumentTypeMatchesElementType = attributeArgument.CanAssignTo(
                        methodParameterParamsType,
                        model,
                        allowImplicitConversion: true,
                        allowEnumToUnderlyingTypeConversion: true);

                    if (argumentTypeMatchesElementType ||
                        (argumentTypeMatchesParameterType && (argumentType != null || !methodParameterParamsType.IsValueType)))
                    {
                        continue;
                    }
                }

                context.ReportDiagnostic(Diagnostic.Create(parameterTypeMismatch,
                    attributeArgument.GetLocation(),
                    i,
                    argumentType?.ToDisplayString() ?? "<null>",
                    methodParameterName,
                    methodParameterType));

                context.CancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
}
