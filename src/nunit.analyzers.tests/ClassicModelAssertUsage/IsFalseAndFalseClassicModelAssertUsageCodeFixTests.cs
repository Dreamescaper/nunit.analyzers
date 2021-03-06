using System.Collections.Immutable;
using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Analyzers.ClassicModelAssertUsage;
using NUnit.Analyzers.Constants;
using NUnit.Framework;

namespace NUnit.Analyzers.Tests.ClassicModelAssertUsage
{
    [TestFixture]
    public sealed class IsFalseAndFalseClassicModelAssertUsageCodeFixTests
    {
        private static readonly DiagnosticAnalyzer analyzer = new ClassicModelAssertUsageAnalyzer();
        private static readonly CodeFixProvider fix = new IsFalseAndFalseClassicModelAssertUsageCodeFix();

        [Test]
        public void VerifyGetFixableDiagnosticIds()
        {
            var fix = new IsFalseAndFalseClassicModelAssertUsageCodeFix();
            var ids = fix.FixableDiagnosticIds.ToImmutableArray();

            Assert.That(ids.Length, Is.EqualTo(2), nameof(ids.Length));
            Assert.That(ids, Contains.Item(AnalyzerIdentifiers.IsFalseUsage), nameof(AnalyzerIdentifiers.IsFalseUsage));
            Assert.That(ids, Contains.Item(AnalyzerIdentifiers.FalseUsage), nameof(AnalyzerIdentifiers.FalseUsage));
        }

        [TestCase("IsFalse", AnalyzerIdentifiers.IsFalseUsage)]
        [TestCase("False", AnalyzerIdentifiers.FalseUsage)]
        public void VerifyIsFalseAndFalseFixes(string assertion, string diagnosticId)
        {
            var expectedDiagnostic = ExpectedDiagnostic.Create(diagnosticId);

            var code = TestUtility.WrapMethodInClassNamespaceAndAddUsings($@"
        public void TestMethod()
        {{
            ↓Assert.{assertion}(false);
        }}");
            var fixedCode = TestUtility.WrapMethodInClassNamespaceAndAddUsings(@"
        public void TestMethod()
        {
            Assert.That(false, Is.False);
        }");
            AnalyzerAssert.CodeFix(analyzer, fix, expectedDiagnostic, code, fixedCode, fixTitle: CodeFixConstants.TransformToConstraintModelDescription);
        }

        [TestCase("IsFalse", AnalyzerIdentifiers.IsFalseUsage)]
        [TestCase("False", AnalyzerIdentifiers.FalseUsage)]
        public void VerifyIsFalseAndFalseFixesWithMessage(string assertion, string diagnosticId)
        {
            var expectedDiagnostic = ExpectedDiagnostic.Create(diagnosticId);

            var code = TestUtility.WrapMethodInClassNamespaceAndAddUsings($@"
        public void TestMethod()
        {{
            ↓Assert.{assertion}(false, ""message"");
        }}");
            var fixedCode = TestUtility.WrapMethodInClassNamespaceAndAddUsings(@"
        public void TestMethod()
        {
            Assert.That(false, Is.False, ""message"");
        }");
            AnalyzerAssert.CodeFix(analyzer, fix, expectedDiagnostic, code, fixedCode, fixTitle: CodeFixConstants.TransformToConstraintModelDescription);
        }

        [TestCase("IsFalse", AnalyzerIdentifiers.IsFalseUsage)]
        [TestCase("False", AnalyzerIdentifiers.FalseUsage)]
        public void VerifyIsFalseAndFalseFixesWithMessageAndParams(string assertion, string diagnosticId)
        {
            var expectedDiagnostic = ExpectedDiagnostic.Create(diagnosticId);

            var code = TestUtility.WrapMethodInClassNamespaceAndAddUsings($@"
        public void TestMethod()
        {{
            ↓Assert.{assertion}(false, ""message"", Guid.NewGuid());
        }}");
            var fixedCode = TestUtility.WrapMethodInClassNamespaceAndAddUsings(@"
        public void TestMethod()
        {
            Assert.That(false, Is.False, ""message"", Guid.NewGuid());
        }");
            AnalyzerAssert.CodeFix(analyzer, fix, expectedDiagnostic, code, fixedCode, fixTitle: CodeFixConstants.TransformToConstraintModelDescription);
        }
    }
}
