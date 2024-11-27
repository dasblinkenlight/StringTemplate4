/*
 * [The "BSD license"]
 * Copyright (c) 2012 Terence Parr
 * All rights reserved.
 *
 * Conversion to C#:
 * Copyright (c) 2012 Sam Harwell, Tunnel Vision Laboratories, LLC
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
using System.Collections.Generic;

[TestClass]
public class TestTemplateRawGroupDirectory : BaseTest {

    [TestMethod]
    public void TestSimpleGroup() {
        var dir = TmpDir;
        WriteFile(dir, "a.st", "foo");
        var group = new TemplateRawGroupDirectory(dir, '$', '$');
        var st = group.GetInstanceOf("a");
        const string expected = "foo";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSimpleGroup2() {
        var dir = TmpDir;
        WriteFile(dir, "a.st", "foo");
        WriteFile(dir, "b.st", "$name$");
        var group = new TemplateRawGroupDirectory(dir, '$', '$');
        var st = group.GetInstanceOf("a");
        const string expected = "foo";
        var result = st.Render();
        Assert.AreEqual(expected, result);

        var b = group.GetInstanceOf("b");
        b.Add("name", "Bob");
        Assert.AreEqual("Bob", b.Render());
    }

    [TestMethod]
    public void TestSimpleGroupAngleBrackets() {
        var dir = TmpDir;
        WriteFile(dir, "a.st", "foo");
        WriteFile(dir, "b.st", "<name>");
        var group = new TemplateRawGroupDirectory(dir);
        var st = group.GetInstanceOf("a");
        const string expected = "foo";
        var result = st.Render();
        Assert.AreEqual(expected, result);

        var b = group.GetInstanceOf("b");
        b.Add("name", "Bob");
        Assert.AreEqual("Bob", b.Render());
    }

    [TestMethod]
    public void TestAnonymousTemplateInRawTemplate() {
        var dir = TmpDir;
        WriteFile(dir, "template.st", "$values:{foo|[$foo$]}$");
        var group = new TemplateRawGroupDirectory(dir, '$', '$');
        var template = group.GetInstanceOf("template");
        List<string> values = ["one", "two", "three"];
        template.Add("values", values);
        Assert.AreEqual("[one][two][three]", template.Render());
    }

    [TestMethod]
    public void TestMap() {
        var dir = TmpDir;
        WriteFile(dir, "a.st", "$names:bold()$");
        WriteFile(dir, "bold.st", "<b>$it$</b>");
        var group = new TemplateRawGroupDirectory(dir, '$', '$');
        var st = group.GetInstanceOf("a");
        List<string> names = ["parrt", "tombu"];
        st.Add("names", names);
        //string asmResult = st.impl.GetInstructions();
        //Console.Out.WriteLine(asmResult);

        //st.Visualize();
        const string expected = "<b>parrt</b><b>tombu</b>";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSuper() {
        var dir1 = $"{TmpDir}dir1011";
        const string a1 = "dir1 a";
        const string b1 = "dir1 b";
        WriteFile(dir1, "a.st", a1);
        WriteFile(dir1, "b.st", b1);
        var dir2 = $"{TmpDir}dir0220";
        var a2 = "[<super.a()>]";
        WriteFile(dir2, "a.st", a2);

        var group1 = new TemplateRawGroupDirectory(dir1);
        var group2 = new TemplateRawGroupDirectory(dir2);
        group2.ImportTemplates(group1);
        var st = group2.GetInstanceOf("a");
        const string expected = "[dir1 a]";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    /// <summary>
    /// This is a regression test for antlr/stringtemplate4#70
    /// </summary>
    /// <seealso href="https://github.com/antlr/stringtemplate4/issues/70">Argument initialisation for sub-template in template with STRawGroupDir doesn't recognize valid parameters</seealso>
    [TestMethod]
    public void TestRawArgumentPassing() {
        var dir1 = TmpDir;
        var mainRawTemplate =
            $"Hello $name${newline}" +
            $"Then do the footer:{newline}" +
            $"$footerRaw(lastLine=veryLastLineRaw())${newline}";
        var footerRawTemplate =
            $"Simple footer. And now a last line:{newline}"+
            $"$lastLine$";
        const string veryLastLineTemplate =
            "That's the last line.";
        WriteFile(dir1, "mainRaw.st", mainRawTemplate);
        WriteFile(dir1, "footerRaw.st", footerRawTemplate);
        WriteFile(dir1, "veryLastLineRaw.st", veryLastLineTemplate);

        var group = new TemplateRawGroupDirectory(dir1, '$', '$');
        var st = group.GetInstanceOf("mainRaw");
        Assert.IsNotNull(st);
        st.Add("name", "John");
        var result = st.Render();
        var expected =
            $"Hello John{newline}" +
            $"Then do the footer:{newline}" +
            $"Simple footer. And now a last line:{newline}" +
            $"That's the last line.{newline}";
        Assert.AreEqual(expected, result);
    }

}
