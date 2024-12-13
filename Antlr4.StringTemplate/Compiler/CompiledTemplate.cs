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

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using Antlr4.StringTemplate.Misc;

namespace Antlr4.StringTemplate.Compiler;

/** The result of compiling a Template.  Contains all the bytecode instructions,
 *  string table, bytecode address to source code map, and other bookkeeping
 *  info.  It's the implementation of a Template you might say.  All instances
 *  of the same template share a single implementation (impl field).
 */
public class CompiledTemplate {

    /** &lt;@r()&gt;, &lt;@r&gt;...&lt;@end&gt;, and @t.r() ::= "..." defined manually by coder */
    public enum RegionType {
        /// <summary>
        /// The region is defined by &lt;@r()&gt;
        /// </summary>
        Implicit,

        /// <summary>
        /// The region is defined by &lt;@r&gt;...&lt;@end&gt;
        /// </summary>
        Embedded,

        /// <summary>
        /// The region is defined by @t.r ::= "..."
        /// </summary>
        Explicit
    }

    private static readonly ReadOnlyCollection<CompiledTemplate> EmptyImplicitlyDefinedTemplates =
        new (Array.Empty<CompiledTemplate>());

    /**
    Every template knows where it is relative to the group that
    loaded it. The prefix is the relative path from the
    root. "/prefix/name" is the fully qualified name of this
    template. All ST.getInstanceOf() calls must use fully qualified
    names. A "/" is added to the front if you don't specify
    one. Template references within template code, however, uses
    relative names, unless of course the name starts with "/".

    This has nothing to do with the outer filesystem path to the group dir
    or group file.

    We set this as we load/compile the template.

    Always ends with "/".
     */
    private string _prefix = "/";

    private List<FormalArgument> _formalArguments;

    /** A list of all regions and subtemplates */
    private List<CompiledTemplate> implicitlyDefinedTemplates;

    private int _numberOfArgsWithDefaultValues;

    public string[] strings;     // string operands of instructions
    public byte[] instrs = new byte[TemplateCompiler.InitialCodeSize];        // byte-addressable code memory.
    public int codeSize;
    public Interval[] sourceMap = new Interval[TemplateCompiler.InitialCodeSize]; // maps IP to range in template pattern

    public string Name { get; set; }

    public string Prefix {
        get => _prefix;

        set {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            if (!value.EndsWith("/")) {
                throw new ArgumentException("The prefix must end with a trailing '/'.");
            }
            _prefix = value;
        }
    }

    /** The original, immutable pattern (not really used again after
     *  initial "compilation"). Useful for debugging.  Even for
     *  subtemplates, this is entire overall template.
     */
    public string Template { get; set; } = string.Empty;

    /** The token that begins template definition; could be &lt;@r&gt; of region. */
    public IToken TemplateDefStartToken { get; set; }

    /** Overall token stream for template (debug only) */
    public ITokenStream Tokens { get; set; }

    /** How do we interpret syntax of template? (debug only) */
    public CommonTree Ast { get; set; }

    public List<FormalArgument> FormalArguments {
        get => _formalArguments;
        set {
            _formalArguments = value;
            _numberOfArgsWithDefaultValues = _formalArguments?.Count(i => i.DefaultValueToken != null) ?? 0;
        }
    }

    public bool HasFormalArgs { get; set; }

    public ReadOnlyCollection<CompiledTemplate> ImplicitlyDefinedTemplates =>
        implicitlyDefinedTemplates == null ?
            EmptyImplicitlyDefinedTemplates :
            implicitlyDefinedTemplates.AsReadOnly();

    /** The group that physically defines this Template definition.  We use it to initiate
     *  interpretation via Template.ToString().  From there, it becomes field 'group'
     *  in interpreter and is fixed until rendering completes.
     */
    public TemplateGroup NativeGroup { get; set; } = TemplateGroup.DefaultGroup;

    /** Does this template come from a &lt;@region&gt;...&lt;@end&gt; embedded in
     *  another template?
     */
    public bool IsRegion { get; set; }

    /** If someone refs &lt;@r()&gt; in template t, an implicit
     *
     *   @t.r() ::= ""
     *
     *  is defined, but you can overwrite this def by defining your
     *  own.  We need to prevent more than one manual def though.  Between
     *  this var and isEmbeddedRegion we can determine these cases.
     */
    public RegionType RegionDefType { get; set; }

    public bool IsAnonSubtemplate { get; set; }

    public int NumberOfArgsWithDefaultValues => _numberOfArgsWithDefaultValues;

    public FormalArgument TryGetFormalArgument(string name) {
        if (name == null) {
            throw new ArgumentNullException(nameof(name));
        }
        return FormalArguments?.FirstOrDefault(i => i.Name == name);
    }

    /// <summary>
    /// Cloning the <see cref="CompiledTemplate"/> for a <see cref="StringTemplate.Template"/> instance allows
    /// <see cref="StringTemplate.Template.Add"/> to be called safely during interpretation for templates that do
    /// not contain formal arguments.
    /// </summary>
    /// <returns>
    /// A copy of the current <see cref="CompiledTemplate"/> instance. The copy is a shallow copy,
    /// except for the <see cref="_formalArguments"/> field which is also cloned.
    /// </returns>
    public CompiledTemplate Clone() {
        var clone = (CompiledTemplate)MemberwiseClone();
        if (_formalArguments != null) {
            _formalArguments = [.._formalArguments];
        }
        return clone;
    }

    public void AddImplicitlyDefinedTemplate(CompiledTemplate sub) {
        sub.Prefix = Prefix;
        if (sub.Name[0] != '/') {
            sub.Name = sub.Prefix + sub.Name;
        }
        implicitlyDefinedTemplates ??= [];
        implicitlyDefinedTemplates.Add(sub);
    }

    public void DefineArgumentDefaultValueTemplates(TemplateGroup group) {
        if (FormalArguments == null) {
            return;
        }
        foreach (var fa in FormalArguments) {
            if (fa.DefaultValueToken != null) {
                switch (fa.DefaultValueToken.Type) {
                    case GroupParser.ANONYMOUS_TEMPLATE:
                        var argSTname = fa.Name + "_default_value";
                        var c2 = new TemplateCompiler(group);
                        var defArgTemplate = Utility.Strip(fa.DefaultValueToken.Text, 1);
                        fa.CompiledDefaultValue = c2.Compile(group.FileName, argSTname, null, defArgTemplate, fa.DefaultValueToken);
                        fa.CompiledDefaultValue.Name = argSTname;
                        fa.CompiledDefaultValue.DefineImplicitlyDefinedTemplates(group);
                        break;

                    case GroupParser.STRING:
                        fa.DefaultValue = Utility.Strip(fa.DefaultValueToken.Text, 1);
                        break;

                    case GroupParser.LBRACK:
                        fa.DefaultValue = Array.Empty<object>();
                        break;

                    case GroupParser.TRUE:
                    case GroupParser.FALSE:
                        fa.DefaultValue = fa.DefaultValueToken.Type == GroupParser.TRUE;
                        break;

                    default:
                        throw new NotSupportedException("Unexpected default value token type.");
                }
            }
        }
    }

    public void DefineFormalArguments(IEnumerable<FormalArgument> args) {
        HasFormalArgs = true; // even if no args; it's formally defined
        if (args == null) {
            FormalArguments = null;
        } else {
            foreach (var a in args) {
                AddArgument(a);
            }
        }
    }

    /** Used by Template.Add() to Add args one by one w/o turning on full formal args definition signal */
    public void AddArgument(FormalArgument a) {
        FormalArguments ??= [];
        a.Index = FormalArguments.Count;
        FormalArguments.Add(a);
        if (a.DefaultValueToken != null) {
            _numberOfArgsWithDefaultValues++;
        }
    }

    public void DefineImplicitlyDefinedTemplates(TemplateGroup group) {
        if (ImplicitlyDefinedTemplates == null) {
            return;
        }
        foreach (var sub in ImplicitlyDefinedTemplates) {
            group.RawDefineTemplate(sub.Name, sub, sub.TemplateDefStartToken);
            sub.DefineImplicitlyDefinedTemplates(group);
        }
    }

    public string GetInstructions() {
        return BytecodeDisassembler.GetInstructions(this);
    }

    private string Disassemble() {
        using var sw = new StringWriter();
        sw.WriteLine(BytecodeDisassembler.Disassemble(this));
        sw.WriteLine("Strings:");
        sw.WriteLine(BytecodeDisassembler.GetStrings(this));
        sw.WriteLine("Bytecode to template map:");
        sw.WriteLine(BytecodeDisassembler.GetSourceMap(this));
        return sw.ToString();
    }

}
