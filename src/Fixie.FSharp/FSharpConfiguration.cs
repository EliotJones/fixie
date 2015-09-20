namespace Fixie.FSharp
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using Internal;

    public class FSharpConfiguration : Configuration
    {
        readonly List<Func<Type, bool>> testClassConditions; 
        public override BindingFlags BindingFlags => BindingFlags.Public | BindingFlags.Static;
        public override IReadOnlyList<Func<Type, bool>> TestClassConditions => testClassConditions;

        public FSharpConfiguration()
        {
            TestClassFactory = Factory;
            testClassConditions = new List<Func<Type, bool>>
            {
                NonDiscoveryClasses,
                NonCompilerGeneratedClasses
            };
        }

        object Factory(Type type)
        {
            if (type.IsAbstract)
            {
                return null;
            }

            try
            {
                return Activator.CreateInstance(type);
            }
            catch (TargetInvocationException exception)
            {
                throw new PreservedException(exception.InnerException);
            }
        }

        static bool NonDiscoveryClasses(Type type)
        {
            return !type.IsSubclassOf(typeof(Convention)) && !type.IsSubclassOf(typeof(TestAssembly));
        }

        static bool NonCompilerGeneratedClasses(Type type)
        {
            return !type.Has<CompilerGeneratedAttribute>();
        }
    }
}
