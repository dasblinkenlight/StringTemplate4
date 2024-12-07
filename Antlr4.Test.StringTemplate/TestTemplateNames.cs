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

using Antlr4.StringTemplate.Debug;

namespace Antlr4.Test.StringTemplate;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Path = System.IO.Path;

[TestClass]
public class TestTemplateNames : BaseTest {

    [TestMethod]
    public void TestAbsoluteTemplateRefFromOutside() {
        // /randomdir/a and /randomdir/subdir/b
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a(x) ::= << </subdir/b()> >>\n");
        WriteFile(Path.Combine(dir, "subdir"), "b.st", "b() ::= <<bar>>\n");
        var group = _templateFactory.CreateTemplateGroupDirectory(dir).Build();
        Assert.AreEqual(" bar ", group.FindTemplate("a").Render());
        Assert.AreEqual(" bar ", group.FindTemplate("/a").Render());
        Assert.AreEqual("bar", group.FindTemplate("/subdir/b").Render());
    }


    [TestMethod]
    public void TestRelativeTemplateRefInExpr() {
        // /randomdir/a and /randomdir/subdir/b
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a(x) ::= << <subdir/b()> >>\n");
        WriteFile(Path.Combine(dir, "subdir"), "b.st", "b() ::= <<bar>>\n");
        var group = _templateFactory.CreateTemplateGroupDirectory(dir).Build();
        Assert.AreEqual(" bar ", group.FindTemplate("a").Render());
    }

    [TestMethod]
    public void TestAbsoluteTemplateRefInExpr() {
        // /randomdir/a and /randomdir/subdir/b
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a(x) ::= << </subdir/b()> >>\n");
        WriteFile(Path.Combine(dir, "subdir"), "b.st", "b() ::= <<bar>>\n");
        var group = _templateFactory.CreateTemplateGroupDirectory(dir).Build();
        Assert.AreEqual(" bar ", group.FindTemplate("a").Render());
    }

    [TestMethod]
    public void TestRefToAnotherTemplateInSameGroup() {
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a() ::= << <b()> >>\n");
        WriteFile(dir, "b.st", "b() ::= <<bar>>\n");
        var group = _templateFactory.CreateTemplateGroupDirectory(dir).Build();
        var st = group.FindTemplate("a");
        const string expected = " bar ";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestRefToAnotherTemplateInSameSubdir() {
        // /randomdir/a and /randomdir/subdir/b
        var dir = TmpDir;
        WriteFile(Path.Combine(dir, "subdir"), "a.st", "a() ::= << <b()> >>\n");
        WriteFile(Path.Combine(dir, "subdir"), "b.st", "b() ::= <<bar>>\n");
        var group = _templateFactory.CreateTemplateGroupDirectory(dir).Build();
        TestContext.WriteLine(group.FindTemplate("/subdir/a").GetCompiledTemplate().ToString());
        Assert.AreEqual(" bar ", group.FindTemplate("/subdir/a").Render());
    }

    [TestMethod]
    public void TestFullyQualifiedGetInstanceOf() {
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a(x) ::= <<foo>>");
        var group = _templateFactory.CreateTemplateGroupDirectory(dir).Build();
        Assert.AreEqual("foo", group.FindTemplate("a").Render());
        Assert.AreEqual("foo", group.FindTemplate("/a").Render());
    }

    [TestMethod]
    public void TestFullyQualifiedTemplateRef() {
        // /randomdir/a and /randomdir/subdir/b
        var dir = TmpDir;
        WriteFile(Path.Combine(dir, "subdir"), "a.st", "a() ::= << </subdir/b()> >>\n");
        WriteFile(Path.Combine(dir, "subdir"), "b.st", "b() ::= <<bar>>\n");
        var group = _templateFactory.CreateTemplateGroupDirectory(dir).Build();

        var template = group.FindTemplate("/subdir/a");
        Assert.IsNotNull(template);
        Assert.AreEqual(" bar ", template.Render());

        template = group.FindTemplate("subdir/a");
        Assert.IsNotNull(template);
        Assert.AreEqual(" bar ", template.Render());
    }

    [TestMethod]
    public void TestFullyQualifiedTemplateRef2() {
        // /randomdir/a and /randomdir/group.stg with b and c templates
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a(x) ::= << </group/b()> >>\n");
        const string groupFile =
            "b() ::= \"bar\"\n" +
            "c() ::= \"</a()>\"\n";
        WriteFile(dir, "group.stg", groupFile);
        var group = _templateFactory.CreateTemplateGroupDirectory(dir).Build();

        var st1 = group.FindTemplate("/a");
        Assert.IsNotNull(st1);
        Assert.AreEqual(" bar ", st1.Render());

        var st2 = group.FindTemplate("/group/c"); // invokes /a
        Assert.IsNotNull(st2);
        Assert.AreEqual(" bar ", st2.Render());
    }

    [TestMethod]
    public void TestRelativeInSubdir() {
        // /randomdir/a and /randomdir/subdir/b
        var dir = TmpDir;
        WriteFile(dir, "a.st", "a(x) ::= << </subdir/c()> >>\n");
        WriteFile(Path.Combine(dir, "subdir"), "b.st", "b() ::= <<bar>>\n");
        WriteFile(Path.Combine(dir, "subdir"), "c.st", "c() ::= << <b()> >>\n");
        var group = _templateFactory.CreateTemplateGroupDirectory(dir).Build();
        Assert.AreEqual("  bar  ", group.FindTemplate("a").Render());
    }

    // TODO: test <a/b()> is RELATIVE NOT ABSOLUTE
}
