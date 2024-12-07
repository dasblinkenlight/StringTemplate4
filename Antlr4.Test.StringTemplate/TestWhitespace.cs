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

using System.Collections.Generic;
using Antlr4.StringTemplate;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StringWriter = System.IO.StringWriter;

[TestClass]
public class TestWhitespace : BaseTest {

    [TestMethod]
    public void TestTrimmedSubtemplates() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<names:{n | <n>}>!", ["names"]);
        var st = group.FindTemplate("test");
        st.Add("names", "Ter");
        st.Add("names", "Tom");
        st.Add("names", "Sumana");
        const string expected = "TerTomSumana!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestTrimmedNewlinesBeforeAfterInTemplate() {
        var templates =
            $"a(x) ::= <<{newline}" +
            $"foo{newline}" +
            $">>{newline}";
        var group = _templateFactory.CreateTemplateGroupString(templates).Build();
        var st = group.FindTemplate("a");
        const string expected = "foo";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDontTrimJustSpaceBeforeAfterInTemplate() {
        var templates =
            "a(x) ::= << foo >>\n";
        var group = _templateFactory.CreateTemplateGroupString(templates).Build();
        var st = group.FindTemplate("a");
        const string expected = " foo ";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestTrimmedSubtemplatesNoArgs() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "[<foo({ foo })>]");
        group.DefineTemplate("foo", "<x>", ["x"]);
        var st = group.FindTemplate("test");
        const string expected = "[ foo ]";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestTrimmedSubtemplatesArgs() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<names:{x|  foo }>", ["names"]);
        var st = group.FindTemplate("test");
        st.Add("names", "Ter");
        st.Add("names", "Tom");
        st.Add("names", "Sumana");
        const string expected = " foo  foo  foo ";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestTrimJustOneWSInSubtemplates() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<names:{n |  <n> }>!", ["names"]);
        var st = group.FindTemplate("test");
        st.Add("names", "Ter");
        st.Add("names", "Tom");
        st.Add("names", "Sumana");
        const string expected = " Ter  Tom  Sumana !";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestTrimNewlineInSubtemplates() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<names:{n |\n" +
                                     "<n>}>!", ["names"]);
        var st = group.FindTemplate("test");
        st.Add("names", "Ter");
        st.Add("names", "Tom");
        st.Add("names", "Sumana");
        const string expected = "TerTomSumana!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestLeaveNewlineOnEndInSubtemplates() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<names:{n |\n" +
                                     "<n>\n" +
                                     "}>!", ["names"]);
        var st = group.FindTemplate("test");
        st.Add("names", "Ter");
        st.Add("names", "Tom");
        st.Add("names", "Sumana");
        var expected = $"Ter{newline}Tom{newline}Sumana{newline}!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [Ignore("will revisit the behavior of indented expressions spanning multiple lines for a future release")]
    [TestMethod]
    public void TestTabBeforeEndInSubtemplates() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "  <names:{n |\n" +
                                     "    <n>\n" +
                                     "  }>!", ["names"]);
        var st = group.FindTemplate("test");
        st.Add("names", "Ter");
        st.Add("names", "Tom");
        st.Add("names", "Sumana");
        var expected =
           $"    Ter{newline}"+
           $"    Tom{newline}" +
           $"    Sumana{newline}!";
        var result = st.Render();
        TestContext.WriteLine(st.GetCompiledTemplate().ToString());
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEmptyExprAsFirstLineGetsNoOutput() {
        var t = _templateFactory.CreateTemplate(
            "<users>\n" +
            "end\n");
        var expecting = $"end{newline}";
        var result = t.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestEmptyLineWithIndent() {
        var t = _templateFactory.CreateTemplate(
            "begin\n" +
            "    \n" +
            "end\n");
        var expecting = $"begin{newline}{newline}end{newline}";
        var result = t.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestEmptyLine() {
        var t = _templateFactory.CreateTemplate(
            "begin\n" +
            "\n" +
            "end\n");
        var expecting = $"begin{newline}{newline}end{newline}";
        var result = t.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestSizeZeroOnLineByItselfGetsNoOutput() {
        var t = _templateFactory.CreateTemplate(
            "begin\n" +
            "<name>\n" +
            "<users>\n" +
            "<users>\n" +
            "end\n");
        var expecting = $"begin{newline}end{newline}";
        var result = t.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestSizeZeroOnLineWithIndentGetsNoOutput() {
        var t = _templateFactory.CreateTemplate(
            "begin\n" +
            "  <name>\n" +
            "	<users>\n" +
            "	<users>\n" +
            "end\n");
        var expecting = $"begin{newline}end{newline}";
        var result = t.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestSizeZeroOnLineWithMultipleExpr() {
        var t = _templateFactory.CreateTemplate(
            "begin\n" +
            "  <name>\n" +
            "	<users><users>\n" +
            "end\n");
        var expecting = $"begin{newline}end{newline}";
        var result = t.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestIFExpr() {
        var t = _templateFactory.CreateTemplate(
            "begin\n" +
            "<if(x)><endif>\n" +
            "end\n");
        var expecting = $"begin{newline}end{newline}";
        var result = t.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestIndentedIFExpr() {
        var t = _templateFactory.CreateTemplate(
            "begin\n" +
            "    <if(x)><endif>\n" +
            "end\n");
        var expecting = $"begin{newline}end{newline}";
        var result = t.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestIFElseExprOnSingleLine() {
        var t = _templateFactory.CreateTemplate(
            "begin\n" +
            "<if(users)><else><endif>\n" +
            "end\n");
        var expecting = $"begin{newline}end{newline}";
        var result = t.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestIFOnMultipleLines() {
        var t = _templateFactory.CreateTemplate(
            "begin\n" +
            "<if(users)>\n" +
            "foo\n" +
            "<else>\n" +
            "bar\n" +
            "<endif>\n" +
            "end\n");
        var expecting = $"begin{newline}bar{newline}end{newline}";
        var result = t.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestElseifOnMultipleLines() {
        var t = _templateFactory.CreateTemplate(
            "begin\n" +
            "<if(a)>\n" +
            "foo\n" +
            "<elseif(b)>\n" +
            "bar\n" +
            "<endif>\n" +
            "end\n");
        var expecting = $"begin{newline}end{newline}";
        var result = t.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestElseifOnMultipleLines2() {
        var t = _templateFactory.CreateTemplate(
            "begin\n" +
            "<if(a)>\n" +
            "foo\n" +
            "<elseif(b)>\n" +
            "bar\n" +
            "<endif>\n" +
            "end\n");
        t.Add("b", true);
        var expecting = $"begin{newline}bar{newline}end{newline}";
        var result = t.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestElseifOnMultipleLines3() {
        var t = _templateFactory.CreateTemplate(
            "begin\n" +
            "  <if(a)>\n" +
            "  foo\n" +
            "  <elseif(b)>\n" +
            "  bar\n" +
            "  <endif>\n" +
            "end\n");
        t.Add("a", true);
        var expecting = $"begin{newline}  foo{newline}end{newline}";
        var result = t.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestEndifNotOnLineAlone() {
        var t = _templateFactory.CreateTemplate(
            "begin\n" +
            "  <if(users)>\n" +
            "  foo\n" +
            "  <else>\n" +
            "  bar\n" +
            "  <endif>end\n");
        var expecting = $"begin{newline}  bar{newline}end{newline}";
        var result = t.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestNestedIFOnMultipleLines() {
        var t = _templateFactory.CreateTemplate(
            "begin\n" +
            "<if(x)>\n" +
            "<if(y)>\n" +
            "foo\n" +
            "<else>\n" +
            "bar\n" +
            "<endif>\n" +
            "<endif>\n" +
            "end\n");
        t.Add("x", "x");
        var expecting = $"begin{newline}bar{newline}end{newline}";
        var result = t.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestIfElseifOnMultipleLines() {
        var t = _templateFactory.CreateTemplate(
            "begin\n" +
            "<if(x&&y)>\n" +
            "foo\n" +
            "<elseif(x)>\n" +
            "bar\n" +
            "<endif>\n" +
            "end\n");
        t.Add("x", "x");
        var expecting = $"begin{newline}bar{newline}end{newline}";
        var result = t.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestLineBreak() {
        var st = _templateFactory.CreateTemplate(
            $"Foo <\\\\>{newline}" +
            $"  \t  bar{newline}"
        );
        var sw = new StringWriter();
        st.Write(new AutoIndentWriter(sw, "\n")); // force \n as newline
        var result = sw.ToString();
        const string expecting = "Foo bar\n";     // expect \n in output
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestLineBreak2() {
        var st = _templateFactory.CreateTemplate(
            $"Foo <\\\\>       {newline}" +
            $"  \t  bar{newline}"
        );
        var sw = new StringWriter();
        st.Write(new AutoIndentWriter(sw, "\n")); // force \n as newline
        var result = sw.ToString();
        const string expecting = "Foo bar\n";
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestLineBreakNoWhiteSpace() {
        var st = _templateFactory.CreateTemplate($"Foo <\\\\>{newline}bar\n");
        var sw = new StringWriter();
        st.Write(new AutoIndentWriter(sw, "\n")); // force \n as newline
        var result = sw.ToString();
        const string expecting = "Foo bar\n";
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestNewlineNormalizationInTemplateString() {
        var st = new Template(
            "Foo\r\n" +
            "Bar\n"
        );
        var sw = new StringWriter();
        st.Write(new AutoIndentWriter(sw, "\n")); // force \n as newline
        var result = sw.ToString();
        const string expecting = "Foo\nBar\n";     // expect \n in output
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestNewlineNormalizationInTemplateStringPC() {
        var st = new Template(
            "Foo\r\n" +
            "Bar\n"
        );
        var sw = new StringWriter();
        st.Write(new AutoIndentWriter(sw, "\r\n")); // force \r\n as newline
        var result = sw.ToString();
        const string expecting = "Foo\r\nBar\r\n";     // expect \r\n in output
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestNewlineNormalizationInAttribute() {
        var st = new Template(
            "Foo\r\n" +
            "<name>\n"
        );
        st.Add("name", "a\nb\r\nc");
        var sw = new StringWriter();
        st.Write(new AutoIndentWriter(sw, "\n")); // force \n as newline
        var result = sw.ToString();
        const string expecting = "Foo\na\nb\nc\n";     // expect \n in output
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestNullIterationLineGivesNoOutput() {
        var t = new Template(
            "begin\n" +
            "<items:{x|<x>}>\n" +
            "end\n");
        var expecting = $"begin{newline}end{newline}";
        var result = t.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestEmptyIterationLineGivesNoOutput() {
        var t = new Template(
            "begin\n" +
            "  <items:{x|<x>}>\n" +
            "end\n");
        t.Add("items", new List<object>());
        var expecting = $"begin{newline}end{newline}";
        var result = t.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestCommentOnlyLineGivesNoOutput() {
        var t = new Template(
            "begin\n" +
            "<! ignore !>\n" +
            "end\n");
        var expecting = $"begin{newline}end{newline}";
        var result = t.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestCommentOnlyLineGivesNoOutput2() {
        var t = new Template(
            "begin\n" +
            "    <! ignore !>\n" +
            "end\n");
        var expecting = $"begin{newline}end{newline}";
        var result = t.Render();
        Assert.AreEqual(expecting, result);
    }

}
