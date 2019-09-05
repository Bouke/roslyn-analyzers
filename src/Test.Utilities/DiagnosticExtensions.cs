﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Test.Utilities
{
    public static class DiagnosticExtensions
    {
        private static void OnAnalyzerException(Exception exception, DiagnosticAnalyzer analyzer, Diagnostic diagnostic)
            => Environment.FailFast(exception.ToString(), exception);

        public static ImmutableArray<Diagnostic> GetAnalyzerDiagnostics<TCompilation>(
            this TCompilation c,
            DiagnosticAnalyzer[] analyzers,
            TestValidationMode validationMode,
            AnalyzerOptions options = null)
            where TCompilation : Compilation
        {
            var compilationWithAnalyzerOptions = new CompilationWithAnalyzersOptions(options, OnAnalyzerException, concurrentAnalysis: c.Options.ConcurrentBuild, logAnalyzerExecutionTime: false);
            var compilationWithAnalyzers = c.WithAnalyzers(analyzers.ToImmutableArray(), compilationWithAnalyzerOptions);
            var diagnostics = c.GetDiagnostics();
            if (validationMode != TestValidationMode.AllowCompileErrors)
            {
                CompilationUtils.ValidateNoCompileErrors(diagnostics);
            }

            var diagnosticDescriptors = analyzers.SelectMany(analyzer => analyzer.SupportedDiagnostics);
            var analyzerDiagnosticIds = diagnosticDescriptors.Select(diagnosticDescriptor => diagnosticDescriptor.Id);
            var allDiagnosticIds = new HashSet<string>(analyzerDiagnosticIds, StringComparer.Ordinal)
            {
                "AD0001"    // Failures caught by the Analyzer Driver.
            };
            var allDiagnostics = compilationWithAnalyzers.GetAllDiagnosticsAsync().Result;
            var resultDiagnostics = allDiagnostics.Where(diagnostic => allDiagnosticIds.Contains(diagnostic.Id));
            return resultDiagnostics.ToImmutableArray();
        }
    }
}
