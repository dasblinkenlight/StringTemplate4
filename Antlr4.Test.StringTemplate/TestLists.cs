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

namespace Antlr4.Test.StringTemplate;

using Antlr4.StringTemplate;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Path = System.IO.Path;

[TestClass]
public class TestLists : BaseTest {

    [TestMethod]
    public void TestJustCat() {
        var e = _templateFactory.CreateTemplate(
            "<[names,phones]>"
        );
        e.Add("names", "Ter");
        e.Add("names", "Tom");
        e.Add("phones", "1");
        e.Add("phones", "2");
        const string expecting = "TerTom12";
        Assert.AreEqual(expecting, e.Render());
    }

    [TestMethod]
    public void TestListLiteralWithEmptyElements() {
        var e = _templateFactory.CreateTemplate(
            "<[\"Ter\",,\"Jesse\"]:{n | <i>:<n>}; separator=\", \", null={foo}>"
        );
        const string expecting = "1:Ter, foo, 2:Jesse";
        Assert.AreEqual(expecting, e.Render());
    }

    [TestMethod]
    public void TestListLiteralWithEmptyFirstElement() {
        var e = _templateFactory.CreateTemplate(
            "<[,\"Ter\",\"Jesse\"]:{n | <i>:<n>}; separator=\", \", null={foo}>"
        );
        const string expecting = "foo, 1:Ter, 2:Jesse";
        Assert.AreEqual(expecting, e.Render());
    }

    [TestMethod]
    public void TestLength() {
        var e = _templateFactory.CreateTemplate(
            "<length([names,phones])>"
        );
        e.Add("names", "Ter");
        e.Add("names", "Tom");
        e.Add("phones", "1");
        e.Add("phones", "2");
        const string expecting = "4";
        Assert.AreEqual(expecting, e.Render());
    }

    [TestMethod]
    public void TestCat2Attributes() {
        var e = _templateFactory.CreateTemplate(
            "<[names,phones]; separator=\", \">"
        );
        e.Add("names", "Ter");
        e.Add("names", "Tom");
        e.Add("phones", "1");
        e.Add("phones", "2");
        const string expecting = "Ter, Tom, 1, 2";
        Assert.AreEqual(expecting, e.Render());
    }

    [TestMethod]
    public void TestCat2AttributesWithApply() {
        var e = _templateFactory.CreateTemplate(
            "<[names,phones]:{a|<a>.}>"
        );
        e.Add("names", "Ter");
        e.Add("names", "Tom");
        e.Add("phones", "1");
        e.Add("phones", "2");
        const string expecting = "Ter.Tom.1.2.";
        Assert.AreEqual(expecting, e.Render());
    }

    [TestMethod]
    public void TestCat3Attributes() {
        var e = _templateFactory.CreateTemplate(
            "<[names,phones,salaries]; separator=\", \">"
        );
        e.Add("names", "Ter");
        e.Add("names", "Tom");
        e.Add("phones", "1");
        e.Add("phones", "2");
        e.Add("salaries", "big");
        e.Add("salaries", "huge");
        const string expecting = "Ter, Tom, 1, 2, big, huge";
        Assert.AreEqual(expecting, e.Render());
    }

    [TestMethod]
    public void TestCatWithTemplateApplicationAsElement() {
        var e = _templateFactory.CreateTemplate(
            "<[names:{n|<n>!},phones]; separator=\", \">"
        );
        e.Add("names", "Ter");
        e.Add("names", "Tom");
        e.Add("phones", "1");
        e.Add("phones", "2");
        const string expecting = "Ter!, Tom!, 1, 2";
        Assert.AreEqual(expecting, e.Render());
    }

    [TestMethod]
    public void TestCatWithIFAsElement() {
        var e = _templateFactory.CreateTemplate(
            "<[{<if(names)>doh<endif>},phones]; separator=\", \">"
        );
        e.Add("names", "Ter");
        e.Add("names", "Tom");
        e.Add("phones", "1");
        e.Add("phones", "2");
        const string expecting = "doh, 1, 2";
        Assert.AreEqual(expecting, e.Render());
    }

    [TestMethod]
    public void TestCatNullValues() {
        // [a, b] must behave like <a><b>; if a==b==null, blank output
        // unless null argument.
        var e = _templateFactory.CreateTemplate(
            "<[no,go]; null=\"foo\", separator=\", \">"
        );
        e.Add("phones", "1");
        e.Add("phones", "2");
        const string expecting = "foo, foo";
        Assert.AreEqual(expecting, e.Render());
    }

    [TestMethod]
    public void TestCatWithNullTemplateApplicationAsElement() {
        var e = _templateFactory.CreateTemplate(
            "<[names:{n|<n>!},\"foo\"]:{a|x}; separator=\", \">"
        );
        e.Add("phones", "1");
        e.Add("phones", "2");
        const string expecting = "x";  // only one since template application gives nothing
        Assert.AreEqual(expecting, e.Render());
    }

    [TestMethod]
    public void TestCatWithNestedTemplateApplicationAsElement() {
        var e = _templateFactory.CreateTemplate(
            "<[names, [\"foo\",\"bar\"]:{x | <x>!},phones]; separator=\", \">"
        );
        e.Add("names", "Ter");
        e.Add("names", "Tom");
        e.Add("phones", "1");
        e.Add("phones", "2");
        const string expecting = "Ter, Tom, foo!, bar!, 1, 2";
        Assert.AreEqual(expecting, e.Render());
    }

    [TestMethod]
    public void TestListAsTemplateArgument() {
        var templates =
                "test(names,phones) ::= \"<foo([names,phones])>\"" + newline +
                "foo(items) ::= \"<items:{a | *<a>*}>\"" + newline
            ;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var e = group.GetInstanceOf("test");
        e.Add("names", "Ter");
        e.Add("names", "Tom");
        e.Add("phones", "1");
        e.Add("phones", "2");
        const string expecting = "*Ter**Tom**1**2*";
        var result = e.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestListWithTwoEmptyListsCollapsesToEmptyList() {
        var e = _templateFactory.CreateTemplate(
            "<[[],[]]:{x | <x>!}; separator=\", \">"
        );
        e.Add("names", "Ter");
        e.Add("names", "Tom");
        Assert.AreEqual(string.Empty, e.Render());
    }

}
