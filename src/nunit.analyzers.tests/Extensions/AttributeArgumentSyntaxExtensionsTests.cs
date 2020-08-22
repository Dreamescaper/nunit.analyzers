using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Analyzers.Extensions;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace NUnit.Analyzers.Tests.Extensions
{
    [TestFixture]
    public sealed class AttributeArgumentSyntaxExtensionsTests
    {
        [TestCaseSource(nameof(GetTestData))]
        public async Task CanAssignToTests(string arguments, string methodParameterType,
            string attributeParameterType, Constraint expectedResult)
        {
            var testCode = $@"
using System;

namespace NUnit.Analyzers.Tests.Targets.Extensions
{{
    public sealed class CanAssignToWhenArgumentIsNullAndTargetIsReferenceType
    {{
        [Arguments({arguments})]
        public void Foo({methodParameterType} a) {{ }}

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
        public sealed class ArgumentsAttribute : Attribute
        {{
            public ArgumentsAttribute({attributeParameterType} x) {{ }}
        }}
    }}
}}";
            var values = await AttributeArgumentSyntaxExtensionsTests.GetAttributeSyntaxAsync(testCode);

            Assert.That(values.Syntax.CanAssignTo(values.TypeSymbol, values.Model), expectedResult);
        }

        private static async Task<(AttributeArgumentSyntax Syntax, ITypeSymbol TypeSymbol, SemanticModel Model)> GetAttributeSyntaxAsync(string code)
        {
            var rootAndModel = await TestHelpers.GetRootAndModel(code);

            // It's assumed the code will have one attribute with one argument,
            // along with one method with one parameter
            var attributeArgumentSyntax = rootAndModel.Node.DescendantNodes().OfType<AttributeSyntax>()
                .Single(_ => _.Name.ToFullString() == "Arguments")
                .DescendantNodes().OfType<AttributeArgumentSyntax>().Single();
            var typeSymbol = rootAndModel.Model.GetDeclaredSymbol(
                    rootAndModel.Node.DescendantNodes().OfType<MethodDeclarationSyntax>().Single()).Parameters[0].Type;

            return (attributeArgumentSyntax, typeSymbol, rootAndModel.Model);
        }

        private static IEnumerable<TestCaseData> GetTestData()
        {
            yield return new TestCaseData("null", "object", "object", Is.True).
                SetName("CanAssignToWhenArgumentIsNullAndTargetIsReferenceType");
            yield return new TestCaseData("null", "int?", "object", Is.True).
                SetName("CanAssignToWhenArgumentIsNullAndTargetIsNullableType");
            yield return new TestCaseData("null", "int", "object", Is.False).
                SetName("CanAssignToWhenArgumentIsNullAndTargetIsValueType");
            yield return new TestCaseData("\"x\"", "object", "string", Is.True).
                SetName("CanAssignToWhenArgumentIsNotNullableAndAssignable");
            yield return new TestCaseData("3", "int", "int?", Is.True).
                SetName("CanAssignToWhenArgumentIsNullableAndAssignable");
            yield return new TestCaseData("\"c14564a0-c59e-475d-9ee9-6303f9687c03\"", "Guid", "string", Is.True).
                SetName("CanAssignToWhenParameterIsGuidAndArgumentIsValidString");
            yield return new TestCaseData("\"x\"", "Guid", "string", Is.False).
                SetName("CanAssignToWhenParameterIsGuidAndArgumentIsInvalidString");
            yield return new TestCaseData("3", "short", "int", Is.True).
                SetName("CanAssignToWhenParameterIsInt16AndArgumentIsInt32");
            yield return new TestCaseData("3", "byte", "int", Is.True).
                SetName("CanAssignToWhenParameterIsByteAndArgumentIsInt32");
            yield return new TestCaseData("3", "sbyte", "int", Is.True).
                SetName("CanAssignToWhenParameterIsSByteAndArgumentIsInt32");
            yield return new TestCaseData("3", "double", "int", Is.True).
                SetName("CanAssignToWhenParameterIsDoubleAndArgumentIsInt32");
            yield return new TestCaseData("3d", "decimal", "double", Is.True).
                SetName("CanAssignToWhenParameterIsDecimalAndArgumentIsDouble");
            yield return new TestCaseData("\"3\"", "decimal", "string", Is.True).
                SetName("CanAssignToWhenParameterIsDecimalAndArgumentIsValidString");
            yield return new TestCaseData("\"x\"", "decimal", "string", Is.False).
                SetName("CanAssignToWhenParameterIsDecimalAndArgumentIsInvalidString");
            yield return new TestCaseData("3", "decimal", "int", Is.True).
                SetName("CanAssignToWhenParameterIsDecimalAndArgumentIsInt32");
            yield return new TestCaseData("3", "long?", "int", Is.True).
                SetName("CanAssignToWhenParameterIsNullableInt64AndArgumentIsInt32");
            yield return new TestCaseData("\"1/1/2000\"", "DateTime", "string", Is.True).
                SetName("CanAssignToWhenParameterIsDateTimeAndArgumentIsValidString");
            yield return new TestCaseData("\"x\"", "DateTime", "string", Is.False).
                SetName("CanAssignToWhenParameterIsDateTimeAndArgumentIsInvalidString");
            yield return new TestCaseData("\"00:03:00\"", "TimeSpan", "string", Is.True).
                SetName("CanAssignToWhenParameterIsTimeSpanAndArgumentIsValidString");
            yield return new TestCaseData("\"x\"", "TimeSpan", "string", Is.False).
                SetName("CanAssignToWhenParameterIsTimeSpanAndArgumentIsInvalidString");
            yield return new TestCaseData("\"2019-10-14T21:11:10+00:00\"", "DateTimeOffset", "string", Is.True).
                SetName("CanAssignToWhenParameterIsDateTimeOffsetAndArgumentIsValidString");
            yield return new TestCaseData("\"x\"", "DateTimeOffset", "string", Is.False).
                SetName("CanAssignToWhenParameterIsDateTimeOffsetAndArgumentIsInvalidString");
            yield return new TestCaseData("new[] { \"a\", \"b\", \"c\" }", "string[]", "string[]", Is.True).
                SetName("CanAssignToWhenArgumentIsImplicitlyTypedArrayAndAssignable");
            yield return new TestCaseData("new[] { \"a\", \"b\", \"c\" }", "int[]", "string[]", Is.False).
                SetName("CanAssignToWhenArgumentIsImplicitlyTypedArrayAndNotAssignable");
            yield return new TestCaseData("\"Russian\"", "System.Globalization.CultureInfo", "object", Is.True).
                SetName("CanAssignToWhenParameterIsCultureInfoAndArgumentIsString");
            yield return new TestCaseData("\"NotACulture\"", "System.Globalization.CultureInfo", "object", Is.True).
                SetName("CanAssignToWhenParameterIsCultureInfoAndArgumentIsInvalidString");
            yield return new TestCaseData("\"1\"", "int", "object", Is.True).
                SetName("CanAssignToWhenParameterIsIntegerAndArgumentIsValidIntegerAsString");
            yield return new TestCaseData("\"1a\"", "int", "object", Is.False).
                SetName("CanAssignToWhenParameterIsIntegerAndArgumentIsInValidIntegerAsString");
            yield return new TestCaseData("\"1.2.3.4\"", "Version", "object", Is.True).
                SetName("CanAssignToWhenParameterIsVersionAndArgumentIsValidString");
            yield return new TestCaseData("\"a.b.c.d\"", "Version", "object", Is.True).
                SetName("CanAssignToWhenParameterIsVersionAndArgumentIsInValidString");
        }
    }
}
