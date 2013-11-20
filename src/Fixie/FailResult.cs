﻿using System;
using System.Collections.Generic;

namespace Fixie
{
    public class FailResult
    {
        public FailResult(CaseResult result)
        {
            Case = result.Case;
            Output = result.Output;
            Exceptions = result.Exceptions;
        }

        public Case Case { get; private set; }
        public string Output { get; private set; }
        public IReadOnlyList<Exception> Exceptions { get; private set; }
    }
}