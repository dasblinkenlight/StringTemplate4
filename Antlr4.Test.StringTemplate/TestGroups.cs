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

using System.Linq;
using Antlr4.StringTemplate.Debug;

namespace Antlr4.Test.StringTemplate;

using Antlr4.StringTemplate;
using Antlr4.StringTemplate.Misc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.CompilerServices;
using Path = System.IO.Path;

[TestClass]
public class TestGroups : BaseTest {

    [TestMethod]
    public void TestSimpleGroup() {
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a(x) ::= <<foo>>");
        var group = _templateFactory.CreateTemplateGroupDirectory(dir).Build();
        var st = group.FindTemplate("a");
        const string expected = "foo";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEscapeOneRightAngle() {
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a(x) ::= << > >>");
        var group = _templateFactory.CreateTemplateGroupDirectory(dir).Build();
        var st = group.FindTemplate("a");
        st.Add("x", "parrt");
        const string expected = " > ";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEscapeJavaRightShift() {
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a(x) ::= << \\>> >>");
        var group = _templateFactory.CreateTemplateGroupDirectory(dir).Build();
        var st = group.FindTemplate("a");
        st.Add("x", "parrt");
        const string expected = " >> ";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEscapeJavaRightShift2() {
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a(x) ::= << >\\> >>");
        var group = _templateFactory.CreateTemplateGroupDirectory(dir).Build();
        var st = group.FindTemplate("a");
        st.Add("x", "parrt");
        const string expected = " >> ";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEscapeJavaRightShiftAtRightEdge() {
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a(x) ::= <<\\>>>"); // <<\>>>
        var group = _templateFactory.CreateTemplateGroupDirectory(dir).Build();
        var st = group.FindTemplate("a");
        st.Add("x", "parrt");
        const string expected = "\\>";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEscapeJavaRightShiftAtRightEdge2() {
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a(x) ::= <<>\\>>>");
        var group = _templateFactory.CreateTemplateGroupDirectory(dir).Build();
        var st = group.FindTemplate("a");
        st.Add("x", "parrt");
        var expected = ">>";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSimpleGroupFromString() {
        const string g =
            "a(x) ::= <<foo>>\n" +
            "b() ::= <<bar>>\n";
        var group = _templateFactory.CreateTemplateGroupString(g).Build();
        var st = group.FindTemplate("a");
        const string expected = "foo";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestGroupWithTwoTemplates() {
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a(x) ::= <<foo>>");
        WriteFile(dir, "b.st", "b() ::= \"bar\"");
        var group = _templateFactory.CreateTemplateGroupDirectory(dir).Build();
        var st1 = group.FindTemplate("a");
        var st2 = group.FindTemplate("b");
        const string expected = "foobar";
        var result = st1.Render() + st2.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSubdir() {
        // /randomdir/a and /randomdir/subdir/b
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a(x) ::= <<foo>>");
        WriteFile(Path.Combine(dir, "subdir"), "b.st", "b() ::= \"bar\"");
        var group = _templateFactory.CreateTemplateGroupDirectory(dir).Build();
        Assert.AreEqual("foo", group.FindTemplate("a").Render());
        Assert.AreEqual("bar", group.FindTemplate("/subdir/b").Render());
        Assert.AreEqual("bar", group.FindTemplate("subdir/b").Render());
    }

    [TestMethod]
    public void TestSubdirWithSubtemplate() {
        // /randomdir/a and /randomdir/subdir/b
        var dir = TmpDir;
        WriteFile(Path.Combine(dir, "subdir"), "a.st", "a(x) ::= \"<x:{y|<y>}>\"");
        var group = _templateFactory.CreateTemplateGroupDirectory(dir).Build();
        var st = group.FindTemplate("/subdir/a");
        st.Add("x", new[] { "a", "b" });
        Assert.AreEqual("ab", st.Render());
    }

    [TestMethod]
    public void TestGroupFileInDir() {
        // /randomdir/a and /randomdir/group.stg with b and c templates
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a(x) ::= <<foo>>");
        var groupFile =
            "b() ::= \"bar\"\n" +
            "c() ::= \"duh\"\n";
        WriteFile(dir, "group.stg", groupFile);
        var group = _templateFactory.CreateTemplateGroupDirectory(dir).Build();
        Assert.AreEqual("foo", group.FindTemplate("a").Render());
        Assert.AreEqual("bar", group.FindTemplate("/group/b").Render());
        Assert.AreEqual("duh", group.FindTemplate("/group/c").Render());
    }

    [TestMethod]
    public void TestSubSubdir() {
        // /randomdir/a and /randomdir/subdir/b
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a(x) ::= <<foo>>");
        WriteFile(Path.Combine(dir, "sub1", "sub2"), "b.st", "b() ::= \"bar\"");
        var group = _templateFactory.CreateTemplateGroupDirectory(dir).Build();
        var st1 = group.FindTemplate("a");
        var st2 = group.FindTemplate("/sub1/sub2/b");
        const string expected = "foobar";
        var result = st1.Render() + st2.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestGroupFileInSubDir() {
        // /randomdir/a and /randomdir/group.stg with b and c templates
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a(x) ::= <<foo>>");
        const string groupFile =
            "b() ::= \"bar\"\n" +
            "c() ::= \"duh\"\n";
        WriteFile(dir, "subdir/group.stg", groupFile);
        var group = _templateFactory.CreateTemplateGroupDirectory(dir).Build();
        var st1 = group.FindTemplate("a");
        var st2 = group.FindTemplate("subdir/group/b");
        var st3 = group.FindTemplate("subdir/group/c");
        const string expected = "foobarduh";
        var result = st1.Render() + st2.Render() + st3.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDupDef() {
        var dir = TmpDir;
        const string groupFile =
            "b() ::= \"bar\"\n" +
            "b() ::= \"duh\"\n";
        WriteFile(dir, "group.stg", groupFile);
        ITemplateErrorListener errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "group.stg")).WithErrorListener(errors).Build();
        group.Load();
        var expected = $"group.stg 2:0: redefinition of template b{newline}";
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestAlias() {
        var dir = TmpDir;
        const string groupFile =
            "a() ::= \"bar\"\n" +
            "b ::= a\n";
        WriteFile(dir, "group.stg", groupFile);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "group.stg")).Build();
        var st = group.FindTemplate("b");
        var expected = "bar";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestAliasWithArgs() {
        var dir = TmpDir;
        const string groupFile =
            "a(x,y) ::= \"<x><y>\"\n" +
            "b ::= a\n";
        WriteFile(dir, "group.stg", groupFile);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "group.stg")).Build();
        var st = group.FindTemplate("b");
        st.Add("x", 1);
        st.Add("y", 2);
        const string expected = "12";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSimpleDefaultArg() {
        var dir = TmpDir;
        const string a = "a() ::= << <b()> >>\n";
        const string b = "b(x=\"foo\") ::= \"<x>\"\n";
        WriteFile(dir, "a.st", a);
        WriteFile(dir, "b.st", b);
        var group = _templateFactory.CreateTemplateGroupDirectory(dir).Build();
        var st = group.FindTemplate("a");
        const string expected = " foo ";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefaultArgument() {
        var templates =
                "method(name) ::= <<" + newline +
                "<stat(name)>" + newline +
                ">>" + newline +
                "stat(name,value=\"99\") ::= \"x=<value>; // <name>\"" + newline
            ;
        WriteFile(TmpDir, "group.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "group.stg")).Build();
        var b = group.FindTemplate("method");
        b.Add("name", "foo");
        const string expected = "x=99; // foo";
        var result = b.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestBooleanDefaultArguments() {
        var templates =
                "method(name) ::= <<" + newline +
                "<stat(name)>" + newline +
                ">>" + newline +
                "stat(name,x=true,y=false) ::= \"<name>; <x> <y>\"" + newline
            ;
        WriteFile(TmpDir, "group.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "group.stg")).Build();
        var b = group.FindTemplate("method");
        b.Add("name", "foo");
        const string expected = "foo; true false";
        var result = b.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefaultArgument2() {
        var templates = $"stat(name,value=\"99\") ::= \"x=<value>; // <name>\"{newline}";
        WriteFile(TmpDir, "group.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "group.stg")).Build();
        var b = group.FindTemplate("stat");
        b.Add("name", "foo");
        const string expected = "x=99; // foo";
        var result = b.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSubtemplateAsDefaultArgSeesOtherArgs() {
        var templates =
                "t(x,y={<x:{s|<s><z>}>},z=\"foo\") ::= <<\n" +
                "x: <x>\n" +
                "y: <y>\n" +
                ">>" + newline;
        WriteFile(TmpDir, "group.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "group.stg")).Build();
        var b = group.FindTemplate("t");
        b.Add("x", "a");
        var expected =
            "x: a" + newline +
            "y: afoo";
        var result = b.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEarlyEvalOfDefaultArgs() {
        const string templates =
            "s(x,y={<(x)>}) ::= \"<x><y>\"\n"; // should see x in def arg
        var group = _templateFactory.CreateTemplateGroupString(templates).Build();
        var b = group.FindTemplate("s");
        b.Add("x", "a");
        const string expected = "aa";
        var result = b.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefaultArgumentAsSimpleTemplate() {
        var templates = "stat(name,value={99}) ::= \"x=<value>; // <name>\"" + newline;
        WriteFile(TmpDir, "group.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "group.stg")).Build();
        var b = group.FindTemplate("stat");
        b.Add("name", "foo");
        const string expected = "x=99; // foo";
        var result = b.Render();
        Assert.AreEqual(expected, result);
    }

    private class Field {
        public string name = "parrt";
        public int n = 0;
        public override string ToString() {
            return "Field";
        }
    }

    [TestMethod]
    public void TestDefaultArgumentManuallySet() {
        // set arg f manually for stat(f=f)
        var templates =
                "method(fields) ::= <<" + newline +
                "<fields:{f | <stat(f)>}>" + newline +
                ">>" + newline +
                "stat(f,value={<f.name>}) ::= \"x=<value>; // <f.name>\"" + newline
            ;
        WriteFile(TmpDir, "group.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "group.stg")).Build();
        var m = group.FindTemplate("method");
        m.Add("fields", new Field());
        const string expected = "x=parrt; // parrt";
        var result = m.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefaultArgumentSeesVarFromDynamicScoping() {
        var templates =
                "method(fields) ::= <<" + newline +
                "<fields:{f | <stat()>}>" + newline +
                ">>" + newline +
                "stat(value={<f.name>}) ::= \"x=<value>; // <f.name>\"" + newline;
        WriteFile(TmpDir, "group.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "group.stg")).Build();
        var m = group.FindTemplate("method");
        m.Add("fields", new Field());
        const string expected = "x=parrt; // parrt";
        var result = m.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefaultArgumentImplicitlySet2() {
        // f of stat is implicit first arg
        var templates =
                "method(fields) ::= <<" + newline +
                "<fields:{f | <f:stat()>}>" + newline +
                ">>" + newline +
                "stat(f,value={<f.name>}) ::= \"x=<value>; // <f.name>\"" + newline;
        WriteFile(TmpDir, "group.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "group.stg")).Build();
        var m = group.FindTemplate("method");
        m.Add("fields", new Field());
        const string expected = "x=parrt; // parrt";
        var result = m.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefaultArgumentAsTemplate() {
        var templates =
                "method(name,size) ::= <<" + newline +
                "<stat(name)>" + newline +
                ">>" + newline +
                "stat(name,value={<name>}) ::= \"x=<value>; // <name>\"" + newline;
        WriteFile(TmpDir, "group.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "group.stg")).Build();
        var b = group.FindTemplate("method");
        b.Add("name", "foo");
        b.Add("size", "2");
        const string expected = "x=foo; // foo";
        var result = b.Render();
        //System.err.println("result='"+result+"'");
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefaultArgumentAsTemplate2() {
        var templates =
                "method(name,size) ::= <<" + newline +
                "<stat(name)>" + newline +
                ">>" + newline +
                "stat(name,value={ [<name>] }) ::= \"x=<value>; // <name>\"" + newline
            ;
        WriteFile(TmpDir, "group.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "group.stg")).Build();
        var b = group.FindTemplate("method");
        b.Add("name", "foo");
        b.Add("size", "2");
        const string expected = "x=[foo] ; // foo"; // won't see ' ' after '=' since it's an indent not simple string
        var result = b.Render();
        //System.err.println("result='"+result+"'");
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDoNotUseDefaultArgument() {
        var templates =
                "method(name) ::= <<" + newline +
                "<stat(name,\"34\")>" + newline +
                ">>" + newline +
                "stat(name,value=\"99\") ::= \"x=<value>; // <name>\"" + newline
            ;
        WriteFile(TmpDir, "group.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "group.stg")).Build();
        var b = group.FindTemplate("method");
        b.Add("name", "foo");
        const string expected = "x=34; // foo";
        var result = b.Render();
        Assert.AreEqual(expected, result);
    }

    /**
     * When the anonymous template specified as a default value for a formalArg
     * contains a syntax error Template 4.0.2 emits a NullPointerException error
     * (after the syntax error)
     *
     * @throws Exception
     */
    [TestMethod]
    public void TestHandleBuggyDefaultArgument() {
        var templates = "main(a={(<\"\")>}) ::= \"\"";
        WriteFile(TmpDir, "t.stg", templates);

        var errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).WithErrorListener(errors).Build();

        var st = group.FindTemplate("main");
        st.Render();

        // Check the errors. This contained an "NullPointerException" before
        Assert.AreEqual(
            "t.stg 1:12: mismatched input ')' expecting RDELIM" + newline,
            errors.ToString());
    }

    private class Counter {
        int n = 0;
        public override string ToString() {
            return (n++).ToString();
        }
    }

    [TestMethod]
    public void TestDefaultArgumentInParensToEvalEarly() {
        var templates =
            "A(x) ::= \"<B()>\"" + newline +
            "B(y={<(x)>}) ::= \"<y> <x> <x> <y>\"" + newline;
        WriteFile(TmpDir, "group.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "group.stg")).Build();
        var a = group.FindTemplate("A");
        a.Add("x", new Counter());
        const string expected = "0 1 2 0"; // trace must be false to get these numbers
        var result = a.Render();
        //System.err.println("result='"+result+"'");
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestTrueFalseArgs() {
        const string groupFile =
            "f(x,y) ::= \"<x><y>\"\n" +
            "g() ::= \"<f(true,{a})>\"";
        WriteFile(TmpDir, "group.stg", groupFile);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "group.stg")).Build();
        var st = group.FindTemplate("g");
        const string expected = "truea";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestNamedArgsInOrder() {
        var dir = TmpDir;
        const string groupFile =
            "f(x,y) ::= \"<x><y>\"\n" +
            "g() ::= \"<f(x={a},y={b})>\"";
        WriteFile(dir, "group.stg", groupFile);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "group.stg")).Build();
        var st = group.FindTemplate("g");
        const string expected = "ab";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestNamedArgsOutOfOrder() {
        var dir = TmpDir;
        const string groupFile =
            "f(x,y) ::= \"<x><y>\"\n" +
            "g() ::= \"<f(y={b},x={a})>\"";
        WriteFile(dir, "group.stg", groupFile);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "group.stg")).Build();
        var st = group.FindTemplate("g");
        const string expected = "ab";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestUnknownNamedArg() {
        var dir = TmpDir;
        const string groupFile =
            "f(x,y) ::= \"<x><y>\"\n" +
            "g() ::= \"<f(x={a},z={b})>\"";
        //012345678901234567

        WriteFile(dir, "group.stg", groupFile);
        var errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "group.stg")).WithErrorListener(errors).Build();
        var st = group.FindTemplate("g");
        st.Render();
        var expected = "context [/g] 1:1 attribute z isn't defined" + newline;
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestMissingNamedArg() {
        var dir = TmpDir;
        const string groupFile =
            "f(x,y) ::= \"<x><y>\"\n" +
            "g() ::= \"<f(x={a},{b})>\"";
        //012345678901234567

        WriteFile(dir, "group.stg", groupFile);
        var errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "group.stg")).WithErrorListener(errors).Build();
        group.Load();
        var expected = "group.stg 2:18: mismatched input '{' expecting ELLIPSIS" + newline;
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestNamedArgsNotAllowInIndirectInclude() {
        var dir = TmpDir;
        const string groupFile =
            "f(x,y) ::= \"<x><y>\"\n" +
            "g(name) ::= \"<(name)(x={a},y={b})>\"";
        //0123456789012345678901234567890
        WriteFile(dir, "group.stg", groupFile);
        var errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "group.stg")).WithErrorListener(errors).Build();
        group.Load();
        // TODO: this could be more informative about the incorrect use of named arguments
        var expected = "group.stg 2:22: '=' came as a complete surprise to me" + newline;
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestCantSeeGroupDirIfGroupFileOfSameName() {
        var dir = TmpDir;
        const string a = "a() ::= <<dir1 a>>\n";
        WriteFile(dir, "group/a.st", a); // can't see this file

        const string groupFile =
            "b() ::= \"group file b\"\n";
        WriteFile(dir, "group.stg", groupFile);

        var group1 = _templateFactory.CreateTemplateGroupDirectory(dir).Build();
        var st = group1.FindTemplate("group/a"); // can't see
        Assert.AreEqual(null, st);
    }

    [TestMethod]
    public void TestUnloadingSimpleGroup() {
        var dir = TmpDir;
        const string a = "a(x) ::= <<foo>>\n";
        const string b = "b() ::= <<bar>>\n";
        WriteFile(dir, "a.st", a);
        WriteFile(dir, "b.st", b);
        var group = _templateFactory.CreateTemplateGroupDirectory(dir).Build();
        group.Load(); // force load
        var st = group.FindTemplate("a");
        var originalHashCode = RuntimeHelpers.GetHashCode(st);
        group.Unload(); // blast cache
        st = group.FindTemplate("a");
        var newHashCode = RuntimeHelpers.GetHashCode(st);
        Assert.AreEqual(originalHashCode == newHashCode, false); // diff objects
        var expected = "foo";
        var result = st.Render();
        Assert.AreEqual(expected, result);
        st = group.FindTemplate("b");
        expected = "bar";
        result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestLoadInvalidTemplate() {
        var dir = TmpDir;
        WriteFile(dir, "invalid.st", "mismatched() ::= <<hello>>");
        var stg = _templateFactory.CreateTemplateGroupDirectory(dir).Build();
        Assert.IsFalse(stg.IsDefined("invalid"));
    }

    [TestMethod]
    public void TestUnloadingGroupFile() {
        var dir = TmpDir;
        const string a =
            "a(x) ::= <<foo>>\n" +
            "b() ::= <<bar>>\n";
        WriteFile(dir, "a.stg", a);
        var group = _templateFactory.CreateTemplateGroupFile(dir + "/a.stg").Build();
        group.Load(); // force load
        var st = group.FindTemplate("a");
        var originalHashCode = RuntimeHelpers.GetHashCode(st);
        group.Unload(); // blast cache
        st = group.FindTemplate("a");
        var newHashCode = RuntimeHelpers.GetHashCode(st);
        Assert.AreEqual(originalHashCode == newHashCode, false); // diff objects
        const string expected1 = "foo";
        var result = st.Render();
        Assert.AreEqual(expected1, result);
        st = group.FindTemplate("b");
        const string expected2 = "bar";
        result = st.Render();
        Assert.AreEqual(expected2, result);
    }

    [TestMethod]
    public void TestGroupFileImport() {
        // /randomdir/group1.stg (a template) and /randomdir/group2.stg with b.
        // group1 imports group2, a includes b
        var dir = TmpDir;
        const string groupFile1 =
            "import \"group2.stg\"\n" +
            "a(x) ::= <<\n" +
            "foo<b()>\n" +
            ">>\n";
        WriteFile(dir, "group1.stg", groupFile1);
        const string groupFile2 = "b() ::= \"bar\"\n";
        WriteFile(dir, "group2.stg", groupFile2);
        var group1 = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "group1.stg")).Build();

        // Is the imported template b found?
        var stb = group1.FindTemplate("b");
        Assert.AreEqual("bar", stb.Render());

        // Is the include of b() resolved?
        var sta = group1.FindTemplate("a");
        Assert.AreEqual("foobar", sta.Render());

        // Are the correct "ThatCreatedThisInstance" groups assigned
        Assert.AreEqual("group1", sta.Group.Name);
        Assert.AreEqual("group1", stb.Group.Name);

        // Are the correct (native) groups assigned for the templates
        Assert.AreEqual("group1", sta.GetCompiledTemplate().NativeGroup.Name);
        Assert.AreEqual("group2", stb.GetCompiledTemplate().NativeGroup.Name);
    }

    [TestMethod]
    public void TestGetTemplateNames() {
        var templates =
            "t() ::= \"foo\"\n" +
            "main() ::= \"<t()>\"";
        WriteFile(TmpDir, "t.stg", templates);

        var group = _templateFactory.CreateTemplateGroupFile(TmpDir + "/t.stg").Build();
        // try to get an undefined template.
        // This will add an entry to the "templates" field in STGroup, however
        // this should not be returned.
        group.FindTemplate("t2");

        var names = group.TemplateNames;

        // Should only contain "t" and "main" (not "t2")
        Assert.AreEqual(2, names.Count);
        CollectionAssert.Contains(names.ToList(), "/t");
        CollectionAssert.Contains(names.ToList(), "/main");
    }

    [TestMethod]
    public void TestUnloadWithImports() {
        WriteFile(TmpDir, "t.stg",
            "import \"g1.stg\"\n\nmain() ::= <<\nv1-<f()>\n>>");
        WriteFile(TmpDir, "g1.stg", "f() ::= \"g1\"");
        WriteFile(TmpDir, "g2.stg", "f() ::= \"g2\"\nf2() ::= \"f2\"\n");
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var st = group.FindTemplate("main");
        Assert.AreEqual("v1-g1", st.Render());

        // Change the text of group t, including the imports.
        WriteFile(TmpDir, "t.stg", "import \"g2.stg\"\n\nmain() ::= <<\nv2-<f()>;<f2()>\n>>");
        group.Unload();
        st = group.FindTemplate("main");
        Assert.AreEqual("v2-g2;f2", st.Render());
    }

    [TestMethod]
    public void TestLineBreakInGroup() {
        var templates =
            "t() ::= <<" + newline +
            "Foo <\\\\>" + newline +
            "  \t  bar" + newline +
            ">>" + newline;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(TmpDir + Path.DirectorySeparatorChar + "t.stg").Build();
        var st = group.FindTemplate("t");
        Assert.IsNotNull(st);
        const string expected = "Foo bar";     // expect \n in output
        Assert.AreEqual(expected, st.Render());
    }

    [TestMethod]
    public void TestLineBreakInGroup2() {
        var templates =
            "t() ::= <<" + newline +
            "Foo <\\\\>       " + newline +
            "  \t  bar" + newline +
            ">>" + newline;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(TmpDir + Path.DirectorySeparatorChar + "t.stg").Build();
        var st = group.FindTemplate("t");
        Assert.IsNotNull(st);
        const string expected = "Foo bar";     // expect \n in output
        Assert.AreEqual(expected, st.Render());
    }

    [TestMethod]
    public void TestLineBreakMissingTrailingNewline() {
        WriteFile(TmpDir, "t.stg", "a(x) ::= <<<\\\\>\r\n>>"); // that is <<<\\>>> not an escaped >>
        var errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(TmpDir + "/" + "t.stg").WithErrorListener(errors).Build();
        var st = group.FindTemplate("a");
        Assert.AreEqual("t.stg 1:15: Missing newline after newline escape <\\\\>" + newline, errors.ToString());
        st.Add("x", "parrt");
        var result = st.Render();
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void TestLineBreakWithScarfedTrailingNewline() {
        WriteFile(TmpDir, "t.stg", "a(x) ::= <<<\\\\>\r\n>>"); // \r\n removed as trailing whitespace
        var errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(TmpDir + "/" + "t.stg").WithErrorListener(errors).Build();
        var st = group.FindTemplate("a");
        Assert.AreEqual("t.stg 1:15: Missing newline after newline escape <\\\\>" + newline, errors.ToString());
        st.Add("x", "parrt");
        var result = st.Render();
        Assert.AreEqual(string.Empty, result);
    }

}
