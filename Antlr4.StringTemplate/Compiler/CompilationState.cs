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

namespace Antlr4.StringTemplate.Compiler;

using Antlr.Runtime;
using Antlr.Runtime.Tree;
using Misc;
using Array = System.Array;

/** temp data used during construction and functions that fill it / use it.
 *  Result is impl CompiledTemplate object.
 */
public class CompilationState {

    /** The compiled code implementation to fill in. */
    internal readonly CompiledTemplate impl = new();

    /** Track unique strings; copy into CompiledTemplate's String[] after compilation */
    internal readonly StringTable stringtable = new();

    /** Track instruction location within code.instrs array; this is
     *  next address to Write to.  Byte-addressable memory.
     */
    internal int ip;

    private readonly ITokenStream tokens;

    private readonly ErrorManager errMgr;

    public CompilationState(ErrorManager errMgr, string name, ITokenStream tokens) {
        this.errMgr = errMgr;
        this.tokens = tokens;
        impl.Name = name;
        impl.Prefix = Utility.GetPrefix(name);
    }

    public int DefineString(string s) {
        return stringtable.Add(s);
    }

    public void ReferenceAttribute(IToken templateToken, CommonTree id) {
        var name = id.Text;
        var arg = impl.TryGetFormalArgument(name);
        if (arg != null) {
            var index = arg.Index;
            Emit1(id, Bytecode.INSTR_LOAD_LOCAL, index);
        } else {
            if (Interpreter.PredefinedAnonymousSubtemplateAttributes.Contains(name)) {
                errMgr.CompiletimeError(ErrorType.REF_TO_IMPLICIT_ATTRIBUTE_OUT_OF_SCOPE, templateToken, id.Token);
                Emit(id, Bytecode.INSTR_NULL);
            } else {
                Emit1(id, Bytecode.INSTR_LOAD_ATTR, name);
            }
        }
    }

    public void SetOption(CommonTree id) {
        var O = TemplateCompiler.supportedOptions[id.Text];
        Emit1(id, Bytecode.INSTR_STORE_OPTION, (int)O);
    }

    public void Function(IToken templateToken, CommonTree id) {
        if (!TemplateCompiler.funcs.TryGetValue(id.Text, out var funcBytecode)) {
            errMgr.CompiletimeError(ErrorType.NO_SUCH_FUNCTION, templateToken, id.Token);
            Emit(id, Bytecode.INSTR_POP);
        } else {
            Emit(id, funcBytecode);
        }
    }

    public void Emit(Bytecode opcode) {
        Emit(null, opcode);
    }

    public void Emit(CommonTree opAST, Bytecode opcode) {
        EnsureCapacity(1);
        if (opAST != null) {
            var i = opAST.TokenStartIndex;
            var j = opAST.TokenStopIndex;
            var p = tokens.Get(i).StartIndex;
            var q = tokens.Get(j).StopIndex;
            if (!(p < 0 || q < 0))
                impl.sourceMap[ip] = Interval.FromBounds(p, q + 1);
        }
        impl.instrs[ip++] = (byte)opcode;
    }

    public void Emit1(CommonTree opAST, Bytecode opcode, int arg) {
        Emit(opAST, opcode);
        EnsureCapacity(Instruction.OperandSizeInBytes);
        WriteShort(impl.instrs, ip, (short)arg);
        ip += Instruction.OperandSizeInBytes;
    }

    private void Emit2(CommonTree opAST, Bytecode opcode, int arg, int arg2) {
        Emit(opAST, opcode);
        EnsureCapacity(Instruction.OperandSizeInBytes * 2);
        WriteShort(impl.instrs, ip, (short)arg);
        ip += Instruction.OperandSizeInBytes;
        WriteShort(impl.instrs, ip, (short)arg2);
        ip += Instruction.OperandSizeInBytes;
    }

    public void Emit2(CommonTree opAST, Bytecode opcode, string s, int arg2) {
        var i = DefineString(s);
        Emit2(opAST, opcode, i, arg2);
    }

    public void Emit1(CommonTree opAST, Bytecode opcode, string s) {
        var i = DefineString(s);
        Emit1(opAST, opcode, i);
    }

    public void Write(int addr, short value) {
        WriteShort(impl.instrs, addr, value);
    }

    private void EnsureCapacity(int n) {
        if ((ip + n) >= impl.instrs.Length) {
            // ensure room for full instruction
            Array.Resize(ref impl.instrs, impl.instrs.Length * 2);
            Array.Resize(ref impl.sourceMap, impl.sourceMap.Length * 2);
        }
    }

    public void Indent(CommonTree indent) {
        Emit1(indent, Bytecode.INSTR_INDENT, indent.Text);
    }

    /** Write value at index into a byte array highest to lowest byte,
     *  left to right.
     */
    private static void WriteShort(byte[] memory, int index, short value) {
        memory[index + 0] = (byte)(value & 0xFF);
        memory[index + 1] = (byte)((value >> (8 * 1)) & 0xFF);
    }

}
