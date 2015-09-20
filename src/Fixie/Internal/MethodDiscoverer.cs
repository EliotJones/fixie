using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Fixie.Internal
{
    public class MethodDiscoverer
    {
        readonly Func<MethodInfo, bool>[] testMethodConditions;
        readonly BindingFlags discoverableMethodFlags;

        public MethodDiscoverer(Configuration config)
        {
            testMethodConditions = config.TestMethodConditions.ToArray();
            discoverableMethodFlags = config.BindingFlags;
        }

        public IReadOnlyList<MethodInfo> TestMethods(Type testClass)
        {
            try
            {
                return testClass.GetMethods(discoverableMethodFlags).Where(IsMatch).ToArray();
            }
            catch (Exception exception)
            {
                throw new Exception(
                    "Exception thrown while attempting to run a custom method-discovery predicate. " +
                    "Check the inner exception for more details.", exception);
            }
        }

        bool IsMatch(MethodInfo candidate)
        {
            return testMethodConditions.All(condition => condition(candidate));
        }
    }
}