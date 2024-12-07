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

using Antlr4.StringTemplate.Debug;

namespace Antlr4.Test.StringTemplate;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Path = System.IO.Path;

[TestClass]
public class TestIndentation : BaseTest {

    [TestMethod]
    public void TestIndentInFrontOfTwoExpr() {
        var templates =
            "list(a,b) ::= <<" +
            "  <a><b>" + newline +
            ">>" + newline;

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var t = group.FindTemplate("list");
        TestContext.WriteLine(t.GetCompiledTemplate().ToString());
        t.Add("a", "Terence");
        t.Add("b", "Jim");
        const string expected = "  TerenceJim";
        Assert.AreEqual(expected, t.Render());
    }

    [TestMethod]
    public void TestSimpleIndentOfAttributeList() {
        var templates =
            "list(names) ::= <<" +
            "  <names; separator=\"\\n\">" + newline +
            ">>" + newline;

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var t = group.FindTemplate("list");
        t.Add("names", "Terence");
        t.Add("names", "Jim");
        t.Add("names", "Sriram");
        var expected =
            "  Terence" + newline +
            "  Jim" + newline +
            "  Sriram";
        Assert.AreEqual(expected, t.Render());
    }

    [TestMethod]
    public void TestIndentOfMultilineAttributes() {
        var templates =
            "list(names) ::= <<" +
            "  <names; separator=\"\n\">" + newline +
            ">>" + newline;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var t = group.FindTemplate("list");
        t.Add("names", "Terence\nis\na\nmaniac");
        t.Add("names", "Jim");
        t.Add("names", "Sriram\nis\ncool");
        var expected =
            "  Terence" + newline +
            "  is" + newline +
            "  a" + newline +
            "  maniac" + newline +
            "  Jim" + newline +
            "  Sriram" + newline +
            "  is" + newline +
            "  cool";
        Assert.AreEqual(expected, t.Render());
    }

    [TestMethod]
    public void TestIndentOfMultipleBlankLines() {
        var templates =
            "list(names) ::= <<" +
            "  <names>" + newline +
            ">>" + newline;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var t = group.FindTemplate("list");
        t.Add("names", "Terence\n\nis a maniac");
        var expected =
            "  Terence" + newline +
            "" + newline + // no indent on blank line
            "  is a maniac";
        Assert.AreEqual(expected, t.Render());
    }

    [TestMethod]
    public void TestIndentBetweenLeftJustifiedLiterals() {
        var templates =
            "list(names) ::= <<" +
            "Before:" + newline +
            "  <names; separator=\"\\n\">" + newline +
            "after" + newline +
            ">>" + newline;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var t = group.FindTemplate("list");
        t.Add("names", "Terence");
        t.Add("names", "Jim");
        t.Add("names", "Sriram");
        var expected =
            "Before:" + newline +
            "  Terence" + newline +
            "  Jim" + newline +
            "  Sriram" + newline +
            "after";
        Assert.AreEqual(expected, t.Render());
    }

    [TestMethod]
    public void TestNestedIndent() {
        var templates =
                "method(name,stats) ::= <<" +
                "void <name>() {" + newline +
                "\t<stats; separator=\"\\n\">" + newline +
                "}" + newline +
                ">>" + newline +
                "ifstat(expr,stats) ::= <<" + newline +
                "if (<expr>) {" + newline +
                "  <stats; separator=\"\\n\">" + newline +
                "}" +
                ">>" + newline +
                "assign(lhs,expr) ::= <<<lhs>=<expr>;>>" + newline
            ;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var t = group.FindTemplate("method");
        t.Add("name", "foo");
        var s1 = group.FindTemplate("assign");
        s1.Add("lhs", "x");
        s1.Add("expr", "0");
        var s2 = group.FindTemplate("ifstat");
        s2.Add("expr", "x>0");
        var s2a = group.FindTemplate("assign");
        s2a.Add("lhs", "y");
        s2a.Add("expr", "x+y");
        var s2b = group.FindTemplate("assign");
        s2b.Add("lhs", "z");
        s2b.Add("expr", "4");
        s2.Add("stats", s2a);
        s2.Add("stats", s2b);
        t.Add("stats", s1);
        t.Add("stats", s2);
        var expected =
            "void foo() {" + newline +
            "\tx=0;" + newline +
            "\tif (x>0) {" + newline +
            "\t  y=x+y;" + newline +
            "\t  z=4;" + newline +
            "\t}" + newline +
            "}";
        Assert.AreEqual(expected, t.Render());
    }

    [TestMethod]
    public void TestIndentedIFWithValueExpr() {
        var t = _templateFactory.CreateTemplate(
            "begin" + newline +
            "    <if(x)>foo<endif>" + newline +
            "end" + newline);
        t.Add("x", "x");
        var expected =
            "begin" + newline +
            "    foo" + newline +
            "end" + newline;
        var result = t.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestIndentedIFWithElse() {
        var t = _templateFactory.CreateTemplate(
            "begin" + newline +
            "    <if(x)>foo<else>bar<endif>" + newline +
            "end" + newline);
        t.Add("x", "x");
        var expected = "begin" + newline + "    foo" + newline + "end" + newline;
        var result = t.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestIndentedIFWithElse2() {
        var t = _templateFactory.CreateTemplate(
            "begin" + newline +
            "    <if(x)>foo<else>bar<endif>" + newline +
            "end" + newline);
        t.Add("x", false);
        var expected = "begin" + newline + "    bar" + newline + "end" + newline;
        var result = t.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestIndentedIFWithNewlineBeforeText() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("t",
            "begin" + newline +
            "    <if(x)>\n" +
            "foo\n" +  // no indent; ignore IF indent
            "    <endif>" + newline +	  // ignore indent on if-tags on lines by themselves
            "end" + newline,
            ["x"]);
        var t = group.FindTemplate("t");
        t.Add("x", "x");
        var expected = "begin" + newline + "foo" + newline + "end";
        var result = t.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestIndentedIFWithEndifNextLine() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("t",
            "begin" + newline +
            "    <if(x)>foo\n" +      // use indent and keep newline
            "    <endif>" + newline +	  // ignore indent on if-tags on lines by themselves
            "end" + newline,
            ["x"]);
        var t = group.FindTemplate("t");
        t.Add("x", "x");
        var expected = "begin" + newline + "    foo" + newline + "end";
        var result = t.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestIFWithIndentOnMultipleLines() {
        var t = _templateFactory.CreateTemplate(
            "begin" + newline +
            "   <if(x)>" + newline +
            "   foo" + newline +
            "   <else>" + newline +
            "   bar" + newline +
            "   <endif>" + newline +
            "end" + newline);
        var expected =
            "begin" + newline +
            "   bar" + newline +
            "end" + newline;
        var result = t.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestIFWithIndentAndExprOnMultipleLines() {
        var t = _templateFactory.CreateTemplate(
            "begin" + newline +
            "   <if(x)>" + newline +
            "   <x>" + newline +
            "   <else>" + newline +
            "   <y>" + newline +
            "   <endif>" + newline +
            "end" + newline);
        t.Add("y", "y");
        var expected =
            "begin" + newline +
            "   y" + newline +
            "end" + newline;
        var result = t.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestIFWithIndentAndExprWithIndentOnMultipleLines() {
        var t = _templateFactory.CreateTemplate(
            "begin" + newline +
            "   <if(x)>" + newline +
            "     <x>" + newline +
            "   <else>" + newline +
            "     <y>" + newline +
            "   <endif>" + newline +
            "end" + newline);
        t.Add("y", "y");
        var expected =
            "begin" + newline +
            "     y" + newline +
            "end" + newline;
        var result = t.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestNestedIFWithIndentOnMultipleLines() {
        var t = _templateFactory.CreateTemplate(
            "begin" + newline +
            "   <if(x)>" + newline +
            "      <if(y)>" + newline +
            "      foo" + newline +
            "      <endif>" + newline +
            "   <else>" + newline +
            "      <if(z)>" + newline +
            "      foo" + newline +
            "      <endif>" + newline +
            "   <endif>" + newline +
            "end" + newline);
        t.Add("x", "x");
        t.Add("y", "y");
        var expected =
            "begin" + newline +
            "      foo" + newline +
            "end" + newline; // no indent
        var result = t.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestIFInSubtemplate() {
        var t = _templateFactory.CreateTemplate(
            "<names:{n |" + newline +
            "   <if(x)>" + newline +
            "   <x>" + newline +
            "   <else>" + newline +
            "   <y>" + newline +
            "   <endif>" + newline +
            "}>" + newline);
        t.Add("names", "Ter");
        t.Add("y", "y");
        var expected = "   y" + newline + newline;
        var result = t.Render();
        Assert.AreEqual(expected, result);
    }

}
