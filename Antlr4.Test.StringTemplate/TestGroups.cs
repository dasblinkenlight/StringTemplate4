﻿/*
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
using Antlr4.StringTemplate.Misc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Runtime.CompilerServices;
using Path = System.IO.Path;

[TestClass]
public class TestGroups : BaseTest {

    [TestMethod]
    public void TestSimpleGroup() {
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a(x) ::= <<foo>>");
        var group = new TemplateGroupDirectory(dir);
        var st = group.GetInstanceOf("a");
        const string expected = "foo";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEscapeOneRightAngle() {
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a(x) ::= << > >>");
        var group = new TemplateGroupDirectory(dir);
        var st = group.GetInstanceOf("a");
        st.Add("x", "parrt");
        const string expected = " > ";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEscapeJavaRightShift() {
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a(x) ::= << \\>> >>");
        var group = new TemplateGroupDirectory(dir);
        var st = group.GetInstanceOf("a");
        st.Add("x", "parrt");
        const string expected = " >> ";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEscapeJavaRightShift2() {
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a(x) ::= << >\\> >>");
        var group = new TemplateGroupDirectory(dir);
        var st = group.GetInstanceOf("a");
        st.Add("x", "parrt");
        const string expected = " >> ";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEscapeJavaRightShiftAtRightEdge() {
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a(x) ::= <<\\>>>"); // <<\>>>
        var group = new TemplateGroupDirectory(dir);
        var st = group.GetInstanceOf("a");
        st.Add("x", "parrt");
        const string expected = "\\>";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEscapeJavaRightShiftAtRightEdge2() {
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a(x) ::= <<>\\>>>");
        var group = new TemplateGroupDirectory(dir);
        var st = group.GetInstanceOf("a");
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
        var group = new TemplateGroupString(g);
        var st = group.GetInstanceOf("a");
        const string expected = "foo";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestGroupWithTwoTemplates() {
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a(x) ::= <<foo>>");
        WriteFile(dir, "b.st", "b() ::= \"bar\"");
        var group = new TemplateGroupDirectory(dir);
        var st1 = group.GetInstanceOf("a");
        var st2 = group.GetInstanceOf("b");
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
        var group = new TemplateGroupDirectory(dir);
        Assert.AreEqual("foo", group.GetInstanceOf("a").Render());
        Assert.AreEqual("bar", group.GetInstanceOf("/subdir/b").Render());
        Assert.AreEqual("bar", group.GetInstanceOf("subdir/b").Render());
    }

    [TestMethod]
    public void TestSubdirWithSubtemplate() {
        // /randomdir/a and /randomdir/subdir/b
        var dir = TmpDir;
        WriteFile(Path.Combine(dir, "subdir"), "a.st", "a(x) ::= \"<x:{y|<y>}>\"");
        var group = new TemplateGroupDirectory(dir);
        var st = group.GetInstanceOf("/subdir/a");
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
        var group = new TemplateGroupDirectory(dir);
        Assert.AreEqual("foo", group.GetInstanceOf("a").Render());
        Assert.AreEqual("bar", group.GetInstanceOf("/group/b").Render());
        Assert.AreEqual("duh", group.GetInstanceOf("/group/c").Render());
    }

    [TestMethod]
    public void TestSubSubdir() {
        // /randomdir/a and /randomdir/subdir/b
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a(x) ::= <<foo>>");
        WriteFile(Path.Combine(dir, "sub1", "sub2"), "b.st", "b() ::= \"bar\"");
        var group = new TemplateGroupDirectory(dir);
        var st1 = group.GetInstanceOf("a");
        var st2 = group.GetInstanceOf("/sub1/sub2/b");
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
        var group = new TemplateGroupDirectory(dir);
        var st1 = group.GetInstanceOf("a");
        var st2 = group.GetInstanceOf("subdir/group/b");
        var st3 = group.GetInstanceOf("subdir/group/c");
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
        var group = new TemplateGroupFile(Path.Combine(dir, "group.stg")) {
            Listener = errors
        };
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
        var group = new TemplateGroupFile(Path.Combine(dir, "group.stg"));
        var st = group.GetInstanceOf("b");
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
        var group = new TemplateGroupFile(Path.Combine(dir, "group.stg"));
        var st = group.GetInstanceOf("b");
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
        var group = new TemplateGroupDirectory(dir);
        var st = group.GetInstanceOf("a");
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
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "group.stg"));
        var b = group.GetInstanceOf("method");
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
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "group.stg"));
        var b = group.GetInstanceOf("method");
        b.Add("name", "foo");
        const string expected = "foo; true false";
        var result = b.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefaultArgument2() {
        var templates = $"stat(name,value=\"99\") ::= \"x=<value>; // <name>\"{newline}";
        WriteFile(TmpDir, "group.stg", templates);
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "group.stg"));
        var b = group.GetInstanceOf("stat");
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
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "group.stg"));
        var b = group.GetInstanceOf("t");
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
        var group = new TemplateGroupString(templates);
        var b = group.GetInstanceOf("s");
        b.Add("x", "a");
        const string expected = "aa";
        var result = b.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefaultArgumentAsSimpleTemplate() {
        var templates = "stat(name,value={99}) ::= \"x=<value>; // <name>\"" + newline;
        WriteFile(TmpDir, "group.stg", templates);
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "group.stg"));
        var b = group.GetInstanceOf("stat");
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
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "group.stg"));
        var m = group.GetInstanceOf("method");
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
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "group.stg"));
        var m = group.GetInstanceOf("method");
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
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "group.stg"));
        var m = group.GetInstanceOf("method");
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
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "group.stg"));
        var b = group.GetInstanceOf("method");
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
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "group.stg"));
        var b = group.GetInstanceOf("method");
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
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "group.stg"));
        var b = group.GetInstanceOf("method");
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
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "t.stg")) {
            Listener = errors
        };

        var st = group.GetInstanceOf("main");
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
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "group.stg"));
        var a = group.GetInstanceOf("A");
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
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "group.stg"));
        var st = group.GetInstanceOf("g");
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
        var group = new TemplateGroupFile(Path.Combine(dir, "group.stg"));
        var st = group.GetInstanceOf("g");
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
        var group = new TemplateGroupFile(Path.Combine(dir, "group.stg"));
        var st = group.GetInstanceOf("g");
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
        var group = new TemplateGroupFile(Path.Combine(dir, "group.stg"));
        var errors = new ErrorBuffer();
        group.Listener = errors;
        var st = group.GetInstanceOf("g");
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
        var group = new TemplateGroupFile(Path.Combine(dir, "group.stg"));
        var errors = new ErrorBuffer();
        group.Listener = errors;
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
        var group = new TemplateGroupFile(Path.Combine(dir, "group.stg"));
        var errors = new ErrorBuffer();
        group.Listener = errors;
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

        var group1 = new TemplateGroupDirectory(dir);
        var st = group1.GetInstanceOf("group/a"); // can't see
        Assert.AreEqual(null, st);
    }

    [TestMethod]
    public void TestUnloadingSimpleGroup() {
        var dir = TmpDir;
        const string a = "a(x) ::= <<foo>>\n";
        const string b = "b() ::= <<bar>>\n";
        WriteFile(dir, "a.st", a);
        WriteFile(dir, "b.st", b);
        var group = new TemplateGroupDirectory(dir);
        group.Load(); // force load
        var st = group.GetInstanceOf("a");
        var originalHashCode = RuntimeHelpers.GetHashCode(st);
        group.Unload(); // blast cache
        st = group.GetInstanceOf("a");
        var newHashCode = RuntimeHelpers.GetHashCode(st);
        Assert.AreEqual(originalHashCode == newHashCode, false); // diff objects
        var expected = "foo";
        var result = st.Render();
        Assert.AreEqual(expected, result);
        st = group.GetInstanceOf("b");
        expected = "bar";
        result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestLoadInvalidTemplate() {
        var dir = TmpDir;
        WriteFile(dir, "invalid.st", "mismatched() ::= <<hello>>");
        var stg = new TemplateGroupDirectory(dir);
        Assert.IsFalse(stg.IsDefined("invalid"));
    }

    [TestMethod]
    public void TestUnloadingGroupFile() {
        var dir = TmpDir;
        const string a =
            "a(x) ::= <<foo>>\n" +
            "b() ::= <<bar>>\n";
        WriteFile(dir, "a.stg", a);
        var group = new TemplateGroupFile(dir + "/a.stg");
        group.Load(); // force load
        var st = group.GetInstanceOf("a");
        var originalHashCode = RuntimeHelpers.GetHashCode(st);
        group.Unload(); // blast cache
        st = group.GetInstanceOf("a");
        var newHashCode = RuntimeHelpers.GetHashCode(st);
        Assert.AreEqual(originalHashCode == newHashCode, false); // diff objects
        const string expected1 = "foo";
        var result = st.Render();
        Assert.AreEqual(expected1, result);
        st = group.GetInstanceOf("b");
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
        var group1 = new TemplateGroupFile(Path.Combine(dir, "group1.stg"));

        // Is the imported template b found?
        var stb = group1.GetInstanceOf("b");
        Assert.AreEqual("bar", stb.Render());

        // Is the include of b() resolved?
        var sta = group1.GetInstanceOf("a");
        Assert.AreEqual("foobar", sta.Render());

        // Are the correct "ThatCreatedThisInstance" groups assigned
        Assert.AreEqual("group1", sta.Group.Name);
        Assert.AreEqual("group1", stb.Group.Name);

        // Are the correct (native) groups assigned for the templates
        Assert.AreEqual("group1", sta.impl.NativeGroup.Name);
        Assert.AreEqual("group2", stb.impl.NativeGroup.Name);
    }

    [TestMethod]
    public void TestGetTemplateNames() {
        var templates =
            "t() ::= \"foo\"\n" +
            "main() ::= \"<t()>\"";
        WriteFile(TmpDir, "t.stg", templates);

        var group = new TemplateGroupFile(TmpDir + "/t.stg");
        // try to get an undefined template.
        // This will add an entry to the "templates" field in STGroup, however
        // this should not be returned.
        group.LookupTemplate("t2");

        var names = group.GetTemplateNames();

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
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "t.stg"));
        var st = group.GetInstanceOf("main");
        Assert.AreEqual("v1-g1", st.Render());

        // Change the text of group t, including the imports.
        WriteFile(TmpDir, "t.stg", "import \"g2.stg\"\n\nmain() ::= <<\nv2-<f()>;<f2()>\n>>");
        group.Unload();
        st = group.GetInstanceOf("main");
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
        var group = new TemplateGroupFile(TmpDir + Path.DirectorySeparatorChar + "t.stg");
        var st = group.GetInstanceOf("t");
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
        var group = new TemplateGroupFile(TmpDir + Path.DirectorySeparatorChar + "t.stg");
        var st = group.GetInstanceOf("t");
        Assert.IsNotNull(st);
        const string expected = "Foo bar";     // expect \n in output
        Assert.AreEqual(expected, st.Render());
    }

    [TestMethod]
    public void TestLineBreakMissingTrailingNewline() {
        WriteFile(TmpDir, "t.stg", "a(x) ::= <<<\\\\>\r\n>>"); // that is <<<\\>>> not an escaped >>
        var errors = new ErrorBuffer();
        var group = new TemplateGroupFile(TmpDir + "/" + "t.stg") {
            Listener = errors
        };
        var st = group.GetInstanceOf("a");
        Assert.AreEqual("t.stg 1:15: Missing newline after newline escape <\\\\>" + newline, errors.ToString());
        st.Add("x", "parrt");
        var result = st.Render();
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void TestLineBreakWithScarfedTrailingNewline() {
        WriteFile(TmpDir, "t.stg", "a(x) ::= <<<\\\\>\r\n>>"); // \r\n removed as trailing whitespace
        var errors = new ErrorBuffer();
        var group = new TemplateGroupFile(TmpDir + "/" + "t.stg") {
            Listener = errors
        };
        var st = group.GetInstanceOf("a");
        Assert.AreEqual("t.stg 1:15: Missing newline after newline escape <\\\\>" + newline, errors.ToString());
        st.Add("x", "parrt");
        var result = st.Render();
        Assert.AreEqual(string.Empty, result);
    }

}
