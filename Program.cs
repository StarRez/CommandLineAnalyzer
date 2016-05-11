using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;

namespace CommandLineAnalyzer
{
	internal class Program
	{
		private static int Main(string[] args)
		{
			if (args.Length == 0 || args.Length % 2 != 0)
			{
				Console.WriteLine("Options: /project <project file>");
				Console.WriteLine("         /analyzer <analyzer dll file>");
			}
			List<string> projects = new List<string>();
			List<string> analyzerList = new List<string>();

			List<string> next = null;
			foreach (string arg in args)
			{
				if (next != null)
				{
					next.Add(arg);
					next = null;
					continue;
				}

				if (arg.Equals("/analyzer", StringComparison.OrdinalIgnoreCase))
				{
					next = analyzerList;
				}
				else if (arg.Equals("/project", StringComparison.OrdinalIgnoreCase))
				{
					next = projects;
				}
				else
				{
					Console.WriteLine("Error: Unknown argument '" + arg + "'");
				}
			}

			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

			// Get all analyzers
			var analyzers = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();

			foreach (string dll in analyzerList)
			{
				Assembly assembly = Assembly.LoadFrom(dll);

				foreach (Type t in assembly.GetTypes())
				{
					if (!t.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(t))
					{
						analyzers.Add((DiagnosticAnalyzer)Activator.CreateInstance(t));
					}
				}
			}

			Console.WriteLine($"Found {analyzers.Count} diagnostics in " + analyzerList.Count + " DLL(s).");

			int errorLevel = 0;

			// Load solution in memory
			MSBuildWorkspace workspace = MSBuildWorkspace.Create();
			foreach (string projectFile in projects)
			{
				Console.WriteLine("Loading " + projectFile);
				Project project = workspace.OpenProjectAsync(projectFile).Result;

				Console.WriteLine($"Analyzing project {project.Name}");

				int count = 0;
				Compilation compilation = project.GetCompilationAsync().Result;
				foreach (DiagnosticAnalyzer analyzer in analyzers)
				{
					Console.WriteLine("...with " + analyzer.GetType().FullName);
					ImmutableArray<Diagnostic> diagnosticResults = compilation.WithAnalyzers(ImmutableArray.Create(analyzer)).GetAnalyzerDiagnosticsAsync().Result;
					Diagnostic[] interestingResults = diagnosticResults.Where(x => x.Severity != DiagnosticSeverity.Hidden).ToArray();

					foreach (Diagnostic diagnostic in interestingResults)
					{
						count++;
						Console.WriteLine(diagnostic.Location.ToString() + ": " + diagnostic.GetMessage());
					}
				}
				Console.WriteLine("Found " + count + " interesting results for " + project.Name);
				if (count == 0)
				{
					Console.WriteLine("Good job!");
				}
				else
				{
					errorLevel = 1;
				}
			}

			Console.WriteLine("End of diagnostics");
			return errorLevel;
		}

		private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			Assembly result = null;

			string shortAssemblyName = args.Name.Split(',')[0];

			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

			foreach (Assembly assembly in assemblies)
			{
				if (shortAssemblyName == assembly.FullName.Split(',')[0])
				{
					result = assembly;
					break;
				}
			}
			return result;
		}
	}
}