using System;
using System.Collections;
using System.IO;
using System.Reflection;

namespace Fixie.Execution
{
    public interface IExecutionSink
    {
        void SendMessage(string message);
        void RecordResult(CaseResult caseResult);
    }

    internal class RemoteAssemblyResolver : MarshalByRefObject, IDisposable
    {
        private IList _directories = (IList)new ArrayList();

        public RemoteAssemblyResolver()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(this.CurrentDomain_AssemblyResolve);
        }

        public override object InitializeLifetimeService()
        {
            return (object)null;
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= new ResolveEventHandler(this.CurrentDomain_AssemblyResolve);
        }

        public void AddDirectory(string directory)
        {
            this._directories.Add((object)directory);
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Console.WriteLine("Resolvign assembly: " + args.Name );
            string name = args.Name;
            int length = name.IndexOf(',');
            if (length == -1)
                return (Assembly)null;
            string str1 = name.Substring(0, length);
            foreach (string path1 in (IEnumerable)this._directories)
            {
                try
                {
                    string str2 = Path.Combine(path1, str1 + ".dll");
                    Console.WriteLine("    considering " + str2);
                    if (File.Exists(str2))
                    {
                        Console.WriteLine(    "exists!");
                        AssemblyName assemblyName = AssemblyName.GetAssemblyName(str2);
                        if (name.ToLower() == assemblyName.FullName.ToLower())
                        {
                            Console.WriteLine("    About to load from this path...");
                            var currentDomainAssemblyResolve = Assembly.LoadFrom(str2);
                            Console.WriteLine("    Done loading from that path.");

                            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                                Console.WriteLine("\t\t" + assembly);
                            return currentDomainAssemblyResolve;
                        }
                    }

                    str2 = Path.Combine(path1, str1 + ".exe");
                    Console.WriteLine("    considering " + str2);
                    if (File.Exists(str2))
                    {
                        Console.WriteLine("exists!");
                        AssemblyName assemblyName = AssemblyName.GetAssemblyName(str2);
                        if (name.ToLower() == assemblyName.FullName.ToLower())
                        {
                            Console.WriteLine("    About to load from this path...");
                            var currentDomainAssemblyResolve = Assembly.LoadFrom(str2);
                            Console.WriteLine("    Done loading from that path.");
                            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                                Console.WriteLine("\t\t" + assembly);
                            return currentDomainAssemblyResolve;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine((string)(object)ex + (object)"");
                }
            }
            return (Assembly)null;
        }
    }
}