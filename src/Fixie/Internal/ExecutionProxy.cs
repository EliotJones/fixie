using System;
using System.Collections.Generic;
using System.Reflection;
using Fixie.Execution;

namespace Fixie.Internal
{
    public class ExecutionProxy : LongLivedMarshalByRefObject
    {
        public IReadOnlyList<MethodGroup> DiscoverTestMethodGroups(string assemblyFullPath, Options options)
        {
            var assembly = LoadAssembly(assemblyFullPath);

            return new Discoverer(options).DiscoverTestMethodGroups(assembly);
        }

        public AssemblyResult RunAssembly(string assemblyFullPath, string listenerFactoryAssemblyFullPath, string listenerFactoryType, Options options, params object[] factoryArgs)
        {
            Console.WriteLine("Object has crossed the boundary.");

            var listener = CreateListener(listenerFactoryAssemblyFullPath, listenerFactoryType, options, factoryArgs);

            var runner = new Runner(listener, options);

            var assembly = LoadAssembly(assemblyFullPath);

            return runner.RunAssembly(assembly);
        }

        public AssemblyResult RunMethods(string assemblyFullPath, string listenerFactoryAssemblyFullPath, string listenerFactoryType, Options options, MethodGroup[] methodGroups, params object[] factoryArgs)
        {
            var listener = CreateListener(listenerFactoryAssemblyFullPath, listenerFactoryType, options, factoryArgs);

            var runner = new Runner(listener, options);

            var assembly = LoadAssembly(assemblyFullPath);

            return runner.RunMethods(assembly, methodGroups);
        }

        public void LoadFrom(string assemblyFullPath)
        {
            Console.WriteLine("Trying to preemptively load assembly at " + assemblyFullPath);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                Console.WriteLine("\t\t" + assembly);
            Assembly.LoadFile(assemblyFullPath);//tehre are many ways to load.  which one is right for reals?
            Console.WriteLine("After trying to preemptively load assembly at " + assemblyFullPath);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                Console.WriteLine("\t\t" + assembly);
        }

        static Listener CreateListener(string listenerFactoryAssemblyFullPath, string listenerFactoryType, Options options, object[] factoryArgs)
        {
            Console.WriteLine("About to load the factory assembly at " + listenerFactoryAssemblyFullPath);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                Console.WriteLine("\t\t" + assembly);
            var type = Assembly.LoadFrom(listenerFactoryAssemblyFullPath).GetType(listenerFactoryType);
            Console.WriteLine("Done getting the factory type from " + listenerFactoryAssemblyFullPath);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                Console.WriteLine("\t\t" + assembly);

            foreach (var arg in factoryArgs)
            {
                Console.WriteLine("CHILD RECEIVED AS: " + arg.GetType());//*THIS* triggers a lookup with the new resolver!
            }

            var factory = (IListenerFactory)Activator.CreateInstance(type, factoryArgs);

            return factory.Create(options);
        }

        public static Assembly LoadAssembly(string assemblyFullPath)
        {
            return Assembly.Load(AssemblyName.GetAssemblyName(assemblyFullPath));
        }
    }
}