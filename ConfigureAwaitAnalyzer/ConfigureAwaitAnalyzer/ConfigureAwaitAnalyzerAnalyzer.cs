using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ConfigureAwaitAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConfigureAwaitAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string cDiagnosticId = "ConfigureAwait";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString cTitle = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString cMessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString cDescription = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string cCategory = "Design";

        private static readonly DiagnosticDescriptor cRule = new DiagnosticDescriptor(cDiagnosticId, cTitle, cMessageFormat, cCategory, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: cDescription);

        private static readonly ImmutableArray<ImmutableArray<string>> cTaskTypes = ImmutableArray.Create(
            ImmutableArray.Create("System", "Threading", "Tasks", "Task"),
            ImmutableArray.Create("System", "Threading", "Tasks", "ValueTask"));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(cRule); } }

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterOperationAction(AnalyzeAwait, OperationKind.Await);
        }

        private void AnalyzeAwait(OperationAnalysisContext context)
        {
            Debug.Assert(context.Operation.Kind == OperationKind.Await, "Only await is analyzed here");
            var awaitOperation = (IAwaitOperation)context.Operation;
            var awaitedOperation = awaitOperation.Operation;
            if (!(awaitedOperation?.Type is INamedTypeSymbol awaitedType))
            {
                return;
            }

            while (!(awaitedType is null))
            {
                if (cTaskTypes.Any(tt => Matches(awaitedType, tt)))
                {
                    // Found an await of an unconfigured task type
                    var diagnostic = Diagnostic.Create(cRule, awaitOperation.Syntax.GetLocation(), awaitedOperation.Type.Name);
                    context.ReportDiagnostic(diagnostic);
                    break;
                }

                awaitedType = awaitedType.BaseType;
            }

            bool Matches(ISymbol symbol, ImmutableArray<string> nameParts)
            {
                for (int i = nameParts.Length - 1; i >= 0; i--)
                {
                    if (symbol is null)
                    {
                        return false;
                    }

                    if (!string.Equals(symbol.Name, nameParts[i], StringComparison.Ordinal))
                    {
                        return false;
                    }

                    symbol = symbol.ContainingNamespace;
                }

                return symbol is INamespaceSymbol ns && ns.IsGlobalNamespace;
            }
        }
    }
}
