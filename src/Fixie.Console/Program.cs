using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Xml.Linq;
using Fixie.Execution;
using Fixie.Reports;

namespace Fixie.ConsoleRunner
{
    public class ExecutionSink : LongLivedMarshalByRefObject, IExecutionSink
    {
        public void SendMessage(string message)
        {
            Console.WriteLine("SENDMESSAGE: " + message);
        }

        public void RecordResult(CaseResult caseResult)
        {
            Console.WriteLine("RECORDRESULT: "+caseResult.Name);
        }
    }

    class Program
    {
        const int FatalError = -1;

        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                var commandLineParser = new CommandLineParser(args);

                if (commandLineParser.HasErrors)
                {
                    using (Foreground.Red)
                        foreach (var error in commandLineParser.Errors)
                            Console.WriteLine(error);

                    Console.WriteLine();
                    Console.WriteLine(CommandLineParser.Usage());
                    return FatalError;
                }

                foreach (var assemblyPath in commandLineParser.AssemblyPaths)
                {
                    if (!File.Exists(assemblyPath))
                    {
                        using (Foreground.Red)
                            Console.WriteLine("Specified test assembly does not exist: " + assemblyPath);

                        Console.WriteLine();
                        Console.WriteLine(CommandLineParser.Usage());
                        return FatalError;
                    }
                }

                var executionResult = new ExecutionResult();

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                foreach (var assemblyPath in commandLineParser.AssemblyPaths)
                {
                    var result = Execute(assemblyPath, commandLineParser.Options);

                    executionResult.Add(result);
                }

                stopwatch.Stop();

                if (executionResult.AssemblyResults.Count > 1)
                    Summarize(executionResult, stopwatch.Elapsed);

                ProduceReports(commandLineParser.Options, executionResult);

                return executionResult.Failed;
            }
            catch (Exception exception)
            {
                using (Foreground.Red)
                    Console.WriteLine("Fatal Error: {0}", exception);
                return FatalError;
            }
        }

        static void Summarize(ExecutionResult executionResult, TimeSpan elapsed)
        {
            var line = new StringBuilder();

            line.AppendFormat("{0} passed", executionResult.Passed);
            line.AppendFormat(", {0} failed", executionResult.Failed);

            if (executionResult.Skipped > 0)
                line.AppendFormat(", {0} skipped", executionResult.Skipped);

            line.AppendFormat(", took {0:N2} seconds", elapsed.TotalSeconds);

            Console.WriteLine("====== " + line + " ======");
        }

        static void ProduceReports(Options options, ExecutionResult executionResult)
        {
            if (options.Contains(CommandLineOption.NUnitXml))
            {
                var report = new NUnitXmlReport();

                var xDocument = report.Transform(executionResult);

                foreach (var fileName in options[CommandLineOption.NUnitXml])
                    xDocument.Save(fileName, SaveOptions.None);
            }

            if (options.Contains(CommandLineOption.XUnitXml))
            {
                var report = new XUnitXmlReport();

                var xDocument = report.Transform(executionResult);

                foreach (var fileName in options[CommandLineOption.XUnitXml])
                    xDocument.Save(fileName, SaveOptions.None);
            }
        }

        static AssemblyResult Execute(string assemblyPath, Options options)
        {
            using (var environment = new ExecutionEnvironment(assemblyPath))
            {
                Console.WriteLine("About to run an assembly: " + assemblyPath);
                using (var executionSink = new ExecutionSink())
                {
                    Console.WriteLine("In the original AppDomain, the arg is known as a " + executionSink.GetType());
                    environment.LoadAssemblyContaining<ListenerFactory>();//even with fancy assembly resolving event, this still needed to happen to resolve chicken/egg problem.
                    return environment.RunAssembly<ListenerFactory>(options, executionSink);//this only works in Fixie.Tests because that legit refrences this assembly.  it doesn't work for Fixie.Smaples which doesnt' reference this, so the type isn't known on both sides even though the interface type is known on both sides.  casting to interface here doesn't work, of course.  moving concretion to main assembly DOES though, which at least confirms the problem.
                    //Is part of the problem that this assembly right here *would* be loaded shortly (for teh ListenerFactory), but by tehn it's already
                    //too late?
                }
            }
        }
    }
}