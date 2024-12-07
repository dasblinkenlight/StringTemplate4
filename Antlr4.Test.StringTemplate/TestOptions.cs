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

using System;

namespace Antlr4.Test.StringTemplate;

using Antlr4.StringTemplate;
using Antlr4.StringTemplate.Misc;
using Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Console = Console;

[TestClass]
public class TestOptions : BaseTest {

    [TestMethod]
    public void TestSeparator() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "hi <name; separator=\", \">!", ["name"]);
        var st = group.GetInstanceOf("test");
        st.Add("name", "Ter");
        st.Add("name", "Tom");
        st.Add("name", "Sumana");
        const string expected = "hi Ter, Tom, Sumana!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSeparatorWithSpaces() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "hi <name; separator= \", \">!", ["name"]);
        var st = group.GetInstanceOf("test");
        Console.WriteLine(st.impl.Ast.ToStringTree());
        st.Add("name", "Ter");
        st.Add("name", "Tom");
        st.Add("name", "Sumana");
        const string expected = "hi Ter, Tom, Sumana!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestAttrSeparator() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "hi <name; separator=sep>!", ["name", "sep"]);
        var st = group.GetInstanceOf("test");
        st.Add("sep", ", ");
        st.Add("name", "Ter");
        st.Add("name", "Tom");
        st.Add("name", "Sumana");
        const string expected = "hi Ter, Tom, Sumana!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestIncludeSeparator() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("foo", "|");
        group.DefineTemplate("test", "hi <name; separator=foo()>!", ["name", "sep"]);
        var st = group.GetInstanceOf("test");
        st.Add("sep", ", ");
        st.Add("name", "Ter");
        st.Add("name", "Tom");
        st.Add("name", "Sumana");
        const string expected = "hi Ter|Tom|Sumana!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSubtemplateSeparator() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "hi <name; separator={<sep> _}>!", ["name", "sep"]);
        var st = group.GetInstanceOf("test");
        st.Add("sep", ",");
        st.Add("name", "Ter");
        st.Add("name", "Tom");
        st.Add("name", "Sumana");
        const string expected = "hi Ter, _Tom, _Sumana!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSeparatorWithNullFirstValueAndNullOption() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "hi <name; null=\"n/a\", separator=\", \">!", ["name"]);
        var st = group.GetInstanceOf("test");
        st.Add("name", null);
        st.Add("name", "Tom");
        st.Add("name", "Sumana");
        const string expected = "hi n/a, Tom, Sumana!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSeparatorWithNull2ndValueAndNullOption() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "hi <name; null=\"n/a\", separator=\", \">!", ["name"]);
        var st = group.GetInstanceOf("test");
        TestContext.WriteLine(st.impl.ToString());
        st.Add("name", "Ter");
        st.Add("name", null);
        st.Add("name", "Sumana");
        const string expected = "hi Ter, n/a, Sumana!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestNullValueAndNullOption() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<name; null=\"n/a\">", ["name"]);
        var st = group.GetInstanceOf("test");
        st.Add("name", null);
        const string expected = "n/a";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestListApplyWithNullValueAndNullOption() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<name:{n | <n>}; null=\"n/a\">", ["name"]);
        var st = group.GetInstanceOf("test");
        st.Add("name", "Ter");
        st.Add("name", null);
        st.Add("name", "Sumana");
        const string expected = "Tern/aSumana";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDoubleListApplyWithNullValueAndNullOption() {
        // first apply sends [Template, null, Template] to second apply, which puts [] around
        // the value.  This verifies that null not blank comes out of first apply
        // since we don't get [null].
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<name:{n | <n>}:{n | [<n>]}; null=\"n/a\">", ["name"]);
        var st = group.GetInstanceOf("test");
        st.Add("name", "Ter");
        st.Add("name", null);
        st.Add("name", "Sumana");
        const string expected = "[Ter]n/a[Sumana]";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestMissingValueAndNullOption() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<name; null=\"n/a\">", ["name"]);
        var st = group.GetInstanceOf("test");
        const string expected = "n/a";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestOptionDoesntApplyToNestedTemplate() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("foo", "<zippo>");
        group.DefineTemplate("test", "<foo(); null=\"n/a\">", ["zippo"]);
        var st = group.GetInstanceOf("test");
        st.Add("zippo", null);
        var result = st.Render();
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void TestIllegalOption() {
        var errors = new ErrorBuffer();
        var group = new TemplateGroup {
            Listener = errors
        };
        group.DefineTemplate("test", "<name; bad=\"ugly\">", ["name"]);
        var st = group.GetInstanceOf("test");
        st.Add("name", "Ter");
        const string expected1 = "Ter";
        var result = st.Render();
        Assert.AreEqual(expected1, result);
        const string expected2 = "[test 1:7: no such option: bad]";
        Assert.AreEqual(expected2, errors.Errors.ToListString());
    }

}
