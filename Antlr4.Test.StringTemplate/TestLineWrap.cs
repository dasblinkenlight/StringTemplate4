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

namespace Antlr4.Test.StringTemplate;

using System.Collections.Generic;
using Antlr4.StringTemplate;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Path = System.IO.Path;
using StringWriter = System.IO.StringWriter;

[TestClass]
public class TestLineWrap : BaseTest {

    [TestMethod]
    public void TestLineWrap1() {
        var templates =
            "array(values) ::= <<int[] a = { <values; wrap=\"\\n\", separator=\",\"> };>>" + newline;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();

        var a = group.FindTemplate("array");
        a.Add("values", new [] {
            3,9,20,2,1,4,6,32,5,6,77,888,2,1,6,32,5,6,77,
            4,9,20,2,1,4,63,9,20,2,1,4,6,32,5,6,77,6,32,5,6,77,
            3,9,20,2,1,4,6,32,5,6,77,888,1,6,32,5});
        const string expecting =
            "int[] a = { 3,9,20,2,1,4,6,32,5,6,77,888,\n" +
            "2,1,6,32,5,6,77,4,9,20,2,1,4,63,9,20,2,1,\n" +
            "4,6,32,5,6,77,6,32,5,6,77,3,9,20,2,1,4,6,\n" +
            "32,5,6,77,888,1,6,32,5 };";

        var sw = new StringWriter();
        ITemplateWriter stw = new AutoIndentWriter(sw, "\n"); // force \n as newline
        stw.LineWidth = 40;
        a.Write(stw);
        var result = sw.ToString();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestLineWrapAnchored() {
        var templates =
            "array(values) ::= <<int[] a = { <values; anchor, wrap, separator=\",\"> };>>" + newline;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();

        var a = group.FindTemplate("array");
        a.Add("values", new[] {
            3,9,20,2,1,4,6,32,5,6,77,888,2,1,6,32,5,6,77,
            4,9,20,2,1,4,63,9,20,2,1,4,6,32,5,6,77,6,32,5,6,77,
            3,9,20,2,1,4,6,32,5,6,77,888,1,6,32,5});
        var expecting =
            "int[] a = { 3,9,20,2,1,4,6,32,5,6,77,888," +     newline +
            "            2,1,6,32,5,6,77,4,9,20,2,1,4," +     newline +
            "            63,9,20,2,1,4,6,32,5,6,77,6," +     newline +
            "            32,5,6,77,3,9,20,2,1,4,6,32," +     newline +
            "            5,6,77,888,1,6,32,5 };";
        Assert.AreEqual(expecting, a.Render(40));
    }

    [TestMethod]
    public void TestSubtemplatesAnchorToo() {
        var templates =
            "array(values) ::= <<{ <values; anchor, separator=\", \"> }>>" + newline;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();

        var x = _templateFactory.CreateTemplate("<\\n>{ <stuff; anchor, separator=\",\\n\"> }<\\n>");
        x.SetGroup(group);
        x.Add("stuff", "1");
        x.Add("stuff", "2");
        x.Add("stuff", "3");
        var a = group.FindTemplate("array");
        a.Add("values", new List<object>() { "a", x, "b" });
        var expecting =
            "{ a, " +     newline +
            "  { 1," +     newline +
            "    2," +     newline +
            "    3 }" +     newline +
            "  , b }";
        Assert.AreEqual(expecting, a.Render(40));
    }

    [TestMethod]
    public void TestFortranLineWrap() {
        var templates =
            "Function(args) ::= <<       FUNCTION line( <args; wrap=\"\\n      c\", separator=\",\"> )>>" + newline;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();

        var a = group.FindTemplate("Function");
        a.Add("args", new[] { "a", "b", "c", "d", "e", "f" });
        var expecting =
            "       FUNCTION line( a,b,c,d," +     newline +
            "      ce,f )";
        Assert.AreEqual(expecting, a.Render(30));
    }

    [TestMethod]
    public void TestLineWrapWithDiffAnchor() {
        var templates =
            "array(values) ::= <<int[] a = { <{1,9,2,<values; wrap, separator=\",\">}; anchor> };>>" + newline;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();

        var a = group.FindTemplate("array");
        a.Add("values", new[] {
            3,9,20,2,1,4,6,32,5,6,77,888,2,1,6,32,5,6,77,
            4,9,20,2,1,4,63,9,20,2,1,4,6});
        var expecting =
            "int[] a = { 1,9,2,3,9,20,2,1,4," +     newline +
            "            6,32,5,6,77,888,2," +     newline +
            "            1,6,32,5,6,77,4,9," +     newline +
            "            20,2,1,4,63,9,20,2," +     newline +
            "            1,4,6 };";
        Assert.AreEqual(expecting, a.Render(30));
    }

    [TestMethod]
    public void TestLineWrapEdgeCase() {
        var templates =
            "duh(chars) ::= \"<chars; wrap={<\\n>}>\"" + newline;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();

        var a = group.FindTemplate("duh");
        a.Add("chars", new[] { "a", "b", "c", "d", "e" });
        // lineWidth==3 implies that we can have 3 characters at most
        var expecting =
            "abc" +     newline +
            "de";
        Assert.AreEqual(expecting, a.Render(3));
    }

    [TestMethod]
    public void TestLineWrapLastCharIsNewline() {
        var templates =
            "duh(chars) ::= <<" + newline +
            "<chars; wrap=\"\\n\"\\>" + newline +
            ">>" + newline;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();

        var a = group.FindTemplate("duh");
        a.Add("chars", new[] { "a", "b", "\n", "d", "e" });
        // don't do \n if it's last element anyway
        var expecting =
            "ab" +     newline +
            "de";
        Assert.AreEqual(expecting, a.Render(3));
    }

    [TestMethod]
    public void TestLineWrapCharAfterWrapIsNewline() {
        var templates =
            "duh(chars) ::= <<" + newline +
            "<chars; wrap=\"\\n\"\\>" + newline +
            ">>" + newline;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();

        var a = group.FindTemplate("duh");
        a.Add("chars", new[] { "a", "b", "c", "\n", "d", "e" });
        // Once we wrap, we must dump chars as we see them.  A newline right
        // after a wrap is just an "unfortunate" event.  People will expect
        // a newline if it's in the data.
        var expecting =
            "abc" + newline +
            ""    + newline +
            "de";
        Assert.AreEqual(expecting, a.Render(3));
    }

    [TestMethod]
    public void TestLineWrapForList() {
        var templates =
            "duh(data) ::= <<!<data; wrap>!>>" + newline;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();

        var a = group.FindTemplate("duh");
        a.Add("data", new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });
        var expecting =
            "!123" + newline +
            "4567" + newline +
            "89!";
        Assert.AreEqual(expecting, a.Render(4));
    }

    [TestMethod]
    public void TestLineWrapForAnonTemplate() {
        var templates =
            "duh(data) ::= <<!<data:{v|[<v>]}; wrap>!>>" + newline;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();

        var a = group.FindTemplate("duh");
        a.Add("data", new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });
        var expecting =
            "![1][2][3]" + newline + // width=9 is the 3 char; don't break til after ]
            "[4][5][6]"  + newline +
            "[7][8][9]!";
        Assert.AreEqual(expecting, a.Render(9));
    }

    [TestMethod]
    public void TestLineWrapForAnonTemplateAnchored() {
        var templates =
            "duh(data) ::= <<!<data:{v|[<v>]}; anchor, wrap>!>>" + newline;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();

        var a = group.FindTemplate("duh");
        a.Add("data", new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });
        var expecting =
            "![1][2][3]" +     newline +
            " [4][5][6]" +     newline +
            " [7][8][9]!";
        Assert.AreEqual(expecting, a.Render(9));
    }

    [TestMethod]
    public void TestLineWrapForAnonTemplateComplicatedWrap() {
        var templates =
            "top(s) ::= <<  <s>.>>" +
            "str(data) ::= <<!<data:{v|[<v>]}; wrap=\"!+\\n!\">!>>" + newline;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();

        var t = group.FindTemplate("top");
        var s = group.FindTemplate("str");
        s.Add("data", new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });
        t.Add("s", s);
        var expecting =
            "  ![1][2]!+" +     newline +
            "  ![3][4]!+" +     newline +
            "  ![5][6]!+" +     newline +
            "  ![7][8]!+" +     newline +
            "  ![9]!.";
        Assert.AreEqual(expecting, t.Render(9));
    }

    [TestMethod]
    public void TestIndentBeyondLineWidth() {
        var templates =
            "duh(chars) ::= <<" + newline +
            "    <chars; wrap=\"\\n\">" + newline +
            ">>" + newline;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();

        var a = group.FindTemplate("duh");
        a.Add("chars", new[] { "a", "b", "c", "d", "e" });
        //
        var expecting =
            "    a" +     newline +
            "    b" +     newline +
            "    c" +     newline +
            "    d" +     newline +
            "    e";
        Assert.AreEqual(expecting, a.Render(2));
    }

    [TestMethod]
    public void TestIndentedExpr() {
        var templates =
            "duh(chars) ::= <<" + newline +
            "    <chars; wrap=\"\\n\">" + newline +
            ">>" + newline;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();

        var a = group.FindTemplate("duh");
        a.Add("chars", new[] { "a", "b", "c", "d", "e" });
        //
        var expecting =
            "    ab" +     newline +
            "    cd" +     newline +
            "    e";
        // width=4 spaces + 2 char.
        Assert.AreEqual(expecting, a.Render(6));
    }

    [TestMethod]
    public void TestNestedIndentedExpr() {
        var templates =
            "top(d) ::= <<  <d>!>>" + newline +
            "duh(chars) ::= <<" + newline +
            "  <chars; wrap=\"\\n\">" + newline +
            ">>" + newline;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();

        var top = group.FindTemplate("top");
        var duh = group.FindTemplate("duh");
        duh.Add("chars", new[] { "a", "b", "c", "d", "e" });
        top.Add("d", duh);
        var expecting =
            "    ab" +     newline +
            "    cd" +     newline +
            "    e!";
        // width=4 spaces + 2 char.
        Assert.AreEqual(expecting, top.Render(6));
    }

    [TestMethod]
    public void TestNestedWithIndentAndTrackStartOfExpr() {
        var templates =
            "top(d) ::= <<  <d>!>>" + newline +
            "duh(chars) ::= <<" + newline +
            "x: <chars; anchor, wrap=\"\\n\">" + newline +
            ">>" + newline;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();

        var top = group.FindTemplate("top");
        var duh = group.FindTemplate("duh");
        duh.Add("chars", new[] { "a", "b", "c", "d", "e" });
        top.Add("d", duh);
        //
        var expecting =
            "  x: ab" +     newline +
            "     cd" +     newline +
            "     e!";
        Assert.AreEqual(expecting, top.Render(7));
    }

    [TestMethod]
    public void TestLineDoesNotWrapDueToLiteral() {
        var templates =
            "m(args,body) ::= <<[TestMethod] public voidfoo(<args; wrap=\"\\n\",separator=\", \">) throws Ick { <body> }>>" + newline;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();

        var a = group.FindTemplate("m");
        a.Add("args", new[] { "a", "b", "c" });
        a.Add("body", "i=3;");
        // make it wrap because of ") throws Ick { " literal
        var n = "[TestMethod] public voidfoo(a, b, c".Length;
        const string expecting =
            "[TestMethod] public voidfoo(a, b, c) throws Ick { i=3; }";
        Assert.AreEqual(expecting, a.Render(n));
    }

    [TestMethod]
    public void TestSingleValueWrap() {
        var templates = "m(args,body) ::= <<{ <body; anchor, wrap=\"\\n\"> }>>" + newline;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();

        var m = group.FindTemplate("m");
        m.Add("body", "i=3;");
        // make it wrap because of ") throws Ick { " literal
        var expecting =
            "{ " +  newline +
            "  i=3; }";
        Assert.AreEqual(expecting, m.Render(2));
    }

    [TestMethod]
    public void TestLineWrapInNestedExpr() {
        var templates =
            "top(arrays) ::= <<Arrays: <arrays>done>>" + newline +
            "array(values) ::= <%int[] a = { <values; anchor, wrap=\"\\n\", separator=\",\"> };<\\n>%>" + newline;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();

        var top = group.FindTemplate("top");
        var a = group.FindTemplate("array");
        a.Add("values", new[] {
            3,9,20,2,1,4,6,32,5,6,77,888,2,1,6,32,5,6,77,
            4,9,20,2,1,4,63,9,20,2,1,4,6,32,5,6,77,6,32,5,6,77,
            3,9,20,2,1,4,6,32,5,6,77,888,1,6,32,5});
        top.Add("arrays", a);
        top.Add("arrays", a); // Add twice
        var expecting =
            "Arrays: int[] a = { 3,9,20,2,1,4,6,32,5," +     newline +
            "                    6,77,888,2,1,6,32,5," +     newline +
            "                    6,77,4,9,20,2,1,4,63," +     newline +
            "                    9,20,2,1,4,6,32,5,6," +     newline +
            "                    77,6,32,5,6,77,3,9,20," +     newline +
            "                    2,1,4,6,32,5,6,77,888," +     newline +
            "                    1,6,32,5 };" +     newline +
            "int[] a = { 3,9,20,2,1,4,6,32,5,6,77,888," +     newline +
            "            2,1,6,32,5,6,77,4,9,20,2,1,4," +     newline +
            "            63,9,20,2,1,4,6,32,5,6,77,6," +     newline +
            "            32,5,6,77,3,9,20,2,1,4,6,32," +     newline +
            "            5,6,77,888,1,6,32,5 };" +     newline +
            "done";
        Assert.AreEqual(expecting, top.Render(40));
    }

}
