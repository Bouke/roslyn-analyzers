﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Analyzers.MetaAnalyzers.CodeFixes
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(BasicPreferIsKindFix))>
    <[Shared]>
    Public NotInheritable Class BasicPreferIsKindFix
        Inherits PreferIsKindFix

        Protected Overrides Function TryGetNodeToFix(root As SyntaxNode, span As TextSpan) As SyntaxNode
            Dim binaryExpression = root.FindNode(span, getInnermostNodeForTie:=True).FirstAncestorOrSelf(Of BinaryExpressionSyntax)()
            Dim invocation = TryCast(binaryExpression.Left, InvocationExpressionSyntax)
            invocation = If(invocation, TryConvertMemberAccessToInvocation(binaryExpression.Left))
            If invocation Is Nothing Then
                Return Nothing
            End If

            Return binaryExpression
        End Function

        Protected Overrides Sub FixDiagnostic(editor As DocumentEditor, nodeToFix As SyntaxNode)
            editor.ReplaceNode(
                nodeToFix,
                Function(nodeToFix2, generator)
                    Dim binaryExpression = DirectCast(nodeToFix2, BinaryExpressionSyntax)
                    Dim invocation = TryCast(binaryExpression.Left, InvocationExpressionSyntax)
                    invocation = If(invocation, TryConvertMemberAccessToInvocation(binaryExpression.Left))

                    Dim newInvocation = invocation _
                        .WithExpression(ConvertKindNameToIsKind(invocation.Expression)) _
                        .AddArgumentListArguments(SyntaxFactory.SimpleArgument(binaryExpression.Right.WithoutTrailingTrivia())) _
                        .WithTrailingTrivia(binaryExpression.Right.GetTrailingTrivia())
                    Dim negate = binaryExpression.OperatorToken.IsKind(SyntaxKind.LessThanGreaterThanToken)
                    If negate Then
                        Return SyntaxFactory.NotExpression(newInvocation.WithoutLeadingTrivia()).WithLeadingTrivia(newInvocation.GetLeadingTrivia())
                    Else
                        Return newInvocation
                    End If
                End Function)
        End Sub

        Private Shared Function TryConvertMemberAccessToInvocation(expression As ExpressionSyntax) As InvocationExpressionSyntax
            Dim memberAccessExpression = TryCast(expression, MemberAccessExpressionSyntax)
            If memberAccessExpression IsNot Nothing Then
                Return SyntaxFactory.InvocationExpression(memberAccessExpression.WithoutTrailingTrivia()) _
                    .WithTrailingTrivia(memberAccessExpression.GetTrailingTrivia())
            Else
                Return Nothing
            End If
        End Function

        Private Shared Function ConvertKindNameToIsKind(expression As ExpressionSyntax) As ExpressionSyntax
            Dim memberAccessExpression = TryCast(expression, MemberAccessExpressionSyntax)
            If memberAccessExpression IsNot Nothing Then
                Return memberAccessExpression.WithName(SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("IsKind")))
            Else
                Return expression
            End If
        End Function
    End Class
End Namespace
