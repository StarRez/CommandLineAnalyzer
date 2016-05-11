# CommandLineAnalyzer

[![Build Status](https://travis-ci.org/StarRez/CommandLineAnalyzer.svg?branch=master)](https://travis-ci.org/StarRez/CommandLineAnalyzer)

Allows running [Roslyn Code Analyzers](https://github.com/dotnet/roslyn-analyzers) from the command line. The intent is for a build process to be able to process analyzers separately to what might be specified in the project itself, either to cut build time on developer machines, or for build processes that don't use MSBuild/Visual Studio.

Usage:

`commandlineanalyzer /project <project file> /analyzer <analyzer dll>`

Multiple projects and/or analyzers can be specified