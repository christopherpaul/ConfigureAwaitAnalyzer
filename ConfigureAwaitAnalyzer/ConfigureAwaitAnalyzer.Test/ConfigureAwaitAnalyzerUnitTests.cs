using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using ConfigureAwaitAnalyzer;

namespace ConfigureAwaitAnalyzer.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {

        //No diagnostics expected to show up
        [TestMethod]
        public void EmptyCode_GivesNoDiagnostic()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void ConfiguredAwaitOfTask_GivesNoDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;

namespace N
{
    class C
    {
        async Task M()
        {
            await N().ConfigureAwait(false);
        }

        Task N()
        {
            return Task.CompletedTask;
        }
    }
}
";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void UnconfiguredAwaitOfTask_GivesDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;

namespace N
{
    class C
    {
        async Task M()
        {
            await N();
        }

        Task N()
        {
            return Task.CompletedTask;
        }
    }
}
";

        var expected = new DiagnosticResult
            {
                Id = "ConfigureAwait",
                Message = "Await of Task does not use ConfigureAwait",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 10, 13)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void ConfiguredAwaitOfTaskT_GivesNoDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;

namespace N
{
    class C
    {
        async Task<string> M()
        {
            return await N().ConfigureAwait(false);
        }

        Task<string> N()
        {
            return Task.FromResult(""foo"");
        }
    }
}
";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void UnconfiguredAwaitOfTaskT_GivesDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;

namespace N
{
    class C
    {
        async Task<string> M()
        {
            return await N();
        }

        Task<string> N()
        {
            return Task.FromResult(""foo"");
        }
    }
}
";

            var expected = new DiagnosticResult
            {
                Id = "ConfigureAwait",
                Message = "Await of Task does not use ConfigureAwait",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                        new[] {
                            new DiagnosticResultLocation("Test0.cs", 10, 20)
                            }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void ConfiguredAwaitOfValueTaskT_GivesNoDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;

namespace N
{
    class C
    {
        async Task<string> M()
        {
            return await N().ConfigureAwait(false);
        }

        async ValueTask<string> N()
        {
            return string.Empty;
        }
    }
}
";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void UnconfiguredAwaitOfValueTaskT_GivesDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;

namespace N
{
    class C
    {
        async Task<string> M()
        {
            return await N();
        }

        async ValueTask<string> N()
        {
            return string.Empty;
        }
    }
}
";

            var expected = new DiagnosticResult
            {
                Id = "ConfigureAwait",
                Message = "Await of ValueTask does not use ConfigureAwait",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                        new[] {
                            new DiagnosticResultLocation("Test0.cs", 10, 20)
                            }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ConfigureAwaitAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ConfigureAwaitAnalyzerAnalyzer();
        }
    }
}
