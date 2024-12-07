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

namespace Antlr4.Test.StringTemplate;

using System.Collections.Generic;
using Antlr4.StringTemplate;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CultureInfo = System.Globalization.CultureInfo;
using DateTime = System.DateTime;
using DateTimeOffset = System.DateTimeOffset;
using Path = System.IO.Path;

[TestClass]
public class TestRenderers : BaseTest {

    [TestMethod]
    public void TestRendererForGroup() {
        const string templates =
            "dateThing(created) ::= \"datetime: <created>\"\n";
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        group.RegisterRenderer(typeof(DateTime), new DateRenderer());
        group.RegisterRenderer(typeof(DateTimeOffset), new DateRenderer());
        var st = group.GetInstanceOf("dateThing");
        st.Add("created", new DateTime(2005, 7, 5));
        const string expecting = "datetime: 07/05/2005 00:00";
        var result = st.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestRendererWithFormat() {
        const string templates =
            "dateThing(created) ::= << date: <created; format=\"yyyy.MM.dd\"> >>\n";
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        group.RegisterRenderer(typeof(DateTime), new DateRenderer());
        group.RegisterRenderer(typeof(DateTimeOffset), new DateRenderer());
        var st = group.GetInstanceOf("dateThing");
        st.Add("created", new DateTime(2005, 7, 5));
        const string expecting = " date: 2005.07.05 ";
        var result = st.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestRendererWithPredefinedFormat() {
        const string templates =
            "dateThing(created) ::= << datetime: <created; format=\"short\"> >>\n";
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        group.RegisterRenderer(typeof(DateTime), new DateRenderer());
        group.RegisterRenderer(typeof(DateTimeOffset), new DateRenderer());
        var st = group.GetInstanceOf("dateThing");
        st.Add("created", new DateTime(2005, 7, 5));
        const string expecting = " datetime: 07/05/2005 00:00 ";
        var result = st.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestRendererWithPredefinedFormat2() {
        const string templates =
            "dateThing(created) ::= << datetime: <created; format=\"full\"> >>\n";
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        group.RegisterRenderer(typeof(DateTime), new DateRenderer());
        group.RegisterRenderer(typeof(DateTimeOffset), new DateRenderer());
        var st = group.GetInstanceOf("dateThing");
        st.Add("created", new DateTime(2005, 7, 5));
        var expecting = " datetime: Tuesday, 05 July 2005 00:00:00 ";
        var result = st.Render();
        Assert.AreEqual(expecting, result);
    }

    [Ignore("medium is not supported on .NET")]
    [TestMethod]
    public void TestRendererWithPredefinedFormat3() {
        const string templates =
            "dateThing(created) ::= << date: <created; format=\"date:medium\"> >>\n";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        group.RegisterRenderer(typeof(DateTime), new DateRenderer());
        group.RegisterRenderer(typeof(DateTimeOffset), new DateRenderer());
        var st = group.GetInstanceOf("dateThing");
        st.Add("created", new DateTime(2005, 7, 5));
        const string expecting = " date: Jul 5, 2005 ";
        var result = st.Render();
        Assert.AreEqual(expecting, result);
    }

    [Ignore("medium is not supported on .NET")]
    [TestMethod]
    public void TestRendererWithPredefinedFormat4() {
        const string templates =
            "dateThing(created) ::= << time: <created; format=\"time:medium\"> >>\n";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        group.RegisterRenderer(typeof(DateTime), new DateRenderer());
        group.RegisterRenderer(typeof(DateTimeOffset), new DateRenderer());
        var st = group.GetInstanceOf("dateThing");
        st.Add("created", new DateTime(2005, 7, 5));
        const string expecting = " time: 12:00:00 AM ";
        var result = st.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestStringRendererWithPrintfFormat() {
        const string templates =
            "foo(x) ::= << <x; format=\"{0,6}\"> >>\n";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        group.RegisterRenderer(typeof(string), new StringRenderer());
        var st = group.GetInstanceOf("foo");
        st.Add("x", "hi");
        const string expecting = "     hi ";
        var result = st.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestRendererWithFormatAndList() {
        const string template =
            "The names: <names; format=\"upper\">";
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.RegisterRenderer(typeof(string), new StringRenderer());
        var st = new Template(group, template);
        st.Add("names", "ter");
        st.Add("names", "tom");
        st.Add("names", "sriram");
        const string expecting = "The names: TERTOMSRIRAM";
        var result = st.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestRendererWithFormatAndSeparator() {
        const string template =
            "The names: <names; separator=\" and \", format=\"upper\">";
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.RegisterRenderer(typeof(string), new StringRenderer());
        var st = new Template(group, template);
        st.Add("names", "ter");
        st.Add("names", "tom");
        st.Add("names", "sriram");
        const string expecting = "The names: TER and TOM and SRIRAM";
        var result = st.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestRendererWithFormatAndSeparatorAndNull() {
        const string template =
            "The names: <names; separator=\" and \", null=\"n/a\", format=\"upper\">";
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.RegisterRenderer(typeof(string), new StringRenderer());
        var st = new Template(group, template);
        var names = new List<string> { "ter", null, "sriram" };
        st.Add("names", names);
        const string expecting = "The names: TER and N/A and SRIRAM";
        var result = st.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestStringRendererWithFormatCap() {
        const string templates =
            "foo(x) ::= << <x; format=\"cap\"> >>\n";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        group.RegisterRenderer(typeof(string), new StringRenderer());
        var st = group.GetInstanceOf("foo");
        st.Add("x", "hi");
        const string expecting = " Hi ";
        var result = st.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestStringRendererWithTemplateIncludeCap() {
        // must toString the t() ref before applying format
        const string templates =
            "foo(x) ::= << <(t()); format=\"cap\"> >>\n" +
            "t() ::= <<ack>>\n";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        //Interpreter.trace = true;
        group.RegisterRenderer(typeof(string), new StringRenderer());
        var st = group.GetInstanceOf("foo");
        st.Add("x", "hi");
        const string expecting = " Ack ";
        var result = st.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestStringRendererWithSubtemplateIncludeCap() {
        // must toString the t() ref before applying format
        const string templates =
            "foo(x) ::= << <({ack}); format=\"cap\"> >>\n";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        //Interpreter.trace = true;
        group.RegisterRenderer(typeof(string), new StringRenderer());
        var st = group.GetInstanceOf("foo");
        st.Add("x", "hi");
        const string expecting = " Ack ";
        var result = st.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestStringRendererWithFormatCapEmptyValue() {
        const string templates =
            "foo(x) ::= << <x; format=\"cap\"> >>\n";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        group.RegisterRenderer(typeof(string), new StringRenderer());
        var st = group.GetInstanceOf("foo");
        st.Add("x", "");
        const string expecting = " ";//FIXME: why not two spaces?
        var result = st.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestStringRendererWithFormatUrlEncode() {
        const string templates =
            "foo(x) ::= << <x; format=\"url-encode\"> >>\n";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        group.RegisterRenderer(typeof(string), new StringRenderer());
        var st = group.GetInstanceOf("foo");
        st.Add("x", "a b");
        const string expecting = " a+b ";
        var result = st.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestStringRendererWithFormatXmlEncode() {
        const string templates =
            "foo(x) ::= << <x; format=\"xml-encode\"> >>\n";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        group.RegisterRenderer(typeof(string), new StringRenderer());
        var st = group.GetInstanceOf("foo");
        st.Add("x", "a<b> &\t\b");
        const string expecting = " a&lt;b&gt; &amp;\t\b ";
        var result = st.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestStringRendererWithFormatXmlEncodeNull() {
        const string templates =
            "foo(x) ::= << <x; format=\"xml-encode\"> >>\n";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        group.RegisterRenderer(typeof(string), new StringRenderer());
        var st = group.GetInstanceOf("foo");
        st.Add("x", null);
        const string expecting = " ";
        var result = st.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestNumberRendererWithPrintfFormat() {
        //string templates = "foo(x,y) ::= << <x; format=\"%d\"> <y; format=\"%2.3f\"> >>\n";
        const string templates = "foo(x,y) ::= << <x; format=\"{0}\"> <y; format=\"{0:0.000}\"> >>\n";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        group.RegisterRenderer(typeof(int), new NumberRenderer());
        group.RegisterRenderer(typeof(double), new NumberRenderer());
        var st = group.GetInstanceOf("foo");
        st.Add("x", -2100);
        st.Add("y", 3.14159);
        const string expecting = " -2100 3.142 ";
        var result = st.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestInstanceofRenderer() {
        const string templates =
            "numberThing(x,y,z) ::= \"numbers: <x>, <y>; <z>\"\n";
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        group.RegisterRenderer(typeof(int), new NumberRenderer());
        group.RegisterRenderer(typeof(double), new NumberRenderer());
        var st = group.GetInstanceOf("numberThing");
        st.Add("x", -2100);
        st.Add("y", 3.14159);
        st.Add("z", "hi");
        const string expecting = "numbers: -2100, 3.14159; hi";
        var result = st.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestLocaleWithNumberRenderer() {
        //string templates = "foo(x,y) ::= << <x; format=\"%,d\"> <y; format=\"%,2.3f\"> >>\n";
        const string templates = "foo(x,y) ::= << <x; format=\"{0:#,#}\"> <y; format=\"{0:0.000}\"> >>\n";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        group.RegisterRenderer(typeof(int), new NumberRenderer());
        group.RegisterRenderer(typeof(double), new NumberRenderer());
        var st = group.GetInstanceOf("foo");
        st.Add("x", -2100);
        st.Add("y", 3.14159);
        // Polish uses ' ' (ASCII 160) for ',' and ',' for '.'
        const string expecting = " -2 100 3,142 "; // Ê
        var result = st.Render(new CultureInfo("pl"));
        Assert.AreEqual(expecting, result);
    }

}
