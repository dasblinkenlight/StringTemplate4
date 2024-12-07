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

namespace Antlr4.Test.StringTemplate;

using Antlr4.StringTemplate;
using Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Path = System.IO.Path;
using StringWriter = System.IO.StringWriter;

[TestClass]
public class TestDebugEvents : BaseTest {

    [TestMethod]
    public void TestString() {
        var templates = $"t() ::= <<foo>>{newline}";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var st = group.GetInstanceOf("t");
        var events = st.GetEvents();
        const string expected =
            "[EvalExprEvent{self=/t(), expr='foo', source=[0..3), output=[0..3)}," +
            " EvalTemplateEvent{self=/t(), output=[0..3)}]";
        var result = events.ToListString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestAttribute() {
        var templates = $"t(x) ::= << <x> >>{newline}";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var st = group.GetInstanceOf("t");
        var events = st.GetEvents();
        const string expected =
            "[IndentEvent{self=/t(x), expr=' <x>', source=[0..4), output=[0..1)}," +
            " EvalExprEvent{self=/t(x), expr='<x>', source=[1..4), output=[0..0)}," +
            " EvalExprEvent{self=/t(x), expr=' ', source=[4..5), output=[0..1)}," +
            " EvalTemplateEvent{self=/t(x), output=[0..1)}]";
        var result = events.ToListString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestTemplateCall() {
        const string templates =
            "t(x) ::= <<[<u()>]>>\n" +
            "u() ::= << <x> >>\n";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var st = group.GetInstanceOf("t");
        var events = st.GetEvents();
        const string expected =
            "[EvalExprEvent{self=/t(x), expr='[', source=[0..1), output=[0..1)}," +
            " IndentEvent{self=/u(), expr=' <x>', source=[0..4), output=[1..2)}," +
            " EvalExprEvent{self=/u(), expr='<x>', source=[1..4), output=[1..1)}," +
            " EvalExprEvent{self=/u(), expr=' ', source=[4..5), output=[1..2)}," +
            " EvalTemplateEvent{self=/u(), output=[1..2)}," +
            " EvalExprEvent{self=/t(x), expr='<u()>', source=[1..6), output=[1..2)}," +
            " EvalExprEvent{self=/t(x), expr=']', source=[6..7), output=[2..3)}," +
            " EvalTemplateEvent{self=/t(x), output=[0..3)}]";
        var result = events.ToListString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEvalExprEventForSpecialCharacter() {
        const string templates = "t() ::= <<[<\\n>]>>\n";
        //                            012 345
        var g = _templateFactory.CreateTemplateGroupString(templates).Build();
        var st = g.GetInstanceOf("t");
        TestContext.WriteLine(st.impl.ToString());
        var writer = new StringWriter();
        var events = st.GetEvents(new AutoIndentWriter(writer, "\n"));
        const string expected =
            "[EvalExprEvent{self=/t(), expr='[', source=[0..1), output=[0..1)}, " +
            "EvalExprEvent{self=/t(), expr='\\n', source=[2..4), output=[1..2)}, " +
            "EvalExprEvent{self=/t(), expr=']', source=[5..6), output=[2..3)}, " +
            "EvalTemplateEvent{self=/t(), output=[0..3)}]";
        var result = events.ToListString();
        Assert.AreEqual(expected, result);
    }
}
