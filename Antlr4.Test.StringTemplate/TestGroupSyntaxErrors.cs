/*
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

namespace Antlr4.Test.StringTemplate;

using Antlr4.StringTemplate;
using Antlr4.StringTemplate.Misc;
using Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Path = System.IO.Path;

[TestClass]
public class TestGroupSyntaxErrors : BaseTest {

    [TestMethod]
    public void TestMissingImportString() {
        const string templates =
            "import\n" +
            "foo() ::= <<>>\n";
        WriteFile(TmpDir, "t.stg", templates);

        ITemplateErrorListener errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).WithErrorListener(errors).Build();
        group.Load(); // force load
        var expected = "t.stg 2:0: mismatched input 'foo' expecting STRING" + newline +
                       "t.stg 2:3: missing EndOfFile at '('" + newline;
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestImportNotString() {
        const string templates =
            "import Super.stg\n" +
            "foo() ::= <<>>\n";
        WriteFile(TmpDir, "t.stg", templates);

        ITemplateErrorListener errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).WithErrorListener(errors).Build();
        group.Load(); // force load
        var expected = "t.stg 1:7: mismatched input 'Super' expecting STRING" + newline;
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestMissingTemplate() {
        const string templates =
            "foo() ::= \n";
        WriteFile(TmpDir, "t.stg", templates);

        ITemplateErrorListener errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).WithErrorListener(errors).Build();
        group.Load(); // force load
        var expected = "t.stg 2:0: missing template at '<EOF>'" + newline;
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestUnclosedTemplate() {
        const string templates = "foo() ::= {";
        WriteFile(TmpDir, "t.stg", templates);

        ITemplateErrorListener errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).WithErrorListener(errors).Build();
        group.Load(); // force load
        var expected = "t.stg 1:11: missing final '}' in {...} anonymous template" + newline +
                       "t.stg 1:10: no viable alternative at input '{'" + newline;
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestParen() {
        const string templates = "foo( ::= << >>\n";
        WriteFile(TmpDir, "t.stg", templates);

        ITemplateErrorListener errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).WithErrorListener(errors).Build();
        group.Load(); // force load
        var expected = "t.stg 1:5: no viable alternative at input '::='" + newline;
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestNewlineInString() {
        const string templates = "foo() ::= \"\nfoo\"\n";
        WriteFile(TmpDir, "t.stg", templates);

        ITemplateErrorListener errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).WithErrorListener(errors).Build();
        group.Load(); // force load
        var expected = "t.stg 1:11: \\n in string" + newline;
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestParen2() {
        const string templates =
            "foo) ::= << >>\n" +
            "bar() ::= <<bar>>\n";
        WriteFile(TmpDir, "t.stg", templates);

        ITemplateErrorListener errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).WithErrorListener(errors).Build();
        group.Load(); // force load
        var expected = "t.stg 1:0: garbled template definition starting at 'foo'" + newline;
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestArg() {
        const string templates = "foo(a,) ::= << >>\n";
        WriteFile(TmpDir, "t.stg", templates);

        ITemplateErrorListener errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).WithErrorListener(errors).Build();
        group.Load(); // force load
        var expected = "t.stg 1:6: missing ID at ')'" + newline;
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestArg2() {
        const string templates = "foo(a,,) ::= << >>\n";
        WriteFile(TmpDir, "t.stg", templates);

        var errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).WithErrorListener(errors).Build();
        group.Load(); // force load
        const string expected =
            "[t.stg 1:6: missing ID at ',', " +
            "t.stg 1:7: missing ID at ')']";
        var result = errors.Errors.ToListString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestArg3() {
        const string templates = "foo(a b) ::= << >>\n";
        WriteFile(TmpDir, "t.stg", templates);

        var errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).WithErrorListener(errors).Build();
        group.Load(); // force load
        const string expected = "[t.stg 1:6: no viable alternative at input 'b']";
        var result = errors.Errors.ToListString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefaultArgsOutOfOrder() {
        const string templates = "foo(a={hi}, b) ::= << >>\n";
        WriteFile(TmpDir, "t.stg", templates);

        var errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).WithErrorListener(errors).Build();
        group.Load(); // force load
        const string expected =
            "[t.stg 1:13: Optional parameters must appear after all required parameters]";
        var result = errors.Errors.ToListString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestErrorWithinTemplate() {
        const string templates = "foo(a) ::= \"<a b>\"\n";
        WriteFile(TmpDir, "t.stg", templates);

        var errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).WithErrorListener(errors).Build();
        group.Load(); // force load
        const string expected = "[t.stg 1:15: 'b' came as a complete surprise to me]";
        var result = errors.Errors.ToListString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestMap2() {
        const string templates = "d ::= [\"k\":]\n";
        WriteFile(TmpDir, "t.stg", templates);

        var errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).WithErrorListener(errors).Build();
        group.Load(); // force load
        const string expected = "[t.stg 1:11: missing value for key at ']']";
        var result = errors.Errors.ToListString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestMap3() {
        const string templates = "d ::= [\"k\":{abcd}}]\n"; // extra }
        WriteFile(TmpDir, "t.stg", templates);

        var errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).WithErrorListener(errors).Build();
        group.Load(); // force load
        const string expected = "[t.stg 1:17: invalid character '}']";
        var result = errors.Errors.ToListString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestUnterminatedString() {
        const string templates = "f() ::= \""; // extra }
        WriteFile(TmpDir, "t.stg", templates);

        var errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).WithErrorListener(errors).Build();
        group.Load(); // force load
        const string expected = "[t.stg 1:9: unterminated string, t.stg 1:9: missing template at '<EOF>']";
        var result = errors.Errors.ToListString();
        Assert.AreEqual(expected, result);
    }

}
