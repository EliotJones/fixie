using Fixie.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Security.Permissions;

namespace Fixie.Execution
{
    public class ExecutionEnvironment : IDisposable
    {
        readonly string assemblyFullPath;
        readonly AppDomain appDomain;
        readonly string previousWorkingDirectory;

        public ExecutionEnvironment(string assemblyPath)
        {
            assemblyFullPath = Path.GetFullPath(assemblyPath);
            appDomain = CreateAppDomain(assemblyFullPath);

            previousWorkingDirectory = Directory.GetCurrentDirectory();
            var assemblyDirectory = Path.GetDirectoryName(assemblyFullPath);
            Directory.SetCurrentDirectory(assemblyDirectory);
        }

        public IReadOnlyList<MethodGroup> DiscoverTestMethodGroups(Options options)
        {
            using (var executionProxy = Create<ExecutionProxy>())
                return executionProxy.DiscoverTestMethodGroups(assemblyFullPath, options);
        }

        public void LoadAssemblyContaining<T>()
        {
            var listenerFactoryAssemblyFullPath = typeof(T).Assembly.Location;

            using (var executionProxy = Create<ExecutionProxy>())
                executionProxy.LoadFrom(listenerFactoryAssemblyFullPath);
        }

        public AssemblyResult RunAssembly<TListenerFactory>(Options options, params object[] factoryArgs) where TListenerFactory : IListenerFactory
        {
            RemoteAssemblyResolver assemblyResolver = (RemoteAssemblyResolver)this.appDomain.CreateInstanceFromAndUnwrap(typeof(RemoteAssemblyResolver).Assembly.CodeBase, typeof(RemoteAssemblyResolver).FullName);
//            string directoryName1 = Path.GetDirectoryName(assemblyFullPath);
//            assemblyResolver.AddDirectory(directoryName1);
            string directoryName2 = Path.GetDirectoryName(new Uri(this.GetType().Assembly.CodeBase).LocalPath);
            assemblyResolver.AddDirectory(directoryName2);

            AssertSafeForAppDomainCommunication(factoryArgs);

            var listenerFactoryAssemblyFullPath = typeof(TListenerFactory).Assembly.Location;
            var listenerFactoryType = typeof(TListenerFactory).FullName;

            using (var executionProxy = Create<ExecutionProxy>())
            {
                Console.WriteLine("Object is about to cross the boundary...");
                return executionProxy.RunAssembly(assemblyFullPath, listenerFactoryAssemblyFullPath, listenerFactoryType, options, factoryArgs);
            }
        }

        public AssemblyResult RunMethods<TListenerFactory>(Options options, MethodGroup[] methodGroups, params object[] factoryArgs) where TListenerFactory : IListenerFactory
        {
            AssertSafeForAppDomainCommunication(factoryArgs);

            var listenerFactoryAssemblyFullPath = typeof(TListenerFactory).Assembly.Location;
            var listenerFactoryType = typeof(TListenerFactory).FullName;

            using (var executionProxy = Create<ExecutionProxy>())
                return executionProxy.RunMethods(assemblyFullPath, listenerFactoryAssemblyFullPath, listenerFactoryType, options, methodGroups, factoryArgs);
        }

        static void AssertSafeForAppDomainCommunication(object[] factoryArgs)
        {
            foreach (var o in factoryArgs)
            {
                if (o == null) continue;
                if (o is LongLivedMarshalByRefObject) continue;
                if (o.GetType().Has<SerializableAttribute>()) continue;

                var type = o.GetType();
                var message = string.Format("Type '{0}' in Assembly '{1}' must either be [Serialiable] or inherit from '{2}'.",
                    type.FullName,
                    type.Assembly,
                    typeof(LongLivedMarshalByRefObject).FullName);
                throw new Exception(message);
            }
        }

        T Create<T>(params object[] args) where T : LongLivedMarshalByRefObject
        {
            return (T)appDomain.CreateInstanceAndUnwrap(typeof(T).Assembly.FullName, typeof(T).FullName, false, 0, null, args, null, null);
        }

        public void Dispose()
        {
            AppDomain.Unload(appDomain);
            Directory.SetCurrentDirectory(previousWorkingDirectory);
        }

        static AppDomain CreateAppDomain(string assemblyFullPath)
        {
            var setup = new AppDomainSetup
            {
                ApplicationBase = Path.GetDirectoryName(assemblyFullPath),
                ApplicationName = Guid.NewGuid().ToString(),
                ConfigurationFile = GetOptionalConfigFullPath(assemblyFullPath)
            };

            return AppDomain.CreateDomain(setup.ApplicationName, null, setup, new PermissionSet(PermissionState.Unrestricted));
        }

        static string GetOptionalConfigFullPath(string assemblyFullPath)
        {
            var configFullPath = assemblyFullPath + ".config";

            return File.Exists(configFullPath) ? configFullPath : null;
        }
    }
}