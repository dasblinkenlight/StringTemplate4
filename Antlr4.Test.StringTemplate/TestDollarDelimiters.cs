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
using Path = System.IO.Path;

[TestClass]
public class TestDollarDelimiters : BaseTest {

    [TestMethod]
    public void TestAttr() {
        const string template = "hi $name$!";
        var st = _templateFactory.CreateTemplate(template, '$', '$');
        st.Add("name", "Ter");
        const string expected = "hi Ter!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestParallelMap() {
        var group = _templateFactory.CreateTemplateGroup().WithDelimiters('$', '$').Build();
        group.DefineTemplate("test", "hi $names,phones:{n,p | $n$:$p$;}$", ["names", "phones"]);
        var st = group.FindTemplate("test");
        st.Add("names", "Ter");
        st.Add("names", "Tom");
        st.Add("names", "Sumana");
        st.Add("phones", "x5001");
        st.Add("phones", "x5002");
        st.Add("phones", "x5003");
        const string expected =
            "hi Ter:x5001;Tom:x5002;Sumana:x5003;";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestRefToAnotherTemplateInSameGroup() {
        var dir = TmpDir;
        const string a = "a() ::= << <$b()$> >>\n";
        const string b = "b() ::= <<bar>>\n";
        WriteFile(dir, "a.st", a);
        WriteFile(dir, "b.st", b);
        var group = _templateFactory.CreateTemplateGroupDirectory(dir).WithDelimiters('$', '$').Build();
        var st = group.FindTemplate("a");
        const string expected = " <bar> ";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefaultArgument() {
        var templates =
                "method(name) ::= <<" + newline +
                "$stat(name)$" + newline +
                ">>" + newline +
                "stat(name,value=\"99\") ::= \"x=$value$; // $name$\"" + newline
            ;
        WriteFile(TmpDir, "group.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "group.stg")).WithDelimiters('$', '$').Build();
        var b = group.FindTemplate("method");
        b.Add("name", "foo");
        const string expected = "x=99; // foo";
        var result = b.Render();
        Assert.AreEqual(expected, result);
    }

    /// <summary>
    /// This is part of a regression test for antlr/stringtemplate4#46.
    /// </summary>
    /// <seealso href="https://github.com/antlr/stringtemplate4/issues/46">STGroupString does not honor delimiter stanza in a string definition</seealso>
    [TestMethod]
    public void TestDelimitersClause() {
        var templates =
                "delimiters \"$\", \"$\"" + newline +
                "method(name) ::= <<" + newline +
                "$stat(name)$" + newline +
                ">>" + newline +
                "stat(name,value=\"99\") ::= \"x=$value$; // $name$\"" + newline
            ;
        WriteFile(TmpDir, "group.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(TmpDir + "/group.stg").Build();
        var b = group.FindTemplate("method");
        b.Add("name", "foo");
        const string expected = "x=99; // foo";
        var result = b.Render();
        Assert.AreEqual(expected, result);
    }

    /// <summary>
    /// This is part of a regression test for antlr/stringtemplate4#46.
    /// </summary>
    /// <seealso href="https://github.com/antlr/stringtemplate4/issues/46">STGroupString does not honor delimiter stanza in a string definition</seealso>
    [TestMethod]
    public void TestDelimitersClauseInGroupString() {
        var templates =
                "delimiters \"$\", \"$\"" + newline +
                "method(name) ::= <<" + newline +
                "$stat(name)$" + newline +
                ">>" + newline +
                "stat(name,value=\"99\") ::= \"x=$value$; // $name$\"" + newline
            ;
        var group = _templateFactory.CreateTemplateGroupString(templates).Build();
        var b = group.FindTemplate("method");
        b.Add("name", "foo");
        const string expected = "x=99; // foo";
        var result = b.Render();
        Assert.AreEqual(expected, result);
    }

    /// <summary>
    /// This is part of a regression test for antlr/stringtemplate4#66.
    /// </summary>
    /// <seealso href="https://github.com/antlr/stringtemplate4/issues/66">Changing delimiters doesn't work with STGroupFile</seealso>
    [TestMethod]
    public void TestImportTemplatePreservesDelimiters() {
        var groupFile =
            "group GenerateHtml;" + newline +
            "import \"html.st\"" + newline +
            "entry() ::= <<" + newline +
            "$html()$" + newline +
            ">>" + newline;
        var htmlFile =
            "html() ::= <<" + newline +
            "<table style=\"stuff\">" + newline +
            ">>" + newline;

        var dir = TmpDir;
        WriteFile(dir, "GenerateHtml.stg", groupFile);
        WriteFile(dir, "html.st", htmlFile);

        var group = _templateFactory.CreateTemplateGroupFile(dir + "/GenerateHtml.stg").WithDelimiters('$', '$').Build();

        // test html template directly
        var st = group.FindTemplate("html");
        Assert.IsNotNull(st);
        const string expected1 = "<table style=\"stuff\">";
        var result = st.Render();
        Assert.AreEqual(expected1, result);

        // test from entry template
        st = group.FindTemplate("entry");
        Assert.IsNotNull(st);
        const string expected2 = "<table style=\"stuff\">";
        result = st.Render();
        Assert.AreEqual(expected2, result);
    }

    /// <summary>
    /// This is part of a regression test for antlr/stringtemplate4#66.
    /// </summary>
    /// <seealso href="https://github.com/antlr/stringtemplate4/issues/66">Changing delimiters doesn't work with STGroupFile</seealso>
    [TestMethod]
    public void TestImportGroupPreservesDelimiters() {
        var groupFile =
            "group GenerateHtml;" + newline +
            "import \"HtmlTemplates.stg\"" + newline +
            "entry() ::= <<" + newline +
            "$html()$" + newline +
            ">>" + newline;
        var htmlFile =
            "html() ::= <<" + newline +
            "<table style=\"stuff\">" + newline +
            ">>" + newline;

        var dir = TmpDir;
        WriteFile(dir, "GenerateHtml.stg", groupFile);
        WriteFile(dir, "HtmlTemplates.stg", htmlFile);

        var group = _templateFactory.CreateTemplateGroupFile(dir + "/GenerateHtml.stg").WithDelimiters('$', '$').Build();

        // test html template directly
        var st = group.FindTemplate("html");
        Assert.IsNotNull(st);
        const string expected1 = "<table style=\"stuff\">";
        var result = st.Render();
        Assert.AreEqual(expected1, result);

        // test from entry template
        st = group.FindTemplate("entry");
        Assert.IsNotNull(st);
        const string expected2 = "<table style=\"stuff\">";
        result = st.Render();
        Assert.AreEqual(expected2, result);
    }

    /// <summary>
    /// This is part of a regression test for antlr/stringtemplate4#66.
    /// </summary>
    /// <seealso href="https://github.com/antlr/stringtemplate4/issues/66">Changing delimiters doesn't work with STGroupFile</seealso>
    [TestMethod]
    public void TestDelimitersClauseOverridesConstructorDelimiters() {
        var groupFile =
            "group GenerateHtml;" + newline +
            "delimiters \"$\", \"$\"" + newline +
            "import \"html.st\"" + newline +
            "entry() ::= <<" + newline +
            "$html()$" + newline +
            ">>" + newline;
        var htmlFile =
            "html() ::= <<" + newline +
            "<table style=\"stuff\">" + newline +
            ">>" + newline;

        var dir = TmpDir;
        WriteFile(dir, "GenerateHtml.stg", groupFile);
        WriteFile(dir, "html.st", htmlFile);

        var group = _templateFactory.CreateTemplateGroupFile(dir + "/GenerateHtml.stg").WithDelimiters('<', '>').Build();

        // test html template directly
        var st = group.FindTemplate("html");
        Assert.IsNotNull(st);
        const string expected1 = "<table style=\"stuff\">";
        var result = st.Render();
        Assert.AreEqual(expected1, result);

        // test from entry template
        st = group.FindTemplate("entry");
        Assert.IsNotNull(st);
        const string expected2 = "<table style=\"stuff\">";
        result = st.Render();
        Assert.AreEqual(expected2, result);
    }

    /// <summary>
    /// This is part of a regression test for antlr/stringtemplate4#66.
    /// </summary>
    /// <seealso href="https://github.com/antlr/stringtemplate4/issues/66">Changing delimiters doesn't work with STGroupFile</seealso>
    [TestMethod]
    public void TestDelimitersClauseOverridesInheritedDelimiters() {
        var groupFile =
            "group GenerateHtml;" + newline +
            "delimiters \"<\", \">\"" + newline +
            "import \"HtmlTemplates.stg\"" + newline +
            "entry() ::= <<" + newline +
            "<html()>" + newline +
            ">>" + newline;
        var htmlFile =
            "delimiters \"$\", \"$\"" + newline +
            "html() ::= <<" + newline +
            "<table style=\"stuff\">" + newline +
            ">>" + newline;

        var dir = TmpDir;
        WriteFile(dir, "GenerateHtml.stg", groupFile);
        WriteFile(dir, "HtmlTemplates.stg", htmlFile);

        var group = _templateFactory.CreateTemplateGroupFile(dir + "/GenerateHtml.stg").Build();

        // test html template directly
        var st = group.FindTemplate("html");
        Assert.IsNotNull(st);
        const string expected1 = "<table style=\"stuff\">";
        var result = st.Render();
        Assert.AreEqual(expected1, result);

        // test from entry template
        st = group.FindTemplate("entry");
        Assert.IsNotNull(st);
        const string expected2 = "<table style=\"stuff\">";
        result = st.Render();
        Assert.AreEqual(expected2, result);
    }

}
