/*
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
using Antlr4.StringTemplate.Debug;

namespace Antlr4.StringTemplate;

public sealed class TemplateFrame {
    private DebugEvents _debugState;

    public TemplateFrame(Template template, TemplateFrame parent) {
        Template = template;
        Parent = parent;

        StackDepth = (parent != null) ? parent.StackDepth + 1 : 1;

        if (parent is { _debugState.IsEarlyEval: true }) {
            GetDebugState().IsEarlyEval = true;
        }
    }

    public Template Template { get; }

    public TemplateFrame Parent { get; }

    public int StackDepth { get; }

    public int InstructionPointer { get; set; }

    public DebugEvents GetDebugState() {
        _debugState ??= new DebugEvents();
        return _debugState;
    }

    /** If an instance of x is enclosed in a y which is in a z, return
     *  a String of these instance names in order from topmost to lowest;
     *  here that would be "[z y x]".
     */
    public string GetEnclosingInstanceStackString() {
        var names = new string[StackDepth];
        var p = this;
        var i = StackDepth-1;
        while (p != null) {
            names[i--] = p.Template.impl.Name;
            p = p.Parent;
        }
        System.Diagnostics.Debug.Assert(i == -1);
        return string.Join(" ", names);
    }

}
