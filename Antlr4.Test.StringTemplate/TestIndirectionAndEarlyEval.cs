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

using Antlr4.StringTemplate;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using ErrorBuffer = Antlr4.StringTemplate.Misc.ErrorBuffer;

[TestClass]
public class TestIndirectionAndEarlyEval : BaseTest {

    [TestMethod]
    public void TestEarlyEval() {
        const string template = "<(name)>";
        var st = _templateFactory.CreateTemplate(template);
        st.Add("name", "Ter");
        const string expected = "Ter";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestIndirectTemplateInclude() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("foo", "bar");
        const string template = "<(name)()>";
        group.DefineTemplate("test", template, ["name"]);
        var st = group.FindTemplate("test");
        st.Add("name", "foo");
        const string expected = "bar";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestIndirectTemplateIncludeWithArgs() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("foo", "<x><y>", ["x", "y"]);
        const string template = "<(name)({1},{2})>";
        group.DefineTemplate("test", template, ["name"]);
        var st = group.FindTemplate("test");
        st.Add("name", "foo");
        const string expected = "12";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestIndirectCallWithPassThru() {
        // pass-through for dynamic template invocation is not supported by the
        // bytecode representation
        WriteFile(TmpDir, "t.stg",
            "t1(x) ::= \"<x>\"\n" +
            "main(x=\"hello\",t=\"t1\") ::= <<\n" +
            "<(t)(...)>\n" +
            ">>");
        var errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(TmpDir + "/t.stg").WithErrorListener(errors).Build();
        var st = group.FindTemplate("main");
        Assert.AreEqual("t.stg 2:34: mismatched input '...' expecting RPAREN" + newline, errors.ToString());
        Assert.IsNull(st);
    }

    [TestMethod]
    public void TestIndirectTemplateIncludeViaTemplate() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("foo", "bar");
        group.DefineTemplate("tname", "foo");
        var template = "<(tname())()>";
        group.DefineTemplate("test", template, ["name"]);
        var st = group.FindTemplate("test");
        const string expected = "bar";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestIndirectProp() {
        const string template = "<u.(propname)>: <u.name>";
        var st = _templateFactory.CreateTemplate(template);
        st.Add("u", new User(1, "parrt"));
        st.Add("propname", "id");
        const string expected = "1: parrt";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestIndirectMap() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("a", "[<x>]", ["x"]);
        group.DefineTemplate("test", "hi <names:(templateName)()>!", ["names", "templateName"]);
        var st = group.FindTemplate("test");
        st.Add("names", "Ter");
        st.Add("names", "Tom");
        st.Add("names", "Sumana");
        st.Add("templateName", "a");
        const string expected = "hi [Ter][Tom][Sumana]!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestNonStringDictLookup() {
        const string template = "<m.(intkey)>";
        var st = _templateFactory.CreateTemplate(template);
        IDictionary<int, string> m = new Dictionary<int, string>();
        m[36] = "foo";
        st.Add("m", m);
        st.Add("intkey", 36);
        const string expected = "foo";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

}
