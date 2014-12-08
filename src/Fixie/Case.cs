﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fixie.Discovery;
using Fixie.Execution;

namespace Fixie
{
    public class Case : BehaviorContext
    {
        readonly List<Exception> exceptions;

        public Case(MethodInfo caseMethod, params object[] parameters)
        {
            Parameters = parameters != null && parameters.Length == 0 ? null : parameters;
            Class = caseMethod.ReflectedType;

            Method = caseMethod.IsGenericMethodDefinition
                         ? caseMethod.MakeGenericMethod(GenericArgumentResolver.ResolveTypeArguments(caseMethod, parameters))
                         : caseMethod;

            Name = GetName();
            MethodGroup = GetMethodGroup();

            exceptions = new List<Exception>();
        }

        string GetName()
        {
            var name = GetMethodGroup();

            if (Method.IsGenericMethod)            
                name = string.Format("{0}<{1}>", name, string.Join(", ", Method.GetGenericArguments().Select(x => x.FullName)));

            if (Parameters != null && Parameters.Length > 0)
                name = string.Format("{0}({1})", name, string.Join(", ", Parameters.Select(x => x.ToDisplayString())));

            return name;
        }

        string GetMethodGroup()
        {
            return Class.FullName + "." + Method.Name;
        }

        public string Name { get; private set; }
        public Type Class { get; private set; }
        public MethodInfo Method { get; private set; }
        public string MethodGroup { get; private set; }
        public object[] Parameters { get; private set; }

        public IReadOnlyList<Exception> Exceptions { get { return exceptions; } }

        public void Fail(Exception reason)
        {
            var wrapped = reason as PreservedException;

            if (wrapped != null)
                exceptions.Add(wrapped.OriginalException);
            else
                exceptions.Add(reason);
        }

        public void ClearExceptions()
        {
            exceptions.Clear();
        }

        public Fixture Fixture { get; internal set; }
        internal TimeSpan Duration { get; set; }
        internal string Output { get; set; }
        public object ReturnValue { get; internal set; }
    }
}
