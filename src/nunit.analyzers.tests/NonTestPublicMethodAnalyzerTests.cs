using Gu.Roslyn.Asserts;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Analyzers.Constants;
using NUnit.Framework;

namespace NUnit.Analyzers.Tests
{
    public class NonTestPublicMethodAnalyzerTests
    {
        private static readonly DiagnosticAnalyzer analyzer = new NonTestPublicMethodAnalyzer();
        private static readonly ExpectedDiagnostic expectedDiagnostic =
            ExpectedDiagnostic.Create(AnalyzerIdentifiers.NonTestPublicMethod);

        [Test]
        public void AnalyzeWhenPublicNonTestMethodInTestClass_Test()
        {
            var testCode = TestUtility.WrapClassInNamespaceAndAddUsing(@"
            public class TestClass
            {
                [Test]
                public void TestMethod() { }

                ↓public void NonTestMethod() { }
            }");

            AnalyzerAssert.Diagnostics(analyzer, expectedDiagnostic, testCode);
        }

        [Test]
        public void AnalyzeWhenPublicNonTestMethodInTestClass_TestCase()
        {
            var testCode = TestUtility.WrapClassInNamespaceAndAddUsing(@"
            public class TestClass
            {
                [TestCase(1)]
                [TestCase(2)]
                public void TestMethod(int i) { }

                ↓public void NonTestMethod() { }
            }");

            AnalyzerAssert.Diagnostics(analyzer, expectedDiagnostic, testCode);
        }

        [TestCase("private")]
        [TestCase("protected")]
        public void ValidWhenNonTestMethodIsNotPublic(string accessModifier)
        {
            var testCode = TestUtility.WrapClassInNamespaceAndAddUsing($@"
            public class TestClass
            {{
                [Test]
                public void TestMethod() {{ }}

                {accessModifier} void NonTestMethod() {{ }}
            }}");

            AnalyzerAssert.Valid(analyzer, testCode);
        }

        [Test]
        public void ValidWhenNotInTestMethod()
        {
            var testCode = TestUtility.WrapClassInNamespaceAndAddUsing(@"
            public class TestClass
            {
                public void NonTestMethod1() { }

                public void NonTestMethod2() { }
            }");

            AnalyzerAssert.Valid(analyzer, testCode);
        }
    }
}
