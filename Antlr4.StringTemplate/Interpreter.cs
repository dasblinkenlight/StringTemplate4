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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ArgumentNullException = System.ArgumentNullException;
using Array = System.Array;
using BitConverter = System.BitConverter;
using CultureInfo = System.Globalization.CultureInfo;
using Environment = System.Environment;
using Exception = System.Exception;
using ICollection = System.Collections.ICollection;
using IDictionary = System.Collections.IDictionary;
using IEnumerable = System.Collections.IEnumerable;
using IEnumerator = System.Collections.IEnumerator;
using IList = System.Collections.IList;
using IOException = System.IO.IOException;
using Math = System.Math;
using StringBuilder = System.Text.StringBuilder;
using StringWriter = System.IO.StringWriter;

namespace Antlr4.StringTemplate;

using Compiler;
using Debug;
using Extensions;
using Misc;

/** This class knows how to execute template bytecodes relative to a
 *  particular TemplateGroup. To execute the byte codes, we need an output stream
 *  and a reference to a Template an instance. That instance's impl field points at
 *  a CompiledTemplate, which contains all the byte codes and other information
 *  relevant to execution.
 *
 *  This interpreter is a stack-based bytecode interpreter.  All operands
 *  go onto an operand stack.
 *
 *  If the group that we're executing relative to has debug set, we track
 *  interpreter events. For now, I am only tracking instance creation events.
 *  These are used by STViz to pair up output chunks with the template
 *  expressions that generate them.
 *
 *  We create a new interpreter for each Template.Render(), DebugTemplate.Visualize, or
 *  DebugTemplate.GetEvents() invocation.
 */
public sealed class Interpreter {

    private readonly ILogger<Interpreter> _logger = NullLogger<Interpreter>.Instance;

    private const int DefaultOperandStackSize = 512;

    private static readonly string[] predefinedAnonSubtemplateAttributes = ["i", "i0"];

    /** Exec st with respect to this group. Once set in Template.ToString(),
     *  it should be fixed. Template has group also.
     */
    private readonly TemplateGroup group;

    /** For renderers, we have to pass in the culture */
    private readonly CultureInfo culture;

    private readonly ErrorManager _errorManager;

    /** Operand stack, grows upwards */
    private object[] operands = new object[DefaultOperandStackSize];
    private int sp = -1;      // stack pointer register
    private int nwline;       // how many char written on this template LINE so far?

    /** Track events inside templates and in this.events */
    private bool _debug;

    /** Track everything happening in interp if debug across all templates.
     *  The last event in this field is the EvalTemplateEvent for the root
     *  template.
     */
    private List<InterpEvent> events;

    public Interpreter(TemplateGroup group, bool debug)
    : this(group, CultureInfo.CurrentCulture, group.ErrorManager, debug)
    {
    }

    public Interpreter(TemplateGroup group, CultureInfo culture, bool debug)
    : this(group, culture, group.ErrorManager, debug)
    {
    }

    public Interpreter(TemplateGroup group, ErrorManager errorManager, bool debug)
    : this(group, CultureInfo.CurrentCulture, errorManager, debug)
    {
    }

    public Interpreter(TemplateGroup group, CultureInfo culture, ErrorManager errorManager, bool debug) {
        this.group = group;
        this.culture = culture;
        _errorManager = errorManager ?? throw new ArgumentNullException(nameof(errorManager));
        _debug = debug;
        if (debug) {
            events = [];
        }
    }

    public static ReadOnlyCollection<string> PredefinedAnonymousSubtemplateAttributes =>
        new(predefinedAnonSubtemplateAttributes);

    /** Execute template self and return how many characters it wrote to out */
    public int Execute(ITemplateWriter @out, TemplateFrame frame) {
        try {
            if (frame.StackDepth > 200) {
                throw new TemplateException("Template stack overflow.", null);
            }
            _logger.LogTrace("Execute({Name})", frame.Template.Name);
            SetDefaultArguments(frame);
            return ExecuteImpl(@out, frame);
        } catch (Exception e) when (!e.IsCritical()) {
            var builder = new StringBuilder();
            builder.AppendLine(e.ToString());
            builder.AppendLine(e.StackTrace);
            _errorManager.RuntimeError(frame, ErrorType.INTERNAL_ERROR, "internal error: " + builder);
            return 0;
        }
    }

    private int ExecuteImpl(ITemplateWriter @out, TemplateFrame frame) {
        var self = frame.Template;
        var start = @out.Index; // track char we're about to Write
        var prevOpcode = Bytecode.Invalid;
        var n = 0; // how many char we Write out
        var code = self.impl.instrs;        // which code block are we executing
        var ip = 0;
        while (ip < self.impl.codeSize) {
            if (_debug) {
                Trace(frame, ip);
            }
            var opcode = (Bytecode)code[ip];
            frame.InstructionPointer = ip;
            ip++; //jump to next instruction or first byte of operand
            int nArgs;
            int nameIndex;
            string name;
            Template st;
            object[] options;
            object left;
            object right;
            object o;
            switch (opcode) {
                case Bytecode.INSTR_LOAD_STR:
                    var strIndex = GetShort(code, ip);
                    ip += Instruction.OperandSizeInBytes;
                    operands[++sp] = self.impl.strings[strIndex];
                    break;

                case Bytecode.INSTR_LOAD_ATTR:
                    nameIndex = GetShort(code, ip);
                    ip += Instruction.OperandSizeInBytes;
                    name = self.impl.strings[nameIndex];
                    try
                    {
                        o = GetAttribute(frame, name);
                        if (o == Template.EmptyAttribute)
                            o = null;
                    }
                    catch (AttributeNotFoundException)
                    {
                        _errorManager.RuntimeError(frame, ErrorType.NO_SUCH_ATTRIBUTE, name);
                        o = null;
                    }
                    operands[++sp] = o;
                    break;

                case Bytecode.INSTR_LOAD_LOCAL:
                    var valueIndex = GetShort(code, ip);
                    ip += Instruction.OperandSizeInBytes;
                    o = self.locals[valueIndex];
                    if (o == Template.EmptyAttribute)
                        o = null;
                    operands[++sp] = o;
                    break;

                case Bytecode.INSTR_LOAD_PROP:
                    nameIndex = GetShort(code, ip);
                    ip += Instruction.OperandSizeInBytes;
                    o = operands[sp--];
                    name = self.impl.strings[nameIndex];
                    operands[++sp] = GetObjectProperty(frame, o, name);
                    break;

                case Bytecode.INSTR_LOAD_PROP_IND:
                    var propName = operands[sp--];
                    o = operands[sp];
                    operands[sp] = GetObjectProperty(frame, o, propName);
                    break;

                case Bytecode.INSTR_NEW:
                    nameIndex = GetShort(code, ip);
                    ip += Instruction.OperandSizeInBytes;
                    name = self.impl.strings[nameIndex];
                    nArgs = GetShort(code, ip);
                    ip += Instruction.OperandSizeInBytes;
                    // look up in original hierarchy not enclosing template (variable group)
                    // see TestSubtemplates.testEvalSTFromAnotherGroup()
                    st = (self.Group as TemplateGroup).GetEmbeddedInstanceOf(frame, name);
                    // get n args and store into st's attr list
                    StoreArguments(frame, nArgs, st);
                    sp -= nArgs;
                    operands[++sp] = st;
                    break;

                case Bytecode.INSTR_NEW_IND:
                    nArgs = GetShort(code, ip);
                    ip += Instruction.OperandSizeInBytes;
                    name = (string)operands[sp - nArgs];
                    st = (self.Group as TemplateGroup).GetEmbeddedInstanceOf(frame, name);
                    StoreArguments(frame, nArgs, st);
                    sp -= nArgs;
                    sp--; // pop template name
                    operands[++sp] = st;
                    break;

                case Bytecode.INSTR_NEW_BOX_ARGS:
                    nameIndex = GetShort(code, ip);
                    ip += Instruction.OperandSizeInBytes;
                    name = self.impl.strings[nameIndex];
                    var attrs = (IDictionary<string, object>)operands[sp--];
                    // look up in original hierarchy not enclosing template (variable group)
                    // see TestSubtemplates.testEvalSTFromAnotherGroup()
                    st = (self.Group as TemplateGroup).GetEmbeddedInstanceOf(frame, name);
                    // get n args and store into st's attr list
                    StoreArguments(frame, attrs, st);
                    operands[++sp] = st;
                    break;

                case Bytecode.INSTR_SUPER_NEW:
                    nameIndex = GetShort(code, ip);
                    ip += Instruction.OperandSizeInBytes;
                    name = self.impl.strings[nameIndex];
                    nArgs = GetShort(code, ip);
                    ip += Instruction.OperandSizeInBytes;
                    SuperNew(frame, name, nArgs);
                    break;

                case Bytecode.INSTR_SUPER_NEW_BOX_ARGS:
                    nameIndex = GetShort(code, ip);
                    ip += Instruction.OperandSizeInBytes;
                    name = self.impl.strings[nameIndex];
                    attrs = (IDictionary<string, object>)operands[sp--];
                    SuperNew(frame, name, attrs);
                    break;

                case Bytecode.INSTR_STORE_OPTION:
                    var optionIndex = GetShort(code, ip);
                    ip += Instruction.OperandSizeInBytes;
                    o = operands[sp--];    // value to store
                    options = (object[])operands[sp]; // get options
                    options[optionIndex] = o; // store value into options on stack
                    break;

                case Bytecode.INSTR_STORE_ARG:
                    nameIndex = GetShort(code, ip);
                    name = self.impl.strings[nameIndex];
                    ip += Instruction.OperandSizeInBytes;
                    o = operands[sp--];
                    attrs = (IDictionary<string, object>)operands[sp];
                    attrs[name] = o; // leave attrs on stack
                    break;

                case Bytecode.INSTR_WRITE:
                    o = operands[sp--];
                    var n1 = WriteObjectNoOptions(@out, frame, o);
                    n += n1;
                    nwline += n1;
                    break;

                case Bytecode.INSTR_WRITE_OPT:
                    options = (object[])operands[sp--]; // get options
                    o = operands[sp--];                 // get option to Write
                    var n2 = WriteObjectWithOptions(@out, frame, o, options);
                    n += n2;
                    nwline += n2;
                    break;

                case Bytecode.INSTR_MAP:
                    st = (Template)operands[sp--]; // get prototype off stack
                    o = operands[sp--];		 // get object to map prototype across
                    Map(frame, o, st);
                    break;

                case Bytecode.INSTR_ROT_MAP:
                    var nMaps = GetShort(code, ip);
                    ip += Instruction.OperandSizeInBytes;
                    var templates = new List<Template>();
                    for (var i = nMaps - 1; i >= 0; i--)
                        templates.Add((Template)operands[sp - i]);
                    sp -= nMaps;
                    o = operands[sp--];
                    if (o != null)
                        RotateMap(frame, o, templates);
                    break;

                case Bytecode.INSTR_ZIP_MAP:
                    st = (Template)operands[sp--];
                    nMaps = GetShort(code, ip);
                    ip += Instruction.OperandSizeInBytes;
                    var exprs = new List<object>();
                    for (var i = nMaps - 1; i >= 0; i--)
                        exprs.Add(operands[sp - i]);

                    sp -= nMaps;
                    operands[++sp] = ZipMap(frame, exprs, st);
                    break;

                case Bytecode.INSTR_BR:
                    ip = GetShort(code, ip);
                    break;

                case Bytecode.INSTR_BRF:
                    var addr = GetShort(code, ip);
                    ip += Instruction.OperandSizeInBytes;
                    o = operands[sp--]; // <if(expr)>...<endif>
                    if (!TestAttributeTrue(o))
                        ip = addr; // jump

                    break;

                case Bytecode.INSTR_OPTIONS:
                    operands[++sp] = new object[TemplateCompiler.NUM_OPTIONS];
                    break;

                case Bytecode.INSTR_ARGS:
                    operands[++sp] = new Dictionary<string, object>();
                    break;

                case Bytecode.INSTR_PASSTHRU:
                    nameIndex = GetShort(code, ip);
                    ip += Instruction.OperandSizeInBytes;
                    name = self.impl.strings[nameIndex];
                    attrs = (IDictionary<string, object>)operands[sp];
                    PassThrough(frame, name, attrs);
                    break;

                case Bytecode.INSTR_LIST:
                    operands[++sp] = new List<object>();
                    break;

                case Bytecode.INSTR_ADD:
                    o = operands[sp--];             // pop value
                    var list = (List<object>)operands[sp]; // don't pop list
                    AddToList(list, frame, o);
                    break;

                case Bytecode.INSTR_TOSTR:
                    // replace with string value; early eval
                    operands[sp] = ToString(frame, operands[sp]);
                    break;

                case Bytecode.INSTR_FIRST:
                    operands[sp] = First(frame, operands[sp]);
                    break;

                case Bytecode.INSTR_LAST:
                    operands[sp] = Last(frame, operands[sp]);
                    break;

                case Bytecode.INSTR_REST:
                    operands[sp] = Rest(frame, operands[sp]);
                    break;

                case Bytecode.INSTR_TRUNC:
                    operands[sp] = Trunc(frame, operands[sp]);
                    break;

                case Bytecode.INSTR_STRIP:
                    operands[sp] = Strip(frame, operands[sp]);
                    break;

                case Bytecode.INSTR_TRIM:
                    o = operands[sp--];
                    if (o is string s1) {
                        operands[++sp] = s1.Trim();
                    } else {
                        _errorManager.RuntimeError(frame, ErrorType.EXPECTING_STRING, "trim", o.GetType());
                        operands[++sp] = o;
                    }
                    break;

                case Bytecode.INSTR_LENGTH:
                    operands[sp] = Length(operands[sp]);
                    break;

                case Bytecode.INSTR_STRLEN:
                    o = operands[sp--];
                    if (o is string s2) {
                        operands[++sp] = s2.Length;
                    } else {
                        _errorManager.RuntimeError(frame, ErrorType.EXPECTING_STRING, "strlen", o.GetType());
                        operands[++sp] = 0;
                    }
                    break;

                case Bytecode.INSTR_REVERSE:
                    operands[sp] = Reverse(frame, operands[sp]);
                    break;

                case Bytecode.INSTR_NOT:
                    operands[sp] = !TestAttributeTrue(operands[sp]);
                    break;

                case Bytecode.INSTR_OR:
                    right = operands[sp--];
                    left = operands[sp--];
                    operands[++sp] = TestAttributeTrue(left) || TestAttributeTrue(right);
                    break;

                case Bytecode.INSTR_AND:
                    right = operands[sp--];
                    left = operands[sp--];
                    operands[++sp] = TestAttributeTrue(left) && TestAttributeTrue(right);
                    break;

                case Bytecode.INSTR_INDENT:
                    strIndex = GetShort(code, ip);
                    ip += Instruction.OperandSizeInBytes;
                    Indent(@out, frame, strIndex);
                    break;

                case Bytecode.INSTR_DEDENT:
                    @out.PopIndentation();
                    break;

                case Bytecode.INSTR_NEWLINE:
                    try
                    {
                        if (prevOpcode == Bytecode.INSTR_NEWLINE ||
                            prevOpcode == Bytecode.INSTR_INDENT ||
                            nwline > 0) {
                            @out.Write(Environment.NewLine);
                        }
                        nwline = 0;
                    } catch (IOException ioe) {
                        _errorManager.IOError(self, ErrorType.WRITE_IO_ERROR, ioe);
                    }
                    break;

                case Bytecode.INSTR_NOOP:
                    break;

                case Bytecode.INSTR_POP:
                    sp--; // throw away top of stack
                    break;

                case Bytecode.INSTR_NULL:
                    operands[++sp] = null;
                    break;

                case Bytecode.INSTR_TRUE:
                    operands[++sp] = true;
                    break;

                case Bytecode.INSTR_FALSE:
                    operands[++sp] = false;
                    break;

                case Bytecode.INSTR_WRITE_STR:
                    strIndex = GetShort(code, ip);
                    ip += Instruction.OperandSizeInBytes;
                    o = self.impl.strings[strIndex];
                    n1 = WriteObjectNoOptions(@out, frame, o);
                    n += n1;
                    nwline += n1;
                    break;

                // TODO: generate this optimization
                //case Bytecode.INSTR_WRITE_LOCAL:
                //    valueIndex = GetShort(code, ip);
                //    ip += Instruction.OperandSizeInBytes;
                //    o = self.locals[valueIndex];
                //    if (o == Template.EmptyAttribute)
                //        o = null;

                //    n1 = WriteObjectNoOptions(@out, frame, o);
                //    n += n1;
                //    nwline += n1;
                //    break;

                case Bytecode.Invalid:
                case Bytecode.INSTR_WRITE_LOCAL:
                default:
                    _errorManager.InternalError(self, "invalid bytecode @ " + (ip - 1) + ": " + opcode, null);
                    _logger.LogError("{CompiledTemplate}", self.impl);
                    break;
            }

            prevOpcode = opcode;
        }

        if (_debug) {
            var e = new EvalTemplateEvent(frame, Interval.FromBounds(start, @out.Index));
            TrackDebugEvent(frame, e);
        }
        return n;
    }

    // TODO: refactor to Remove duplicated code

    private void SuperNew(TemplateFrame frame, string name, int nArgs) {
        var self = frame.Template;
        Template st;
        var imported = self.impl.NativeGroup.LookupImportedTemplate(name);
        if (imported == null) {
            _errorManager.RuntimeError(frame, ErrorType.NO_IMPORTED_TEMPLATE, name);
            st = (self.Group as TemplateGroup)!.CreateStringTemplateInternally(new CompiledTemplate());
        } else {
            st = imported.NativeGroup.GetEmbeddedInstanceOf(frame, name);
            st.Group = group;
        }
        // get n args and store into st's attr list
        StoreArguments(frame, nArgs, st);
        sp -= nArgs;
        operands[++sp] = st;
    }

    private void SuperNew(TemplateFrame frame, string name, IDictionary<string, object> attrs) {
        var self = frame.Template;
        Template st;
        var imported = self.impl.NativeGroup.LookupImportedTemplate(name);
        if (imported == null) {
            _errorManager.RuntimeError(frame, ErrorType.NO_IMPORTED_TEMPLATE, name);
            st = (self.Group as TemplateGroup)!.CreateStringTemplateInternally(new CompiledTemplate());
        } else {
            st = imported.NativeGroup.CreateStringTemplateInternally(imported);
            st.Group = group;
        }
        // get n args and store into st's attr list
        StoreArguments(frame, attrs, st);
        operands[++sp] = st;
    }

    private void PassThrough(TemplateFrame frame, string templateName, IDictionary<string, object> attrs) {
        var c = group.LookupTemplate(templateName);
        if (c?.FormalArguments == null) {
            return;
        }
        foreach (var arg in c.FormalArguments.Where(arg => !attrs.ContainsKey(arg.Name))) {
            //System.out.println("arg "+arg.name+" missing");
            try {
                var o = GetAttribute(frame, arg.Name);
                // If the attribute exists but there is no value and
                // the formal argument has no default value, make it null.
                if (o == Template.EmptyAttribute && arg.DefaultValueToken == null) {
                    attrs[arg.Name] = null;
                } else if (o != Template.EmptyAttribute) {
                    // Else, the attribute has an existing value, set arg.
                    attrs[arg.Name] = o;
                }
            } catch (AttributeNotFoundException) {
                // if no such attribute exists for arg.name, set parameter
                // if no default value
                if (arg.DefaultValueToken == null) {
                    _errorManager.RuntimeError(frame, ErrorType.NO_SUCH_ATTRIBUTE_PASS_THROUGH, arg.Name);
                    attrs[arg.Name] = null;
                }
            }
        }
    }

    private void StoreArguments(TemplateFrame frame, IDictionary<string, object> attrs, Template st) {
        var noSuchAttributeReported = false;
        if (attrs != null) {
            foreach (var argument in attrs) {
                if (!st.impl.HasFormalArgs) {
                    if (st.impl.FormalArguments == null || st.impl.TryGetFormalArgument(argument.Key) == null) {
                        // we clone the CompiledTemplate to prevent modifying the original
                        // _formalArguments map during interpretation.
                        st.impl = st.impl.Clone();
                        st.Add(argument.Key, argument.Value);
                    } else {
                        st.RawSetAttribute(argument.Key, argument.Value);
                    }
                } else {
                    // don't let it throw an exception in RawSetAttribute
                    if (st.impl.FormalArguments == null || st.impl.TryGetFormalArgument(argument.Key) == null) {
                        noSuchAttributeReported = true;
                        _errorManager.RuntimeError(
                            frame,
                            ErrorType.NO_SUCH_ATTRIBUTE,
                            argument.Key);
                        continue;
                    }

                    st.RawSetAttribute(argument.Key, argument.Value);
                }
            }
        }

        if (st.impl.HasFormalArgs) {
            var argumentCountMismatch = false;
            var formalArguments = st.impl.FormalArguments ?? [];

            // first make sure that all non-default arguments are specified
            // ignore this check if a NO_SUCH_ATTRIBUTE error already occurred
            if (!noSuchAttributeReported) {
                foreach (var formalArgument in formalArguments) {
                    if (formalArgument.DefaultValueToken != null || formalArgument.DefaultValue != null) {
                        // this argument has a default value, so it doesn't need to appear in attrs
                        continue;
                    }

                    if (attrs == null || !attrs.ContainsKey(formalArgument.Name)) {
                        argumentCountMismatch = true;
                        break;
                    }
                }
            }

            // next make sure there aren't too many arguments. note that the names
            // of arguments are checked below as they are applied to the template
            // instance, so there's no need to do that here.
            if (attrs != null && attrs.Count > formalArguments.Count) {
                argumentCountMismatch = true;
            }

            if (argumentCountMismatch) {
                var nArgs = attrs?.Count ?? 0;
                var nFormalArgs = formalArguments.Count;
                _errorManager.RuntimeError(
                    frame,
                    ErrorType.ARGUMENT_COUNT_MISMATCH,
                    nArgs,
                    st.impl.Name,
                    nFormalArgs);
            }
        }
    }

    private void StoreArguments(TemplateFrame frame, int nArgs, Template st) {
        if (nArgs > 0 && !st.impl.HasFormalArgs && st.impl.FormalArguments == null) {
            st.Add(Template.ImplicitArgumentName, null); // pretend we have "it" arg
        }

        var nFormalArgs = 0;
        if (st.impl.FormalArguments != null) {
            nFormalArgs = st.impl.FormalArguments.Count;
        }
        var firstArg = sp - (nArgs - 1);
        var numToStore = Math.Min(nArgs, nFormalArgs);
        if (st.impl.IsAnonSubtemplate) {
            nFormalArgs -= predefinedAnonSubtemplateAttributes.Length;
        }
        if (nArgs < (nFormalArgs - st.impl.NumberOfArgsWithDefaultValues) || nArgs > nFormalArgs) {
            _errorManager.RuntimeError(frame,
                ErrorType.ARGUMENT_COUNT_MISMATCH,
                nArgs,
                st.impl.Name,
                nFormalArgs);
        }

        if (st.impl.FormalArguments == null)
            return;

        for (var i = 0; i < numToStore; i++) {
            var o = operands[firstArg + i];
            var argName = st.impl.FormalArguments[i].Name;
            st.RawSetAttribute(argName, o);
        }
    }

    private void Indent(ITemplateWriter @out, TemplateFrame frame, int strIndex)
    {
        var self = frame.Template;
        var indent = self.impl.strings[strIndex];
        if (_debug) {
            var start = @out.Index; // track char we're about to write
            EvalExprEvent e = new IndentEvent(frame, new Interval(start, indent.Length), GetExpressionInterval(frame));
            TrackDebugEvent(frame, e);
        }

        @out.PushIndentation(indent);
    }

    /** Write out an expression result that doesn't use expression options.
     *  E.g., &lt;name&gt;
     */
    private int WriteObjectNoOptions(ITemplateWriter @out, TemplateFrame frame, object o) {
        var start = @out.Index; // track char we're about to Write
        var n = WriteObject(@out, frame, o, null);
        if (_debug) {
            var templateLocation = frame.Template.impl.sourceMap[frame.InstructionPointer];
            var e = new EvalExprEvent(frame, Interval.FromBounds(start, @out.Index), templateLocation);
            TrackDebugEvent(frame, e);
        }

        return n;
    }

    /** Write out an expression result that uses expression options.
     *  E.g., &lt;names; separator=", "&gt;
     */
    private int WriteObjectWithOptions(ITemplateWriter @out, TemplateFrame frame, object o, object[] options) {
        var start = @out.Index; // track char we're about to Write
        // precompute all option values (Render all the way to strings)
        string[] optionStrings = null;
        if (options != null) {
            optionStrings = new string[options.Length];
            for (var i = 0; i < TemplateCompiler.NUM_OPTIONS; i++) {
                optionStrings[i] = ToString(frame, options[i]);
            }
        }

        if (options != null && options[(int)RenderOption.Anchor] != null) {
            @out.PushAnchorPoint();
        }
        var n = WriteObject(@out, frame, o, optionStrings);
        if (options != null && options[(int)RenderOption.Anchor] != null) {
            @out.PopAnchorPoint();
        }

        if (_debug) {
            var templateLocation = frame.Template.impl.sourceMap[frame.InstructionPointer];
            var e = new EvalExprEvent(frame, Interval.FromBounds(start, @out.Index), templateLocation);
            TrackDebugEvent(frame, e);
        }
        return n;
    }

    /** Generic method to emit text for an object. It differentiates
     *  between templates, iterable objects, and plain old Java objects (POJOs)
     */
    private int WriteObject(ITemplateWriter @out, TemplateFrame frame, object o, string[] options) {
        var n = 0;
        if (o == null) {
            if (options != null && options[(int)RenderOption.Null] != null) {
                o = options[(int)RenderOption.Null];
            } else {
                return 0;
            }
        }

        var proxyFactory = (frame.Template.Group as TemplateGroup)?.GetTypeProxyFactory(o.GetType());
        if (proxyFactory != null) {
            o = proxyFactory.CreateProxy(frame, o);
        }
        System.Diagnostics.Debug.Assert(!(o is TemplateFrame));
        if (o is Template template) {
            frame = new TemplateFrame(template, frame);
            if (options != null && options[(int)RenderOption.Wrap] != null) {
                // if we have a wrap string, then inform writer it
                // might need to wrap
                try {
                    @out.WriteWrap(options[(int)RenderOption.Wrap]);
                } catch (IOException ioe) {
                    _errorManager.IOError(template, ErrorType.WRITE_IO_ERROR, ioe);
                }
            }
            n = Execute(@out, frame);
        } else {
            o = ConvertAnythingIterableToIterator(frame, o); // normalize
            try {
                n = o is IEnumerator ?
                    WriteIterator(@out, frame, o, options) :
                    WritePlainObject(@out, frame, o, options);
            } catch (IOException ioe) {
                _errorManager.IOError(frame.Template, ErrorType.WRITE_IO_ERROR, ioe, o);
            }
        }
        return n;
    }

    private int WriteIterator(ITemplateWriter @out, TemplateFrame frame, object o, string[] options) {
        if (o == null) {
            return 0;
        }
        var n = 0;
        var it = (IEnumerator)o;
        string separator = null;
        if (options != null) {
            separator = options[(int)RenderOption.Separator];
        }
        var seenAValue = false;
        while (it.MoveNext()) {
            var iterValue = it.Current;
            // Emit separator if we're beyond first value
            var needSeparator = seenAValue &&
                                separator != null &&            // we have a separator and
                                (iterValue != null ||           // either we have a value
                                 options[(int)RenderOption.Null] != null); // or no value but null option
            if (needSeparator)
                n += @out.WriteSeparator(separator);
            var nw = WriteObject(@out, frame, iterValue, options);
            if (nw > 0)
                seenAValue = true;
            n += nw;
        }
        return n;
    }

    private int WritePlainObject(ITemplateWriter @out, TemplateFrame frame, object o, string[] options) {
        string formatString = null;
        if (options != null) {
            formatString = options[(int)RenderOption.Format];
        }
        var r = frame.Template.impl.NativeGroup.GetAttributeRenderer(o.GetType());

        string v;
        if (r != null) {
            v = r.ToString(o, formatString, culture);
        } else {
            if (o is bool b) {
                v = b ? "true" : "false";
            } else {
                v = o.ToString();
            }
        }
        int n;
        if (options != null && options[(int)RenderOption.Wrap] != null) {
            n = @out.Write(v, options[(int)RenderOption.Wrap]);
        } else {
            n = @out.Write(v);
        }
        return n;
    }

    private Interval GetExpressionInterval(TemplateFrame frame) =>
        frame.Template.impl.sourceMap[frame.InstructionPointer];

    private void Map(TemplateFrame frame, object attr, Template st) {
        RotateMap(frame, attr, [st]);
    }

    // <names:a()> or <names:a(),b()>
    private void RotateMap(TemplateFrame frame, object attr, List<Template> prototypes) {
        if (attr == null) {
            operands[++sp] = null;
            return;
        }
        attr = ConvertAnythingIterableToIterator(frame, attr);
        if (attr is IEnumerator iterator) {
            var mapped = RotateMapIterator(frame, iterator, prototypes);
            operands[++sp] = mapped;
        } else {
            // if only single value, just apply first template to sole value
            var proto = prototypes[0];
            var st = proto.CreateShadow();
            if (st != null) {
                SetFirstArgument(frame, st, attr);
                if (st.impl.IsAnonSubtemplate) {
                    st.RawSetAttribute("i0", 0);
                    st.RawSetAttribute("i", 1);
                }
                operands[++sp] = st;
            } else {
                operands[++sp] = null;
            }
        }
    }

    private List<Template> RotateMapIterator(TemplateFrame frame, IEnumerator iterator, List<Template> prototypes) {
        var mapped = new List<Template>();
        var i0 = 0;
        var i = 1;
        var ti = 0;
        while (iterator.MoveNext()) {
            var iterValue = iterator.Current;
            if (iterValue == null) {
                mapped.Add(null);
                continue;
            }
            var templateIndex = ti % prototypes.Count; // rotate through
            ti++;
            var proto = prototypes[templateIndex];
            var st = group.CreateStringTemplateInternally(proto);
            SetFirstArgument(frame, st, iterValue);
            if (st.impl.IsAnonSubtemplate) {
                st.RawSetAttribute("i0", i0);
                st.RawSetAttribute("i", i);
            }
            mapped.Add(st);
            i0++;
            i++;
        }
        return mapped;
    }

    // <names,phones:{n,p | ...}> or <a,b:t()>
    // todo: i, i0 not set unless mentioned? map:{k,v | ..}?
    private Template.AttributeList ZipMap(TemplateFrame frame, List<object> exprs, Template prototype) {
        if (exprs == null || prototype == null || exprs.Count == 0) {
            return null; // do not apply if missing templates or empty values
        }
        // make everything iterable
        for (var i = 0; i < exprs.Count; i++) {
            var attr = exprs[i];
            if (attr != null) {
                exprs[i] = ConvertAnythingToIterator(frame, attr);
            }
        }
        // ensure arguments line up
        var numExprs = exprs.Count;
        var code = prototype.impl;
        var formalArguments = code.FormalArguments;
        if (!code.HasFormalArgs || formalArguments == null) {
            _errorManager.RuntimeError(frame, ErrorType.MISSING_FORMAL_ARGUMENTS);
            return null;
        }

        // todo: track formal args not names for efficient filling of locals
        var formalArgumentNames = formalArguments.Select(i => i.Name).ToArray();
        var nFormalArgs = formalArgumentNames.Length;
        if (prototype.IsAnonymousSubtemplate) {
            nFormalArgs -= predefinedAnonSubtemplateAttributes.Length;
        }
        if (nFormalArgs != numExprs) {
            _errorManager.RuntimeError(frame, ErrorType.MAP_ARGUMENT_COUNT_MISMATCH, numExprs, nFormalArgs);
            // TODO just fill first n
            // truncate arg list to match smaller size
            var shorterSize = Math.Min(formalArgumentNames.Length, numExprs);
            numExprs = shorterSize;
            Array.Resize(ref formalArgumentNames, shorterSize);
        }

        // keep walking while at least one attribute has values

        var results = new Template.AttributeList();
        var i2 = 0; // iteration number from 0
        while (true) {
            // get a value for each attribute in list; put into Template instance
            var numEmpty = 0;
            var embedded = group.CreateStringTemplateInternally(prototype);
            embedded.RawSetAttribute("i0", i2);
            embedded.RawSetAttribute("i", i2 + 1);
            for (var a = 0; a < numExprs; a++) {
                var it = (IEnumerator)exprs[a];
                if (it != null && it.MoveNext()) {
                    var argName = formalArgumentNames[a];
                    var iteratedValue = it.Current;
                    embedded.RawSetAttribute(argName, iteratedValue);
                } else {
                    numEmpty++;
                }
            }
            if (numEmpty == numExprs) {
                break;
            }
            results.Add(embedded);
            i2++;
        }
        return results;
    }

    private void SetFirstArgument(TemplateFrame frame, Template st, object attr) {
        if (!st.impl.HasFormalArgs) {
            if (st.impl.FormalArguments == null) {
                st.Add(Template.ImplicitArgumentName, attr);
                return;
            }
            // else fall through to set locals[0]
        }
        if (st.impl.FormalArguments == null) {
            _errorManager.RuntimeError(frame, ErrorType.ARGUMENT_COUNT_MISMATCH, 1, st.impl.Name, 0);
            return;
        }
        st.locals[0] = attr;
    }

    private void AddToList(List<object> list, TemplateFrame frame, object o) {
        o = ConvertAnythingIterableToIterator(frame, o);
        if (o is IEnumerator it) {
            // copy of elements into our temp list
            while (it.MoveNext()) {
                list.Add(it.Current);
            }
        } else {
            list.Add(o);
        }
    }

    /** Return the first attribute if multiple valued or the attribute
     *  itself if single-valued.  Used in &lt;names:First()&gt;
     */
    private object First(TemplateFrame frame, object v) {
        if (v == null) {
            return null;
        }
        var r = v;
        v = ConvertAnythingIterableToIterator(frame, v);
        if (v is IEnumerator it) {
            if (it.MoveNext()) {
                r = it.Current;
            }
        }
        return r;
    }

    /** Return the last attribute if multiple valued or the attribute
     *  itself if single-valued. Unless it's a list or array, this is pretty
     *  slow as it iterates until the last element.
     */
    private object Last(TemplateFrame frame, object v) {
        if (v == null) {
            return null;
        }
        if (v is IList list) {
            return list[list.Count - 1];
        }
        var last = v;
        v = ConvertAnythingIterableToIterator(frame, v);
        if (v is IEnumerator it) {
            while (it.MoveNext()) {
                last = it.Current;
            }
        }
        return last;
    }

    /** Return everything but the first attribute if multiple valued
     *  or null if single-valued.
     */
    private object Rest(TemplateFrame frame, object v) {
        if (v == null) {
            return null;
        }
        v = ConvertAnythingIterableToIterator(frame, v);
        if (v is IEnumerator it) {
            if (!it.MoveNext()) {
                return null; // if not even one value return null
            }
            var a = new List<object>();
            // first value is ignored above
            while (it.MoveNext()) {
                var o = it.Current;
                a.Add(o);
            }
            return a;
        }
        // rest of single-valued attribute is null
        return null;
    }

    /** Return all but the last element.  Trunc(x)=null if x is single-valued. */
    private object Trunc(TemplateFrame frame, object v) {
        if (v == null) {
            return null;
        }
        v = ConvertAnythingIterableToIterator(frame, v);
        if (v is IEnumerator it) {
            var a = new List<object>();
            while (it.MoveNext()) {
                a.Add(it.Current);
            }
            // remove the last item
            a.RemoveAt(a.Count - 1);
            return a;
        }

        // Trunc(x)==null when x single-valued attribute
        return null;
    }

    /** Return a new list w/o null values. */
    private object Strip(TemplateFrame frame, object v) {
        if (v == null) {
            return null;
        }
        v = ConvertAnythingIterableToIterator(frame, v);
        if (v is IEnumerator it) {
            var a = new List<object>();
            while (it.MoveNext()) {
                var o = it.Current;
                if (o != null) {
                    a.Add(o);
                }
            }
            return a;
        }
        return v; // Strip(x)==x when x single-valued attribute
    }

    /** Return a list with the same elements as v but in reverse order. null
     *  values are NOT stripped out. use Reverse(Strip(v)) to do that.
     */
    private object Reverse(TemplateFrame frame, object v) {
        if (v == null) {
            return null;
        }
        v = ConvertAnythingIterableToIterator(frame, v);
        if (v is IEnumerator it) {
            var a = new List<object>();
            while (it.MoveNext()) {
                a.Add(it.Current);
            }
            a.Reverse();
            return a;
        }
        return v;
    }

    /** Return the length of a mult-valued attribute or 1 if it is a
     *  single attribute. If attribute is null return 0.
     *  Special case several common collections and primitive arrays for
     *  speed. This method by Kay Roepke from v3.
     */
    private object Length(object v) {
        switch (v) {
            case null:
                return 0;
            case string:
                return 1;
            case ICollection collection:
                return collection.Count;
            case IEnumerable enumerable:
                return enumerable.Cast<object>().Count();
            case IEnumerator iterator: {
                var i = 0;
                while (iterator.MoveNext()) {
                    i++;
                }
                return i;
            }
            default: {
                return 1;
            }
        }
    }

    private string ToString(TemplateFrame frame, object value)
    {
        switch (value) {
            case null:
                return null;
            case string s:
                return s;
        }

        // if not string already, must evaluate it
        var sw = new StringWriter();
        /*
        Interpreter interp = new Interpreter(group, new NoIndentWriter(sw), culture);
        interp.WriteObjectNoOptions(self, value, -1, -1);
        */

        if (_debug && !frame.GetDebugState().IsEarlyEval) {
            frame = new TemplateFrame(frame.Template, frame);
            frame.GetDebugState().IsEarlyEval = true;
        }
        WriteObjectNoOptions(new NoIndentWriter(sw), frame, value);
        return sw.ToString();
    }

    private static object ConvertAnythingIterableToIterator(TemplateFrame frame, object o) {
        switch (o) {
            case null:
                return null;
            case string str:
                return str;
            case IDictionary dictionary when (frame.Template.Group as TemplateGroup)?.IterateAcrossValues == true:
                return dictionary.Values.GetEnumerator();
            case IDictionary dictionary:
                return dictionary.Keys.GetEnumerator();
            case ICollection collection:
                return collection.GetEnumerator();
            case IEnumerable enumerable:
                return enumerable.GetEnumerator();
            default:
                //// This code is implied in the last line
                //IEnumerator enumerator = o as IEnumerator;
                //if ( enumerator != null )
                //    return enumerator;
                return o;
        }
    }

    private static IEnumerator ConvertAnythingToIterator(TemplateFrame frame, object o) {
        o = ConvertAnythingIterableToIterator(frame, o);
        if (o is IEnumerator iter) {
            return iter;
        }
        var singleton = new Template.AttributeList(1) { o };
        return singleton.GetEnumerator();
    }

    private bool TestAttributeTrue(object a) {
        return a switch {
            null => false,
            bool b => b,
            string => true,
            ICollection collection => collection.Count > 0,
            IEnumerable enumerable => enumerable.Cast<object>().Any(),
            _ => true
        };
    }

    private object GetObjectProperty(TemplateFrame frame, object o, object property) {
        var self = frame.Template;
        if (o == null) {
            _errorManager.RuntimeError(frame, ErrorType.NO_SUCH_PROPERTY, "null." + property);
            return null;
        }
        try {
            var proxyFactory = (self.Group as TemplateGroup)!.GetTypeProxyFactory(o.GetType());
            if (proxyFactory != null) {
                o = proxyFactory.CreateProxy(frame, o);
            }
            var adaptorDelegate = (self.Group as TemplateGroup)!.GetModelAdaptor(o.GetType());
            return adaptorDelegate(o, property, ToString(frame, property));
        } catch (TemplateNoSuchPropertyException e) {
            _errorManager.RuntimeError(frame, ErrorType.NO_SUCH_PROPERTY,
                e, o.GetType().Name + "." + property);
        }
        return null;
    }

    /** Find an attr via dynamic scoping up enclosing scope chain.
     *  If not found, look for a map.  So attributes sent in to a template
     *  override dictionary names.
     */
    private object GetAttribute(TemplateFrame frame, string name) {
        var scope = frame;
        while (scope != null) {
            var template = scope.Template;
            var arg = template.impl.TryGetFormalArgument(name);
            if (arg != null) {
                var o = template.locals[arg.Index];
                return o;
            }
            scope = scope.Parent;
        }
        // got to root template and no definition, try dictionaries in group
        var self = frame.Template;
        if (self.impl.NativeGroup.IsDictionary(name)) {
            return self.impl.NativeGroup.RawGetDictionary(name);
        }
        // not found, report unknown attr
        throw new AttributeNotFoundException(frame, name);
    }

    /** Set any default argument values that were not set by the
     *  invoking template or by setAttribute directly.  Note
     *  that the default values may be templates.
     *
     *  The evaluation context is the template enclosing invokedST.
     */
    private void SetDefaultArguments(TemplateFrame frame) {
        var invokedST = frame.Template;
        if (invokedST.impl.FormalArguments == null || invokedST.impl.NumberOfArgsWithDefaultValues == 0) {
            return;
        }
        foreach (var arg in invokedST.impl.FormalArguments) {
            // if no value for attribute and default arg, inject default arg into self
            if (invokedST.locals[arg.Index] != Template.EmptyAttribute || arg.DefaultValueToken == null) {
                continue;
            }
            if (arg.DefaultValueToken.Type == GroupParser.ANONYMOUS_TEMPLATE) {
                var code = arg.CompiledDefaultValue ?? new CompiledTemplate();
                var defaultArgST = group.CreateStringTemplateInternally(code);
                // default arg template must see other args so it's enclosing
                // instance is the template we are invoking.
                defaultArgST.Group = group;
                // If default arg is template with single expression
                // wrapped in parens, x={<(...)>}, then eval to string
                // rather than setting x to the template for later
                // eval.
                var defArgTemplate = arg.DefaultValueToken.Text;
                if (defArgTemplate.StartsWith("{" + group.DelimiterStartChar + "(")
                    && defArgTemplate.EndsWith(")" + group.DelimiterStopChar + "}")) {
                    invokedST.RawSetAttribute(arg.Name, ToString(new TemplateFrame(defaultArgST, frame), defaultArgST));
                } else {
                    invokedST.RawSetAttribute(arg.Name, defaultArgST);
                }
            } else {
                invokedST.RawSetAttribute(arg.Name, arg.DefaultValue);
            }
        }
    }

    private void Trace(TemplateFrame frame, int ip) {
        var self = frame.Template;
        var buf = new StringBuilder();
        BytecodeDisassembler.DisassembleInstruction(self.impl, buf, ip);
        var name = self.impl.Name + ":";
        if (ReferenceEquals(self.impl.Name, Template.UnknownName)) {
            name = string.Empty;
        }
        var tr = new StringBuilder();
        tr.Append($"{name + buf,-40}");
        tr.Append("\tstack=[");
        for (var i = 0; i <= sp; i++) {
            var o = operands[i];
            PrintForTrace(tr, frame, o);
        }
        tr.Append(" ], calls=");
        tr.Append(frame.GetEnclosingInstanceStackString());
        tr.Append(", sp=" + sp + ", nw=" + nwline);
        var s = tr.ToString();
        _logger.LogTrace("{Trace}", s);
    }

    private void PrintForTrace(StringBuilder tr, TemplateFrame frame, object o) {
        if (o is Template template) {
            if (template.impl == null) {
                tr.Append("bad-template()");
            } else {
                tr.Append(" " + template.impl.Name + "()");
            }
            return;
        }
        o = ConvertAnythingIterableToIterator(frame, o);
        if (o is IEnumerator it) {
            tr.Append(" [");
            while (it.MoveNext()) {
                var iterValue = it.Current;
                PrintForTrace(tr, frame, iterValue);
            }
            tr.Append(" ]");
        } else {
            tr.Append(" " + o);
        }
    }

    public List<InterpEvent> GetEvents() {
        return events;
    }

    /** For every event, we track in overall list and in self's
     *  event list so that each template has a list of events used to
     *  create it.  If EvalTemplateEvent, store in parent's
     *  childEvalTemplateEvents list for STViz tree view.
     */
    private void TrackDebugEvent(TemplateFrame frame, InterpEvent e) {
        _logger.LogTrace("{Event}", e);
        events.Add(e);
        //		if ( self.debugState==null ) self.debugState = new ST.DebugState();
        //		self.debugState.events.add(e);
        frame.GetDebugState().Events.Add(e);
        if (e is EvalTemplateEvent evt) {
            //ST parent = getDebugState(self).interpEnclosingInstance;
            var parent = frame.Parent;
            if (parent != null) {
                // System.out.println("add eval "+e.self.getName()+" to children of "+parent.getName());
                //				if ( parent.debugState==null ) parent.debugState = new ST.DebugState();
                //				parent.debugState.childEvalTemplateEvents.add((EvalTemplateEvent)e);
                parent.GetDebugState().ChildEvalTemplateEvents.Add(evt);
            }
        }
    }

    private static int GetShort(byte[] value, int startIndex) {
        return BitConverter.ToInt16(value, startIndex);
    }

}
