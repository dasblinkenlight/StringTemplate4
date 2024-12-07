/*
 * [The "BSD license"]
 *  Copyright (c) 2011 Terence Parr
 *  All rights reserved.
 *
 *  Redistribution and use in source and binary forms, with or without
 *  modification, are permitted provided that the following conditions
 *  are met:
 *  1. Redistributions of source code must retain the above copyright
 *     notice, this list of conditions and the following disclaimer.
 *  2. Redistributions in binary form must reproduce the above copyright
 *     notice, this list of conditions and the following disclaimer in the
 *     documentation and/or other materials provided with the distribution.
 *  3. The name of the author may not be used to endorse or promote products
 *     derived from this software without specific prior written permission.
 *
 *  THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
 *  IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 *  OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 *  IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
 *  INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 *  NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 *  DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 *  THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 *  (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 *  THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

namespace Antlr4.Test.StringTemplate;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Path = System.IO.Path;

[TestClass]
public class TestNoNewlineTemplates : BaseTest {

    [TestMethod]
    public void TestNoNewlineTemplate() {
        var template =
            "t(x) ::= <%" + newline +
            "[  <if(!x)>" +
            "<else>" +
            "<x>" + newline +
            "<endif>" +
            "" + newline +
            "" + newline +
            "]" + newline +
            "" + newline +
            "%>" + newline;
        var g = _templateFactory.CreateTemplateGroupString(template).Build();
        var st = g.FindTemplate("t");
        st.Add("x", 99);
        const string expected = "[  99]";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestWSNoNewlineTemplate() {
        var template =
            "t(x) ::= <%" + newline +
            "" + newline +
            "%>" + newline;
        var g = _templateFactory.CreateTemplateGroupString(template).Build();
        var st = g.FindTemplate("t");
        st.Add("x", 99);
        var result = st.Render();
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void TestEmptyNoNewlineTemplate() {
        var template = "t(x) ::= <%%>" + newline;
        var g = _templateFactory.CreateTemplateGroupString(template).Build();
        var st = g.FindTemplate("t");
        st.Add("x", 99);
        var result = st.Render();
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void TestIgnoreIndent() {
        var template =
            "t(x) ::= <%" + newline +
            "	foo" + newline +
            "	<x>" + newline +
            "%>" + newline;
        var g = _templateFactory.CreateTemplateGroupString(template).Build();
        var st = g.FindTemplate("t");
        st.Add("x", 99);
        const string expected = "foo99";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestIgnoreIndentInIF() {
        var template =
            "t(x) ::= <%" + newline +
            "	<if(x)>" + newline +
            "		foo" + newline +
            "	<endif>" + newline +
            "	<x>" + newline +
            "%>" + newline;
        var g = _templateFactory.CreateTemplateGroupString(template).Build();
        var st = g.FindTemplate("t");
        st.Add("x", 99);
        const string expected = "foo99";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestKeepWS() {
        var template =
            "t(x) ::= <%" + newline +
            "	<x> <x> hi" + newline +
            "%>" + newline;
        var g = _templateFactory.CreateTemplateGroupString(template).Build();
        var st = g.FindTemplate("t");
        st.Add("x", 99);
        const string expected = "99 99 hi";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestRegion() {
        var template =
            "t(x) ::= <%\n" +
            "<@r>\n" +
            "	Ignore\n" +
            "	newlines and indents\n" +
            "<x>\n\n\n" +
            "<@end>\n" +
            "%>\n";
        var g = _templateFactory.CreateTemplateGroupString(template).Build();
        var st = g.FindTemplate("t");
        st.Add("x", 99);
        const string expected = "Ignorenewlines and indents99";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefineRegionInSubgroup() {
        var dir = TmpDir;
        const string g1 = "a() ::= <<[<@r()>]>>\n";
        WriteFile(dir, "g1.stg", g1);
        const string g2 = "@a.r() ::= <%\n" +
                          "	foo\n\n\n" +
                          "%>\n";
        WriteFile(dir, "g2.stg", g2);

        var group1 = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "g1.stg")).Build();
        var group2 = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "g2.stg")).Build();
        group2.ImportTemplates(group1); // define r in g2
        var st = group2.FindTemplate("a");
        const string expected = "[foo]";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

}
