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

using System;
using Antlr4.StringTemplate.Debug;

namespace Antlr4.Test.StringTemplate;

using System.Collections.Generic;
using Antlr4.StringTemplate;
using Antlr4.StringTemplate.Misc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ArgumentException = ArgumentException;
using Path = System.IO.Path;
using StringWriter = System.IO.StringWriter;

[TestClass]
public class TestCoreBasics : BaseTest {

    [TestMethod]
    public void TestNullAttr() {
        const string template = "hi <name>!";
        var st = _templateFactory.CreateTemplate(template);
        const string expected = "hi !";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestAttr() {
        const string template = "hi <name>!";
        var st = _templateFactory.CreateTemplate(template);
        st.Add("name", "Ter");
        const string expected = "hi Ter!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestChainAttr() {
        const string template = "<x>:<names>!";
        var st = _templateFactory.CreateTemplate(template);
        st.Add("names", "Ter").Add("names", "Tom").Add("x", 1);
        const string expected = "1:TerTom!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [Ignore("this check has been removed in Commit 10e937d")]
    [TestMethod]
    public void TestSetUnknownAttr() {
        const string templates =
            "t() ::= <<hi <name>!>>\n";
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var st = group.FindTemplate("t");
        string result = null;
        try {
            st.Add("name", "Ter");
        } catch (ArgumentException iae) {
            result = iae.Message;
        }
        const string expected = "no such attribute: name";
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestMultiAttr() {
        const string template = "hi <name>!";
        var st = _templateFactory.CreateTemplate(template);
        st.Add("name", "Ter");
        st.Add("name", "Tom");
        const string expected = "hi TerTom!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestAttrIsList() {
        const string template = "hi <name>!";
        var st = _templateFactory.CreateTemplate(template);
        var names = new List<string>() { "Ter", "Tom" };
        st.Add("name", names);
        st.Add("name", "Sumana"); // shouldn't alter my version of names list!
        const string expected =
            "hi TerTomSumana!";  // Template sees 3 names
        var result = st.Render();
        Assert.AreEqual(expected, result);

        Assert.AreEqual(2, names.Count); // my names list is still just 2
    }

    [TestMethod]
    public void TestAttrIsArray() {
        const string template = "hi <name>!";
        var st = _templateFactory.CreateTemplate(template);
        var names = new[] { "Ter", "Tom" };
        st.Add("name", names);
        st.Add("name", "Sumana"); // shouldn't alter my version of names list!
        const string expected =
            "hi TerTomSumana!";  // Template sees 3 names
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestProp() {
        const string template = "<u.id>: <u.name>"; // checks field and method getter
        var st = _templateFactory.CreateTemplate(template);
        st.Add("u", new User(1, "parrt"));
        const string expected = "1: parrt";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestPropWithNoAttr() {
        const string template = "<foo.a>: <ick>";
        var st = _templateFactory.CreateTemplate(template);
        st.Add("foo", new Dictionary<string, string>() { { "a", "b" } });
        const string expected = "b: ";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestMapAcrossDictionaryUsesKeys() {
        const string template = "<foo:{f | <f>}>"; // checks field and method getter
        var st = _templateFactory.CreateTemplate(template);
        st.Add("foo", new SortedDictionary<string, string>() { { "a", "b" }, { "c", "d" } });
        const string expected = "ac";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSTProp() {
        const string template = "<t.x>"; // get x attr of template t
        var st = _templateFactory.CreateTemplate(template);
        var t = _templateFactory.CreateTemplate("<x>");
        t.Add("x", "Ter");
        st.Add("t", t);
        const string expected = "Ter";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestBooleanISProp() {
        const string template = "<t.isManager>"; // call isManager
        var st = _templateFactory.CreateTemplate(template);
        st.Add("t", new User(32, "Ter"));
        const string expected = "true";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestBooleanHASProp() {
        const string template = "<t.hasParkingSpot>"; // call hasParkingSpot
        var st = _templateFactory.CreateTemplate(template);
        st.Add("t", new User(32, "Ter"));
        const string expected = "true";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestStaticMethod() {
        const string template = "<t.StaticMethod>";
        var st = _templateFactory.CreateTemplate(template);
        st.Add("t", new User(32, "Ter"));
        const string expected = "method_result";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestStaticProperty() {
        const string template = "<t.StaticProperty>";
        var st = _templateFactory.CreateTemplate(template);
        st.Add("t", new User(32, "Ter"));
        const string expected = "property_result";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestStaticField() {
        const string template = "<t.StaticField>";
        var st = _templateFactory.CreateTemplate(template);
        st.Add("t", new User(32, "Ter"));
        const string expected = "field_value";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestNullAttrProp() {
        const string template = "<u.id>: <u.name>";
        var st = _templateFactory.CreateTemplate(template);
        const string expected = ": ";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestNoSuchProp() {
        var errors = new ErrorBufferAllErrors();
        const string template = "<u.qqq>";
        var group = new TemplateGroup {
            Listener = errors
        };
        var st = _templateFactory.CreateTemplateImplicit(template, group);
        st.Add("u", new User(1, "parrt"));
        var result = st.Render();
        Assert.AreEqual(string.Empty, result);
        var msg = (TemplateRuntimeMessage)errors.Errors[0];
        var e = (TemplateNoSuchPropertyException)msg.Cause;
        Assert.AreEqual("Antlr4.Test.StringTemplate.BaseTest+User.qqq", e.PropertyName);
    }

    [TestMethod]
    public void TestNullIndirectProp() {
        var errors = new ErrorBufferAllErrors();
        var group = new TemplateGroup {
            Listener = errors
        };
        const string template = "<u.(qqq)>";
        var st = _templateFactory.CreateTemplateImplicit(template, group);
        st.Add("u", new User(1, "parrt"));
        st.Add("qqq", null);
        var result = st.Render();
        Assert.AreEqual(string.Empty, result);
        var msg = (TemplateRuntimeMessage)errors.Errors[0];
        var e = (TemplateNoSuchPropertyException)msg.Cause;
        Assert.AreEqual("Antlr4.Test.StringTemplate.BaseTest+User.null", e.PropertyName);
    }

    [TestMethod]
    public void TestPropConvertsToString() {
        var errors = new ErrorBufferAllErrors();
        var group = new TemplateGroup {
            Listener = errors
        };
        const string template = "<u.(name)>";
        var st = new Template(template, group);
        st.Add("u", new User(1, "parrt"));
        st.Add("name", 100);
        var result = st.Render();
        Assert.AreEqual(string.Empty, result);
        var msg = (TemplateRuntimeMessage)errors.Errors[0];
        var e = (TemplateNoSuchPropertyException)msg.Cause;
        Assert.AreEqual("Antlr4.Test.StringTemplate.BaseTest+User.100", e.PropertyName);
    }

    [TestMethod]
    public void TestInclude() {
        const string template = "Load <box()>;";
        var st = new Template(template);
        st.impl.NativeGroup.DefineTemplate("box", "kewl\ndaddy");
        var expected = $"Load kewl{newline}daddy;";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestIncludeWithArg() {
        const string template = "Load <box(\"arg\")>;";
        var st = new Template(template);
        st.impl.NativeGroup.DefineTemplate("box", "kewl <x> daddy", ["x"]);
        TestContext.WriteLine(st.impl.ToString());
        st.Add("name", "Ter");
        const string expected = "Load kewl arg daddy;";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestIncludeWithArg2() {
        const string template = "Load <box(\"arg\", foo())>;";
        var st = new Template(template);
        st.impl.NativeGroup.DefineTemplate("box", "kewl <x> <y> daddy", ["x", "y"]);
        st.impl.NativeGroup.DefineTemplate("foo", "blech");
        st.Add("name", "Ter");
        const string expected = "Load kewl arg blech daddy;";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestIncludeWithEmptySubtemplateArg() {
        const string template = "load <box({})>;";
        var st = new Template(template);
        st.impl.NativeGroup.DefineTemplate("box", "kewl <x> daddy", ["x"]);
        TestContext.WriteLine(st.impl.ToString());
        st.Add("name", "Ter");
        const string expected = "load kewl  daddy;";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestIncludeWithNestedArgs() {
        const string template = "Load <box(foo(\"arg\"))>;";
        var st = new Template(template);
        st.impl.NativeGroup.DefineTemplate("box", "kewl <y> daddy", ["y"]);
        st.impl.NativeGroup.DefineTemplate("foo", "blech <x>", ["x"]);
        st.Add("name", "Ter");
        const string expected = "Load kewl blech arg daddy;";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestPassThru() {
        const string templates =
            "a(x,y) ::= \"<b(...)>\"\n" +
            "b(x,y) ::= \"<x><y>\"\n";
        var group = _templateFactory.CreateTemplateGroupString(templates).Build();
        var a = group.FindTemplate("a");
        a.Add("x", "x");
        a.Add("y", "y");
        const string expected = "xy";
        var result = a.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestPassThruWithDefaultValue() {
        const string templates =
            "a(x,y) ::= \"<b(...)>\"\n" + // should not set y when it sees "no value" from above
            "b(x,y={99}) ::= \"<x><y>\"\n";
        var group = _templateFactory.CreateTemplateGroupString(templates).Build();
        var a = group.FindTemplate("a");
        a.Add("x", "x");
        const string expected = "x99";
        var result = a.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestPassThruWithDefaultValueThatLacksDefinitionAbove() {
        const string templates =
            "a(x) ::= \"<b(...)>\"\n" + // should not set y when it sees "no definition" from above
            "b(x,y={99}) ::= \"<x><y>\"\n";
        var group = _templateFactory.CreateTemplateGroupString(templates).Build();
        var a = group.FindTemplate("a");
        a.Add("x", "x");
        const string expected = "x99";
        var result = a.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestPassThruPartialArgs() {
        const string templates =
            "a(x,y) ::= \"<b(y={99},...)>\"\n" +
            "b(x,y) ::= \"<x><y>\"\n";
        var group = _templateFactory.CreateTemplateGroupString(templates).Build();
        var a = group.FindTemplate("a");
        a.Add("x", "x");
        a.Add("y", "y");
        const string expected= "x99";
        var result = a.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestPassThruNoMissingArgs() {
        const string templates =
            "a(x,y) ::= \"<b(y={99},x={1},...)>\"\n" +
            "b(x,y) ::= \"<x><y>\"\n";
        var group = _templateFactory.CreateTemplateGroupString(templates).Build();
        var a = group.FindTemplate("a");
        a.Add("x", "x");
        a.Add("y", "y");
        const string expected = "199";
        var result = a.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefineTemplate() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("inc", "<x>+1", ["x"]);
        group.DefineTemplate("test", "hi <name>!", ["name"]);
        var st = group.FindTemplate("test");
        st.Add("name", "Ter");
        st.Add("name", "Tom");
        st.Add("name", "Sumana");
        const string expected = "hi TerTomSumana!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestMap() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("inc", "[<x>]", ["x"]);
        group.DefineTemplate("test", "hi <name:inc()>!", ["name"]);
        var st = group.FindTemplate("test");
        st.Add("name", "Ter");
        st.Add("name", "Tom");
        st.Add("name", "Sumana");
        const string expected = "hi [Ter][Tom][Sumana]!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestIndirectMap() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("inc", "[<x>]", ["x"]);
        group.DefineTemplate("test", "<name:(t)()>!", ["t", "name"]);
        var st = group.FindTemplate("test");
        st.Add("t", "inc");
        st.Add("name", "Ter");
        st.Add("name", "Tom");
        st.Add("name", "Sumana");
        const string expected = "[Ter][Tom][Sumana]!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestMapWithExprAsTemplateName() {
        const string templates =
            "d ::= [\"foo\":\"bold\"]\n" +
            "test(name) ::= \"<name:(d.foo)()>\"\n" +
            "bold(x) ::= <<*<x>*>>\n";
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var st = group.FindTemplate("test");
        st.Add("name", "Ter");
        st.Add("name", "Tom");
        st.Add("name", "Sumana");
        const string expected = "*Ter**Tom**Sumana*";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestParallelMap() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "hi <names,phones:{n,p | <n>:<p>;}>", ["names", "phones"]);
        var st = group.FindTemplate("test");
        st.Add("names", "Ter");
        st.Add("names", "Tom");
        st.Add("names", "Sumana");
        st.Add("phones", "x5001");
        st.Add("phones", "x5002");
        st.Add("phones", "x5003");
        const string expected = "hi Ter:x5001;Tom:x5002;Sumana:x5003;";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestParallelMapWith3Versus2Elements() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "hi <names,phones:{n,p | <n>:<p>;}>", ["names", "phones"]);
        var st = group.FindTemplate("test");
        st.Add("names", "Ter");
        st.Add("names", "Tom");
        st.Add("names", "Sumana");
        st.Add("phones", "x5001");
        st.Add("phones", "x5002");
        const string expected = "hi Ter:x5001;Tom:x5002;Sumana:;";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestParallelMapThenMap() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("bold", "[<x>]", ["x"]);
        group.DefineTemplate("test", "hi <names,phones:{n,p | <n>:<p>;}:bold()>", ["names", "phones"]);
        var st = group.FindTemplate("test");
        st.Add("names", "Ter");
        st.Add("names", "Tom");
        st.Add("names", "Sumana");
        st.Add("phones", "x5001");
        st.Add("phones", "x5002");
        const string expected = "hi [Ter:x5001;][Tom:x5002;][Sumana:;]";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestMapThenParallelMap() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("bold", "[<x>]", ["x"]);
        group.DefineTemplate("test", "hi <[names:bold()],phones:{n,p | <n>:<p>;}>", ["names", "phones"]);
        var st = group.FindTemplate("test");
        st.Add("names", "Ter");
        st.Add("names", "Tom");
        st.Add("names", "Sumana");
        st.Add("phones", "x5001");
        st.Add("phones", "x5002");
        const string expected = "hi [Ter]:x5001;[Tom]:x5002;[Sumana]:;";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestMapIndexes() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("inc", "<i>:<x>", ["x", "i"]);
        group.DefineTemplate("test", "<name:{n|<inc(n,i)>}; separator=\", \">", ["name"]);
        var st = group.FindTemplate("test");
        st.Add("name", "Ter");
        st.Add("name", "Tom");
        st.Add("name", null); // don't count this one
        st.Add("name", "Sumana");
        const string expected = "1:Ter, 2:Tom, 3:Sumana";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestMapIndexes2() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<name:{n | <i>:<n>}; separator=\", \">", ["name"]);
        var st = group.FindTemplate("test");
        st.Add("name", "Ter");
        st.Add("name", "Tom");
        st.Add("name", null); // don't count this one. still can't apply subtemplate to null value
        st.Add("name", "Sumana");
        const string expected = "1:Ter, 2:Tom, 3:Sumana";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestMapSingleValue() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("a", "[<x>]", ["x"]);
        group.DefineTemplate("test", "hi <name:a()>!", ["name"]);
        var st = group.FindTemplate("test");
        st.Add("name", "Ter");
        const string expected = "hi [Ter]!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestMapNullValue() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("a", "[<x>]", ["x"]);
        group.DefineTemplate("test", "hi <name:a()>!", ["name"]);
        var st = group.FindTemplate("test");
        const string expected = "hi !";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestMapNullValueInList() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<name; separator=\", \">", ["name"]);
        var st = group.FindTemplate("test");
        st.Add("name", "Ter");
        st.Add("name", "Tom");
        st.Add("name", null); // don't print this one
        st.Add("name", "Sumana");
        const string expected = "Ter, Tom, Sumana";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestRepeatedMap() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("a", "[<x>]", ["x"]);
        group.DefineTemplate("b", "(<x>)", ["x"]);
        group.DefineTemplate("test", "hi <name:a():b()>!", ["name"]);
        var st = group.FindTemplate("test");
        st.Add("name", "Ter");
        st.Add("name", "Tom");
        st.Add("name", "Sumana");
        const string expected = "hi ([Ter])([Tom])([Sumana])!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestRepeatedMapWithNullValue() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("a", "[<x>]", ["x"]);
        group.DefineTemplate("b", "(<x>)", ["x"]);
        group.DefineTemplate("test", "hi <name:a():b()>!", ["name"]);
        var st = group.FindTemplate("test");
        st.Add("name", "Ter");
        st.Add("name", null);
        st.Add("name", "Sumana");
        const string expected = "hi ([Ter])([Sumana])!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestRepeatedMapWithNullValueAndNullOption() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("a", "[<x>]", ["x"]);
        group.DefineTemplate("b", "(<x>)", ["x"]);
        group.DefineTemplate("test", "hi <name:a():b(); null={x}>!", ["name"]);
        var st = group.FindTemplate("test");
        st.Add("name", "Ter");
        st.Add("name", null);
        st.Add("name", "Sumana");
        const string expected = "hi ([Ter])x([Sumana])!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestRoundRobinMap() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("a", "[<x>]", ["x"]);
        group.DefineTemplate("b", "(<x>)", ["x"]);
        group.DefineTemplate("test", "hi <name:a(),b()>!", ["name"]);
        var st = group.FindTemplate("test");
        st.Add("name", "Ter");
        st.Add("name", "Tom");
        st.Add("name", "Sumana");
        const string expected = "hi [Ter](Tom)[Sumana]!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestTrueCond() {
        const string template = "<if(name)>works<endif>";
        var st = new Template(template);
        st.Add("name", "Ter");
        const string expected = "works";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEmptyIFTemplate() {
        const string template = "<if(x)>fail<elseif(name)><endif>";
        var st = new Template(template);
        st.Add("name", "Ter");
        var result = st.Render();
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void TestCondParens() {
        const string template = "<if(!(x||y)&&!z)>works<endif>";
        var st = new Template(template);
        const string expected = "works";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestFalseCond() {
        const string template = "<if(name)>works<endif>";
        var st = new Template(template);
        var result = st.Render();
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void TestFalseCond2() {
        const string template = "<if(name)>works<endif>";
        var st = new Template(template);
        st.Add("name", null);
        var result = st.Render();
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void TestFalseCondWithFormalArgs() {
        // insert of indent instr was not working; ok now
        var dir = TmpDir;
        const string groupFile =
            "a(scope) ::= <<\n" +
            "foo\n" +
            "    <if(scope)>oops<endif>\n" +
            "bar\n" +
            ">>\n";
        WriteFile(dir, "group.stg", groupFile);
        var group = _templateFactory.CreateTemplateGroupFile(dir + "/group.stg").Build();
        var st = group.FindTemplate("a");
        TestContext.WriteLine(st.GetCompiledTemplate().ToString());
        var expected = $"foo{newline}bar";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestElseIf2() {
        const string template =
            "<if(x)>fail1<elseif(y)>fail2<elseif(z)>works<else>fail3<endif>";
        var st = new Template(template);
        st.Add("z", "blort");
        const string expected = "works";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestElseIf3() {
        const string template =
            "<if(x)><elseif(y)><elseif(z)>works<else><endif>";
        var st = new Template(template);
        st.Add("z", "blort");
        const string expected = "works";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestNotTrueCond() {
        const string template = "<if(!name)>works<endif>";
        var st = new Template(template);
        st.Add("name", "Ter");
        var result = st.Render();
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void TestNotFalseCond() {
        const string template = "<if(!name)>works<endif>";
        var st = new Template(template);
        const string expected = "works";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestParensInConditional() {
        const string template = "<if((a||b)&&(c||d))>works<endif>";
        var st = new Template(template);
        st.Add("a", true);
        st.Add("b", true);
        st.Add("c", true);
        st.Add("d", true);
        const string expected = "works";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestParensInConditional2() {
        const string template = "<if((!a||b)&&!(c||d))>broken<else>works<endif>";
        var st = new Template(template);
        st.Add("a", true);
        st.Add("b", true);
        st.Add("c", true);
        st.Add("d", true);
        const string expected = "works";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestTrueCondWithElse() {
        const string template = "<if(name)>works<else>fail<endif>";
        var st = new Template(template);
        st.Add("name", "Ter");
        const string expected = "works";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestFalseCondWithElse() {
        const string template = "<if(name)>fail<else>works<endif>";
        var st = new Template(template);
        const string expected = "works";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestElseIf() {
        const string template = "<if(name)>fail<elseif(id)>works<else>fail<endif>";
        var st = new Template(template);
        st.Add("id", "2DF3DF");
        const string expected = "works";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestElseIfNoElseAllFalse() {
        const string template = "<if(name)>fail<elseif(id)>fail<endif>";
        var st = new Template(template);
        var result = st.Render();
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void TestElseIfAllExprFalse() {
        const string template = "<if(name)>fail<elseif(id)>fail<else>works<endif>";
        var st = new Template(template);
        const string expected = "works";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestOr() {
        const string template = "<if(name||notThere)>works<else>fail<endif>";
        var st = new Template(template);
        st.Add("name", "Ter");
        const string expected = "works";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestMapConditionAndEscapeInside() {
        var template = @"<if(m.name)>works \\<endif>";
        var st = new Template(template);
        IDictionary<string, string> m = new Dictionary<string, string>();
        m["name"] = "Ter";
        st.Add("m", m);
        const string expected= "works \\";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestAnd() {
        var template = "<if(name&&notThere)>fail<else>works<endif>";
        var st = new Template(template);
        st.Add("name", "Ter");
        const string expected= "works";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestAndNot() {
        var template = "<if(name&&!notThere)>works<else>fail<endif>";
        var st = new Template(template);
        st.Add("name", "Ter");
        const string expected= "works";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestCharLiterals() {
        var st = new Template(
            "Foo <\\n><\\n><\\t> bar\n"
        );
        var sw = new StringWriter();
        st.Write(new AutoIndentWriter(sw, "\n")); // force \n as newline
        var result = sw.ToString();
        const string expected1 = "Foo \n\n\t bar\n";     // expect \n in output
        Assert.AreEqual(expected1, result);

        st = new Template($"Foo <\\n><\\t> bar{newline}");
        sw = new StringWriter();
        st.Write(new AutoIndentWriter(sw, "\n")); // force \n as newline
        const string expected2 = "Foo \n\t bar\n";     // expect \n in output
        result = sw.ToString();
        Assert.AreEqual(expected2, result);

        st = new Template(@"Foo<\ >bar<\n>");
        sw = new StringWriter();
        st.Write(new AutoIndentWriter(sw, "\n")); // force \n as newline
        result = sw.ToString();
        const string expected3 = "Foo bar\n"; // forced \n
        Assert.AreEqual(expected3, result);
    }

    [TestMethod]
    public void TestUnicodeLiterals() {
        var st = new Template(
            "Foo <\\uFEA5><\\n><\\u00C2> bar\n"
        );
        var expected1 = $"Foo \ufea5{newline}\u00C2 bar{newline}";
        var result = st.Render();
        Assert.AreEqual(expected1, result);

        st = new Template($"Foo <\\uFEA5><\\n><\\u00C2> bar{newline}");
        var expected2 = $"Foo \ufea5{newline}\u00C2 bar{newline}";
        result = st.Render();
        Assert.AreEqual(expected2, result);

        st = new Template("Foo<\\ >bar<\\n>");
        var expected3 = $"Foo bar{newline}";
        result = st.Render();
        Assert.AreEqual(expected3, result);
    }

    [TestMethod]
    public void TestSubtemplateExpr() {
        const string template = "<{name\n}>";
        var st = new Template(template);
        var expected = $"name{newline}";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSeparator() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<names:{n | case <n>}; separator=\", \">", ["names"]);
        var st = group.FindTemplate("test");
        st.Add("names", "Ter");
        st.Add("names", "Tom");
        const string expected = "case Ter, case Tom";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSeparatorInList() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<names:{n | case <n>}; separator=\", \">", ["names"]);
        var st = group.FindTemplate("test");
        st.Add("names", new List<string>() { "Ter", "Tom" });
        const string expected = "case Ter, case Tom";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSeparatorInList2() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<names:{n | case <n>}; separator=\", \">", ["names"]);
        var st = group.FindTemplate("test");
        st.Add("names", "Ter");
        st.Add("names", new List<string>(["Tom", "Sriram"]));
        const string expected = "case Ter, case Tom, case Sriram";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSeparatorInArray() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<names:{n | case <n>}; separator=\", \">", ["names"]);
        var st = group.FindTemplate("test");
        st.Add("names", new[] { "Ter", "Tom" });
        const string expected = "case Ter, case Tom";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSeparatorInArray2() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<names:{n | case <n>}; separator=\", \">", ["names"]);
        var st = group.FindTemplate("test");
        st.Add("names", "Ter");
        st.Add("names", new[] { "Tom", "Sriram" });
        const string expected = "case Ter, case Tom, case Sriram";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSeparatorInPrimitiveArray() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<names:{n | case <n>}; separator=\", \">", ["names"]);
        var st = group.FindTemplate("test");
        st.Add("names", new[] { 0, 1 });
        const string expected = "case 0, case 1";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSeparatorInPrimitiveArray2() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<names:{n | case <n>}; separator=\", \">", ["names"]);
        var st = group.FindTemplate("test");
        st.Add("names", 0);
        st.Add("names", new[] { 1, 2 });
        const string expected = "case 0, case 1, case 2";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    /** (...) forces early eval to string.
     * We need an STWriter so I must pick one.  toString(...) is used to
     * ensure b is property name in &lt;a.b&gt;.  It's used to eval default args
     * (usually strings). It's used to eval option values (usually strings).
     * So in general no-indent is fine.  Now, if I used indent-writer, it
     * would mostly work too.  What about &lt;(t())&gt; when t() is huge and indented,
     * but you had called render() with a no-indent-writer?  now *part* your
     * input is indented!
     */
    [TestMethod]
    public void TestEarlyEvalIndent() {
        const string templates =
            "t() ::= <<  abc>>\n" +
            "main() ::= <<\n" +
            "<t()>\n" +
            "<(t())>\n" + // early eval ignores indents; mostly for simply strings
            "  <t()>\n" +
            "  <(t())>\n" +
            ">>\n";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var st = group.FindTemplate("main");
        var result = st.Render();
        var expected =
           $"  abc{newline}" +
           $"abc{newline}" +
           $"    abc{newline}" +
            "  abc";
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEarlyEvalNoIndent() {
        const string templates =
            "t() ::= <<  abc>>\n" +
            "main() ::= <<\n" +
            "<t()>\n" +
            "<(t())>\n" + // early eval ignores indents; mostly for simply strings
            "  <t()>\n" +
            "  <(t())>\n" +
            ">>\n";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var st = group.FindTemplate("main");
        var sw = new StringWriter();
        var w = new NoIndentWriter(sw);
        st.Write(w);
        var result = sw.ToString();
        var expected =
            $"abc{newline}" +
            $"abc{newline}" +
            $"abc{newline}" +
            "abc";
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestArrayOfTemplates() {
        const string template = "<foo>!";
        var st = new Template(template);
        var t = new Template[] { new ("hi"), new ("mom") };
        st.Add("foo", t);
        const string expected = "himom!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestArrayOfTemplatesInTemplate() {
        const string template = "<foo>!";
        var st = new Template(template);
        var t = new Template[] { new ("hi"), new ("mom") };
        st.Add("foo", t);
        var wrapper = new Template("<x>");
        wrapper.Add("x", st);
        const string expected = "himom!";
        var result = wrapper.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestListOfTemplates() {
        const string template = "<foo>!";
        var st = new Template(template);
        var t = new List<Template> { new("hi"), new ("mom") };
        st.Add("foo", t);
        const string expected = "himom!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestListOfTemplatesInTemplate() {
        const string template = "<foo>!";
        var st = new Template(template);
        var t = new List<Template> { new ("hi"), new ("mom") };
        st.Add("foo", t);
        var wrapper = new Template("<x>");
        wrapper.Add("x", st);
        const string expected = "himom!";
        var result = wrapper.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Playing() {
        const string template = "<a:t(x,y),u()>";
        var st = new Template(template);
        TestContext.WriteLine(st.impl.ToString());
    }

    [TestMethod]
    public void TestPrototype() {
        var prototype = new Template("simple template");

        var st = new Template(prototype);
        st.Add("arg1", "value");
        Assert.AreEqual("simple template", st.Render());

        var st2 = new Template(prototype);
        st2.Add("arg1", "value");
        Assert.AreEqual("simple template", st2.Render());
    }

    [TestMethod]
    public void TestFormatPositionalArguments() {
        const string n = "n";
        const string p = "p";
        const string expected = "n:p";
        var actual = Template.Format("<%1>:<%2>", n, p);
        Assert.AreEqual(expected, actual);
    }

}
