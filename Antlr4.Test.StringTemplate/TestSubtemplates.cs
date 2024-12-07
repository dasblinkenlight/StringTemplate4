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
using Antlr4.StringTemplate.Misc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Path = System.IO.Path;

[TestClass]
public class TestSubtemplates : BaseTest {

    [TestMethod]
    public void TestSimpleIteration() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<names:{n|<n>}>!", ["names"]);
        var st = group.FindTemplate("test");
        st.Add("names", "Ter");
        st.Add("names", "Tom");
        st.Add("names", "Sumana");
        const string expected = "TerTomSumana!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestMapIterationIsByKeys() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<emails:{n|<n>}>!", ["emails"]);
        var st = group.FindTemplate("test");
        var emails = new Dictionary<string, string> {
            ["parrt"] = "Ter",
            ["tombu"] = "Tom",
            ["dmose"] = "Dan"
        };
        st.Add("emails", emails);
        const string expected = "parrttombudmose!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSimpleIterationWithArg() {
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
    public void TestNestedIterationWithArg() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<users:{u | <u.id:{id | <id>=}><u.name>}>!", ["users"]);
        var st = group.FindTemplate("test");
        st.Add("users", new User(1, "parrt"));
        st.Add("users", new User(2, "tombu"));
        st.Add("users", new User(3, "sri"));
        const string expected = "1=parrt2=tombu3=sri!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSubtemplateAsDefaultArg() {
        var templates =
            "t(x,y={<x:{s|<s><s>}>}) ::= <<\n" +
            "x: <x>\n" +
            "y: <y>\n" +
           $">>{newline}";
        WriteFile(TmpDir, "group.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "group.stg")).Build();
        var b = group.FindTemplate("t");
        b.Add("x", "a");
        var expecting = $"x: a{newline}y: aa";
        var result = b.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestParallelAttributeIteration() {
        var e = _templateFactory.CreateTemplate(
            "<names,phones,salaries:{n,p,s | <n>@<p>: <s>\n}>"
        );
        e.Add("names", "Ter");
        e.Add("names", "Tom");
        e.Add("phones", "1");
        e.Add("phones", "2");
        e.Add("salaries", "big");
        e.Add("salaries", "huge");
        var expecting = $"Ter@1: big{newline}Tom@2: huge{newline}";
        Assert.AreEqual(expecting, e.Render());
    }

    [TestMethod]
    public void TestParallelAttributeIterationWithNullValue() {
        var e = _templateFactory.CreateTemplate(
            "<names,phones,salaries:{n,p,s | <n>@<p>: <s>\n}>"
        );
        e.Add("names", "Ter");
        e.Add("names", "Tom");
        e.Add("names", "Sriram");
        e.Add("phones", new List<string> { "1", null, "3" });
        e.Add("salaries", "big");
        e.Add("salaries", "huge");
        e.Add("salaries", "enormous");
        var expecting =
           $"Ter@1: big{newline}" +
           $"Tom@: huge{newline}" +
           $"Sriram@3: enormous{newline}";
        Assert.AreEqual(expecting, e.Render());
    }

    [TestMethod]
    public void TestParallelAttributeIterationHasI() {
        var e = _templateFactory.CreateTemplate(
            "<names,phones,salaries:{n,p,s | <i0>. <n>@<p>: <s>\n}>"
        );
        e.Add("names", "Ter");
        e.Add("names", "Tom");
        e.Add("phones", "1");
        e.Add("phones", "2");
        e.Add("salaries", "big");
        e.Add("salaries", "huge");
        var expecting =
           $"0. Ter@1: big{newline}" +
           $"1. Tom@2: huge{newline}";
        Assert.AreEqual(expecting, e.Render());
    }

    [TestMethod]
    public void TestParallelAttributeIterationWithDifferentSizes() {
        var e = _templateFactory.CreateTemplate(
            "<names,phones,salaries:{n,p,s | <n>@<p>: <s>}; separator=\", \">"
        );
        e.Add("names", "Ter");
        e.Add("names", "Tom");
        e.Add("names", "Sriram");
        e.Add("phones", "1");
        e.Add("phones", "2");
        e.Add("salaries", "big");
        const string expecting = "Ter@1: big, Tom@2: , Sriram@: ";
        Assert.AreEqual(expecting, e.Render());
    }

    [TestMethod]
    public void TestParallelAttributeIterationWithSingletons() {
        var e = _templateFactory.CreateTemplate(
            "<names,phones,salaries:{n,p,s | <n>@<p>: <s>}; separator=\", \">"
        );
        e.Add("names", "Ter");
        e.Add("phones", "1");
        e.Add("salaries", "big");
        const string expecting = "Ter@1: big";
        Assert.AreEqual(expecting, e.Render());
    }

    [TestMethod]
    public void TestParallelAttributeIterationWithDifferentSizesTemplateRefInsideToo() {
        var templates =
           $"page(names,phones,salaries) ::= {newline}" +
            "	<< <names,phones,salaries:{n,p,s | <value(n)>@<value(p)>: <value(s)>}; separator=\", \"> >>" + newline +
           $"value(x) ::= \"<if(!x)>n/a<else><x><endif>\"{newline}";
        WriteFile(TmpDir, "g.stg", templates);

        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "g.stg")).Build();
        var p = group.FindTemplate("page");
        p.Add("names", "Ter");
        p.Add("names", "Tom");
        p.Add("names", "Sriram");
        p.Add("phones", "1");
        p.Add("phones", "2");
        p.Add("salaries", "big");
        var expecting = " Ter@1: big, Tom@2: n/a, Sriram@n/a: n/a ";
        Assert.AreEqual(expecting, p.Render());
    }

    [TestMethod]
    public void TestEvalSTIteratingSubtemplateInSTFromAnotherGroup() {
        var errors = new ErrorBuffer();
        var innerGroup = new TemplateGroup {
            Listener = errors
        };
        innerGroup.DefineTemplate("test", "<m:samegroup()>", ["m"]);
        innerGroup.DefineTemplate("samegroup", "hi ", ["x"]);
        var st = innerGroup.FindTemplate("test");
        st.Add("m", new[] { 1, 2, 3 });

        var outerGroup = _templateFactory.CreateTemplateGroup().Build();
        outerGroup.DefineTemplate("errorMessage", "<x>", ["x"]);
        var outerST = outerGroup.FindTemplate("errorMessage");
        outerST.Add("x", st);

        const string expected = "hi hi hi ";
        var result = outerST.Render();

        Assert.AreEqual(errors.Errors.Count, 0); // ignores no such prop errors

        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEvalSTIteratingSubtemplateInSTFromAnotherGroupSingleValue() {
        var errors = new ErrorBuffer();
        var innerGroup = new TemplateGroup {
            Listener = errors
        };
        innerGroup.DefineTemplate("test", "<m:samegroup()>", ["m"]);
        innerGroup.DefineTemplate("samegroup", "hi ", ["x"]);
        var st = innerGroup.FindTemplate("test");
        st.Add("m", 10);

        var outerGroup = _templateFactory.CreateTemplateGroup().Build();
        outerGroup.DefineTemplate("errorMessage", "<x>", ["x"]);
        var outerST = outerGroup.FindTemplate("errorMessage");
        outerST.Add("x", st);

        const string expected = "hi ";
        var result = outerST.Render();

        Assert.AreEqual(errors.Errors.Count, 0); // ignores no such prop errors

        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEvalSTFromAnotherGroup() {
        var errors = new ErrorBuffer();
        var innerGroup = new TemplateGroup {
            Listener = errors
        };
        innerGroup.DefineTemplate("bob", "inner");
        var st = innerGroup.FindTemplate("bob");

        var outerGroup = new TemplateGroup {
            Listener = errors
        };
        outerGroup.DefineTemplate("errorMessage", "<x>", ["x"]);
        outerGroup.DefineTemplate("bob", "outer"); // should not be visible to test() in innerGroup
        var outerST = outerGroup.FindTemplate("errorMessage");
        outerST.Add("x", st);

        var expected = "inner";
        var result = outerST.Render();

        Assert.AreEqual(errors.Errors.Count, 0); // ignores no such prop errors

        Assert.AreEqual(expected, result);
    }
}
