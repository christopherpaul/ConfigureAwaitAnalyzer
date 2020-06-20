using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace ConfigureAwaitAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConfigureAwaitAnalyzerCodeFixProvider)), Shared]
    public class ConfigureAwaitAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string cTitle = "Add ConfigureAwait(false)";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(ConfigureAwaitAnalyzerAnalyzer.cDiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the await syntax identified by the diagnostic.
            var awaitSyntax = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<AwaitExpressionSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: cTitle,
                    createChangedSolution: c => AddConfigureAwaitAsync(context.Document, awaitSyntax, c),
                    equivalenceKey: cTitle),
                diagnostic);
        }

        private async Task<Solution> AddConfigureAwaitAsync(Document document, AwaitExpressionSyntax awaitSyntax, CancellationToken cancellationToken)
        {
            // Add .ConfigureAwait(false) to the operand of await
            var originalAwaitedExpression = awaitSyntax.Expression;
            var replacementAwaitedExpression =
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        originalAwaitedExpression,
                        SyntaxFactory.IdentifierName("ConfigureAwait")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)))));

            // Replace the original operand with the new one
            var replacementAwaitSyntax = awaitSyntax.WithExpression(replacementAwaitedExpression);

            // Replace the await in the syntax tree
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var updatedRoot = root.ReplaceNode(awaitSyntax, replacementAwaitSyntax);

            // Replace the syntax tree in the document
            var originalSolution = document.Project.Solution;
            var newSolution = originalSolution.WithDocumentSyntaxRoot(document.Id, updatedRoot);

            return newSolution;
        }
    }
}
