/*
 [The "BSD license"]
 Copyright (c) 2009 Terence Parr
 All rights reserved.

 Redistribution and use in source and binary forms, with or without
 modification, are permitted provided that the following conditions
 are met:
 1. Redistributions of source code must retain the above copyright
    notice, this list of conditions and the following disclaimer.
 2. Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the
    documentation and/or other materials provided with the distribution.
 3. The name of the author may not be used to endorse or promote products
    derived from this software without specific prior written permission.

 THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
 IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
 INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/


namespace Antlr4.Test.StringTemplate;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Antlr4.StringTemplate;
using Extensions;
using Antlr4.StringTemplate.Misc;
using Antlr4.StringTemplate.Compiler;
using Path = System.IO.Path;

[TestClass]
public class TestSyntaxErrors : BaseTest {

    [TestMethod]
    public void TestEmptyExpr() {
        const string template = " <> ";
        var group = new TemplateGroup();
        var errors = new ErrorBuffer();
        group.Listener = errors;
        try {
            group.DefineTemplate("test", template);
        } catch (TemplateException) {
        }
        var result = errors.ToString();
        var expected = "test 1:0: this doesn't look like a template: \" <> \"" + newline;
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestIt() {
        var templates =
            $"main() ::= <<{newline}" +
            $"<@r>a<@end>{newline}" +
            $"<@r()>{newline}" +
            ">>";
        WriteFile(TmpDir, "t.stg", templates);
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "t.stg"));
        var errors = new ErrorBuffer();
        group.Listener = errors;
        group.Load();
        Assert.AreEqual(0, errors.Errors.Count);

        var template = group.GetInstanceOf("main");
        var expected = $"a{newline}a";
        var result = template.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEmptyExpr2() {
        const string template = "hi <> ";
        var group = new TemplateGroup();
        var errors = new ErrorBuffer();
        group.Listener = errors;
        try {
            group.DefineTemplate("test", template);
        } catch (TemplateException) {
        }
        var result = errors.ToString();
        var expected = "test 1:3: doesn't look like an expression" + newline;
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestUnterminatedExpr() {
        const string template = "hi <t()$";
        var group = new TemplateGroup();
        var errors = new ErrorBuffer();
        group.Listener = errors;
        try {
            group.DefineTemplate("test", template);
        } catch (TemplateException) {
        }
        var result = errors.ToString();
        var expected = "test 1:7: invalid character '$'" + newline +
                       "test 1:7: invalid character '<EOF>'" + newline +
                       "test 1:7: premature EOF" + newline;
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestWeirdChar() {
        const string template = "   <*>";
        var group = new TemplateGroup();
        var errors = new ErrorBuffer();
        group.Listener = errors;
        try {
            group.DefineTemplate("test", template);
        } catch (TemplateException) {
        }
        var result = errors.ToString();
        var expected = "test 1:4: invalid character '*'" + newline +
                       "test 1:0: this doesn't look like a template: \"   <*>\"" + newline;
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestWeirdChar2() {
        const string template = "\n<\\\n";
        var group = new TemplateGroup();
        var errors = new ErrorBuffer();
        group.Listener = errors;
        try {
            group.DefineTemplate("test", template);
        } catch (TemplateException) {
        }
        var result = errors.ToString();
        var expected = "test 1:2: invalid escaped char: '<EOF>'" + newline +
                       "test 1:2: expecting '>', found '<EOF>'" + newline;
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestValidButOutOfPlaceChar() {
        const string templates = "foo() ::= <<hi <.> mom>>\n";
        WriteFile(TmpDir, "t.stg", templates);

        var errors = new ErrorBuffer();
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "t.stg")) {
            Listener = errors
        };
        group.Load(); // force load
        var expected = "t.stg 1:15: doesn't look like an expression" + newline;
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestValidButOutOfPlaceCharOnDifferentLine() {
        const string templates =
            "foo() ::= \"hi <\n" +
            ".> mom\"\n";
        WriteFile(TmpDir, "t.stg", templates);

        var errors = new ErrorBuffer();
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "t.stg")) {
            Listener = errors
        };
        group.Load(); // force load
        const string expected = "[t.stg 1:15: \\n in string, t.stg 1:14: doesn't look like an expression]";
        var result = errors.Errors.ToListString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestErrorInNestedTemplate() {
        var templates = "foo() ::= \"hi <name:{[<aaa.bb!>]}> mom\"\n";
        WriteFile(TmpDir, "t.stg", templates);

        var errors = new ErrorBuffer();
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "t.stg")) {
            Listener = errors
        };
        group.Load(); // force load
        var expected = "t.stg 1:29: mismatched input '!' expecting RDELIM" + newline;
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEOFInExpr() {
        const string templates = "foo() ::= \"hi <name:{x|[<aaa.bb>]}\"\n";
        WriteFile(TmpDir, "t.stg", templates);

        var errors = new ErrorBuffer();
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "t.stg")) {
            Listener = errors
        };
        group.Load(); // force load
        var expected = "t.stg 1:34: premature EOF" + newline;
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEOFInExpr2() {
        const string templates = "foo() ::= \"hi <name:{x|[<aaa.bb>]}\"\n";
        WriteFile(TmpDir, "t.stg", templates);

        var errors = new ErrorBuffer();
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "t.stg")) {
            Listener = errors
        };
        group.Load(); // force load
        var expected = "t.stg 1:34: premature EOF" + newline;
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEOFInString() {
        const string templates = "foo() ::= << <f(\"foo>>\n";
        WriteFile(TmpDir, "t.stg", templates);

        var errors = new ErrorBuffer();
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "t.stg")) {
            Listener = errors
        };
        group.Load(); // force load
        var expected =
            $"t.stg 1:20: EOF in string{newline}" +
            $"t.stg 1:20: premature EOF{newline}";
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestNonTerminatedComment() {
        var templates = "foo() ::= << <!foo> >>";
        WriteFile(TmpDir, "t.stg", templates);

        var errors = new ErrorBuffer();
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "t.stg")) {
            Listener = errors
        };
        group.Load(); // force load
        var expected =
            $"t.stg 1:20: Non-terminated comment starting at 1:1: '!>' missing{newline}";
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestMissingRPAREN() {
        const string templates = "foo() ::= \"hi <foo(>\"\n";
        WriteFile(TmpDir, "t.stg", templates);

        var errors = new ErrorBuffer();
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "t.stg")) {
            Listener = errors
        };
        group.Load(); // force load
        var expected = $"t.stg 1:19: '>' came as a complete surprise to me{newline}";
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestRotPar() {
        const string templates = "foo() ::= \"<a,b:t(),u()>\"\n";
        WriteFile(TmpDir, "t.stg", templates);

        var errors = new ErrorBuffer();
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "t.stg")) {
            Listener = errors
        };
        group.Load(); // force load
        var expected = $"t.stg 1:19: mismatched input ',' expecting RDELIM{newline}";
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

}
