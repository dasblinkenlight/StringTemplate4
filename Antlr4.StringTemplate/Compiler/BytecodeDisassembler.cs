﻿/*
 * [The "BSD licence"]
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

using System.Collections.Generic;
using Misc;
using ArgumentException = System.ArgumentException;
using BitConverter = System.BitConverter;
using StringBuilder = System.Text.StringBuilder;

public class BytecodeDisassembler {

    private readonly CompiledTemplate code;

    public BytecodeDisassembler(CompiledTemplate code) {
        this.code = code;
    }

    public string GetInstructions() {
        var buf = new StringBuilder();
        var ip = 0;
        while (ip < code.codeSize) {
            if (ip > 0) {
                buf.Append(", ");
            }
            int opcode = code.instrs[ip];
            var I = Instruction.instructions[opcode];
            buf.Append(I.name);
            ip++;
            for (var operand = 0; operand < I.nopnds; operand++) {
                buf.Append(' ');
                buf.Append(GetShort(code.instrs, ip));
                ip += Instruction.OperandSizeInBytes;
            }
        }
        return buf.ToString();
    }

    public string Disassemble() {
        var buf = new StringBuilder();
        var i = 0;
        while (i < code.codeSize) {
            i = DisassembleInstruction(buf, i);
            buf.AppendLine();
        }
        return buf.ToString();
    }

    public int DisassembleInstruction(StringBuilder buf, int ip) {
        int opcode = code.instrs[ip];
        if (ip >= code.codeSize) {
            throw new ArgumentException("ip out of range: " + ip);
        }
        var I = Instruction.instructions[opcode];
        if (I == null) {
            throw new ArgumentException("no such instruction " + opcode + " at address " + ip);
        }
        var instrName = I.name;
        buf.Append($"{ip:0000}:\t{instrName,-14}");
        ip++;
        if (I.nopnds == 0) {
            buf.Append("  ");
            return ip;
        }
        var operands = new List<string>();
        for (var i = 0; i < I.nopnds; i++) {
            var operand = GetShort(code.instrs, ip);
            ip += Instruction.OperandSizeInBytes;
            switch (I.type[i]) {
                case OperandType.String:
                    operands.Add(ShowConstantPoolOperand(operand));
                    break;

                case OperandType.Address:
                case OperandType.Int:
                    operands.Add(operand.ToString());
                    break;

                case OperandType.None:
                default:
                    operands.Add(operand.ToString());
                    break;
            }
        }
        for (var i = 0; i < operands.Count; i++) {
            var s = operands[i];
            if (i > 0) {
                buf.Append(", ");
            }
            buf.Append(s);
        }
        return ip;
    }

    private string ShowConstantPoolOperand(int poolIndex) {
        var buf = new StringBuilder();
        buf.Append("#");
        buf.Append(poolIndex);
        var s = "<bad string index>";
        if (poolIndex < code.strings.Length) {
            if (code.strings[poolIndex] == null) {
                s = "null";
            } else {
                s = code.strings[poolIndex];
                if (code.strings[poolIndex] != null) {
                    s = Utility.ReplaceEscapes(s);
                    s = '"' + s + '"';
                }
            }
        }
        buf.Append(":");
        buf.Append(s);
        return buf.ToString();
    }

    internal static int GetShort(byte[] memory, int index) {
        return BitConverter.ToInt16(memory, index);
    }

    public string GetStrings() {
        var buf = new StringBuilder();
        var addr = 0;
        if (code.strings == null) {
            return buf.ToString();
        }
        foreach (var os in code.strings) {
            var s = Utility.ReplaceEscapes(os);
            buf.AppendLine($"{addr:0000}: \"{s}\"");
            addr++;
        }
        return buf.ToString();
    }

    public string GetSourceMap() {
        var buf = new StringBuilder();
        var addr = 0;
        foreach (var interval in code.sourceMap) {
            if (interval != null) {
                var chunk = code.Template.Substring(interval.Start, interval.Length);
                buf.AppendLine($"{addr:0000}: {interval}\t\"{chunk}\"");
            }
            addr++;
        }
        return buf.ToString();
    }

}
