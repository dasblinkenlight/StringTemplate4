﻿/*
 * [The "BSD license"]
 * Copyright (c) 2011 Terence Parr
 * All rights reserved.
 *
 * Conversion to C#:
 * Copyright (c) 2011 Sam Harwell, Tunnel Vision Laboratories, LLC
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. The name of the author may not be used to endorse or promote products
 *    derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

namespace Antlr4.StringTemplate.Misc;

using System.Collections.Generic;
using IEqualityComparer = System.Collections.IEqualityComparer;
using RuntimeHelpers = System.Runtime.CompilerServices.RuntimeHelpers;

public class ObjectReferenceEqualityComparer<T> : IEqualityComparer<T>, IEqualityComparer
    where T : class
{
    private static readonly ObjectReferenceEqualityComparer<T> _default = new ObjectReferenceEqualityComparer<T>();

    public static ObjectReferenceEqualityComparer<T> Default
    {
        get
        {
            return _default;
        }
    }

    public bool Equals(T x, T y)
    {
        return ReferenceEquals(x, y);
    }

    public int GetHashCode(T obj)
    {
        return RuntimeHelpers.GetHashCode(obj);
    }

    bool IEqualityComparer.Equals(object x, object y)
    {
        return ReferenceEquals(x, y);
    }

    int IEqualityComparer.GetHashCode(object obj)
    {
        return RuntimeHelpers.GetHashCode(obj);
    }
}