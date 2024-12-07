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

using Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ErrorBuffer = Antlr4.StringTemplate.Misc.ErrorBuffer;
using Path = System.IO.Path;

[TestClass]
public class TestGroupSyntax : BaseTest {

    [TestMethod]
    public void TestSimpleGroup() {
        var templates = $"t() ::= <<foo>>{newline}";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var expected =
            "t() ::= <<" + newline +
            "foo"        + newline +
            ">>"         + newline;
        var result = group.Description;
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEscapedQuote() {
        // setTest(ranges) ::= "<ranges; separator=\"||\">"
        // has to unescape the strings.
        var templates = $"setTest(ranges) ::= \"<ranges; separator=\\\"||\\\">\"{newline}";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var expected =
            "setTest(ranges) ::= <<" + newline +
            "<ranges; separator=\"||\">" + newline +
            ">>" + newline;
        var result = group.Description;
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestMultiTemplates() {
        var templates =
            "ta(x) ::= \"[<x>]\"" + newline +
            "duh() ::= <<hi there>>" + newline +
            "wow() ::= <<last>>" + newline;

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var expected =
            "ta(x) ::= <<" + newline +
            "[<x>]" + newline +
            ">>" + newline +
            "duh() ::= <<" + newline +
            "hi there" + newline +
            ">>" + newline +
            "wow() ::= <<" + newline +
            "last" + newline +
            ">>" + newline;
        var result = group.Description;
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSetDefaultDelimiters() {
        var templates =
            "delimiters \"<\", \">\"" +     newline +
            "ta(x) ::= \"[<x>]\"" +     newline;

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var st = group.FindTemplate("ta");
        st.Add("x", "hi");
        const string expected = "[hi]";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSetNonDefaultDelimiters() {
        var templates =
            "delimiters \"%\", \"%\"" +     newline +
            "ta(x) ::= \"[%x%]\"" +     newline;

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var st = group.FindTemplate("ta");
        st.Add("x", "hi");
        const string expected = "[hi]";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSingleTemplateWithArgs() {
        var templates =
            "t(a,b) ::= \"[<a>]\"" + newline;

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var expected =
            "t(a,b) ::= <<" + newline +
            "[<a>]" + newline +
            ">>" + newline;
        var result = group.Description;
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefaultValues() {
        var templates = $"t(a={{def1}},b=\"def2\") ::= \"[<a>]\"{newline}";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var expected =
            "t(a={def1},b=\"def2\") ::= <<" + newline +
            "[<a>]" + newline +
            ">>" + newline;
        var result = group.Description;
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefaultValues2() {
        var templates = $"t(x, y, a={{def1}}, b=\"def2\") ::= \"[<a>]\"{newline}";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var expected =
            "t(x,y,a={def1},b=\"def2\") ::= <<" + newline +
            "[<a>]" + newline +
            ">>" + newline;
        var result = group.Description;
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefaultValueTemplateWithArg() {
        var templates = $"t(a={{x | 2*<x>}}) ::= \"[<a>]\"{newline}";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var expected =
            "t(a={x | 2*<x>}) ::= <<" + newline +
            "[<a>]" + newline +
            ">>" + newline;
        var result = group.Description;
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefaultValueBehaviorTrue() {
        var templates =
            "t(a=true) ::= <<\n" +
            "<a><if(a)>+<else>-<endif>\n" +
            ">>\n";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(TmpDir + Path.DirectorySeparatorChar + "t.stg").Build();
        var st = group.FindTemplate("t");
        const string expected = "true+";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefaultValueBehaviorFalse() {
        const string templates =
            "t(a=false) ::= <<\n" +
            "<a><if(a)>+<else>-<endif>\n" +
            ">>\n";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(TmpDir + Path.DirectorySeparatorChar + "t.stg").Build();
        var st = group.FindTemplate("t");
        const string expected = "false-";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefaultValueBehaviorEmptyTemplate() {
        const string templates =
            "t(a={}) ::= <<\n" +
            "<a><if(a)>+<else>-<endif>\n" +
            ">>\n";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(TmpDir + Path.DirectorySeparatorChar + "t.stg").Build();
        var st = group.FindTemplate("t");
        const string expected = "+";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefaultValueBehaviorEmptyList() {
        const string templates =
            "t(a=[]) ::= <<\n" +
            "<a><if(a)>+<else>-<endif>\n" +
            ">>\n";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(TmpDir + Path.DirectorySeparatorChar + "t.stg").Build();
        var st = group.FindTemplate("t");
        const string expected = "-";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestNestedTemplateInGroupFile() {
        var templates = $"t(a) ::= \"<a:{{x | <x:{{y | <y>}}>}}>\"{newline}";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var expected =
            "t(a) ::= <<" +     newline +
            "<a:{x | <x:{y | <y>}>}>" +     newline +
            ">>" +     newline;
        var result = group.Description;
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestNestedDefaultValueTemplate() {
        var templates = "t(a={x | <x:{y|<y>}>}) ::= \"ick\"" + newline;

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        group.Load();
        var expected =
            "t(a={x | <x:{y|<y>}>}) ::= <<" + newline +
            "ick" + newline +
            ">>" + newline;
        var result = group.Description;
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestNestedDefaultValueTemplateWithEscapes() {
        var templates =
            "t(a={x | \\< <x:{y|<y>\\}}>}) ::= \"[<a>]\"" + newline;

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var expected =
            "t(a={x | \\< <x:{y|<y>\\}}>}) ::= <<" + newline +
            "[<a>]" + newline +
            ">>" + newline;
        var result = group.Description;
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestMessedUpTemplateDoesntCauseRuntimeError() {
        const string templates =
            "main(p) ::= <<\n" +
            "<f(x=\"abc\")>\n" +
            ">>\n" +
            "\n" +
            "f() ::= <<\n" +
            "<x>\n" +
            ">>\n";
        WriteFile(TmpDir, "t.stg", templates);

        var errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).WithErrorListener(errors).Build();
        var st = group.FindTemplate("main");
        st.Render();

        const string expected =
            "[context [/main] 1:1 attribute x isn't defined," +
            " context [/main] 1:1 passed 1 arg(s) to template /f with 0 declared arg(s)," +
            " context [/main /f] 1:1 attribute x isn't defined]";
        var result = errors.Errors.ToListString();
        Assert.AreEqual(expected, result);
    }

}
