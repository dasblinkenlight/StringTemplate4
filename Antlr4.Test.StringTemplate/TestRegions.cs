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

using Antlr4.StringTemplate;
using Antlr4.StringTemplate.Misc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Path = System.IO.Path;

namespace Antlr4.Test.StringTemplate;

[TestClass]
public class TestRegions : BaseTest {

    [TestMethod]
    public void TestEmbeddedRegion() {
        var dir = TmpDir;
        const string groupFile =
            "a() ::= <<\n" +
            "[<@r>bar<@end>]\n" +
            ">>\n";
        WriteFile(dir, "group.stg", groupFile);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "group.stg")).Build();
        var st = group.GetInstanceOf("a");
        var expected = "[bar]";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestRegion() {
        var dir = TmpDir;
        const string groupFile =
            "a() ::= <<\n" +
            "[<@r()>]\n" +
            ">>\n";
        WriteFile(dir, "group.stg", groupFile);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "group.stg")).Build();
        var st = group.GetInstanceOf("a");
        const string expected = "[]";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefineRegionInSubgroup() {
        var dir = TmpDir;
        WriteFile(dir, "g1.stg", "a() ::= <<[<@r()>]>>\n");
        WriteFile(dir, "g2.stg", "@a.r() ::= <<foo>>\n");

        var group1 = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "g1.stg")).Build();
        var group2 = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "g2.stg")).Build();
        group2.ImportTemplates(group1); // define r in g2
        var st = group2.GetInstanceOf("a");
        const string expected = "[foo]";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefineRegionInSubgroupOneInSubdir() {
        var dir = TmpDir;
        WriteFile(dir, "g1.stg", "a() ::= <<[<@r()>]>>\n");
        WriteFile(Path.Combine(dir, "subdir"), "g2.stg", "@a.r() ::= <<foo>>\n");

        var group1 = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "g1.stg")).Build();
        var group2 = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "subdir", "g2.stg")).Build();
        group2.ImportTemplates(group1); // define r in g2
        var st = group2.GetInstanceOf("a");
        const string expected = "[foo]";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefineRegionInSubgroupBothInSubdir() {
        var dir = TmpDir;
        WriteFile(Path.Combine(dir, "subdir"), "g1.stg", "a() ::= <<[<@r()>]>>\n");
        WriteFile(Path.Combine(dir, "subdir"), "g2.stg", "@a.r() ::= <<foo>>\n");

        var group1 = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "subdir", "g1.stg")).Build();
        var group2 = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "subdir", "g2.stg")).Build();
        group2.ImportTemplates(group1); // define r in g2
        var st = group2.GetInstanceOf("a");
        const string expected = "[foo]";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefineRegionInSubgroupThatRefsSuper() {
        var dir = TmpDir;
        const string g1 = "a() ::= <<[<@r>foo<@end>]>>\n";
        WriteFile(dir, "g1.stg", g1);
        const string g2 = "@a.r() ::= <<(<@super.r()>)>>\n";
        WriteFile(dir, "g2.stg", g2);

        var group1 = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "g1.stg")).Build();
        var group2 = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "g2.stg")).Build();
        group2.ImportTemplates(group1); // define r in g2
        var st = group2.GetInstanceOf("a");
        const string expected = "[(foo)]";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefineRegionInSubgroup2() {
        var dir = TmpDir;
        const string g1 = "a() ::= <<[<@r()>]>>\n";
        WriteFile(dir, "g1.stg", g1);
        const string g2 = "@a.r() ::= <<foo>>>\n";
        WriteFile(dir, "g2.stg", g2);

        var group1 = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "g1.stg")).Build();
        var group2 = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "g2.stg")).Build();
        group1.ImportTemplates(group2); // opposite of previous; g1 imports g2
        var st = group1.GetInstanceOf("a");
        const string expected = "[]"; // @a.r implicitly defined in g1; can't see g2's
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDefineRegionInSameGroup() {
        var dir = TmpDir;
        const string g = "a() ::= <<[<@r()>]>>\n" +
                         "@a.r() ::= <<foo>>\n";
        WriteFile(dir, "g.stg", g);

        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "g.stg")).Build();
        var st = group.GetInstanceOf("a");
        const string expected = "[foo]";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestAnonymousTemplateInRegion() {
        var dir = TmpDir;
        const string g =
            "a() ::= <<[<@r()>]>>\n" +
            "@a.r() ::= <<\n"+
            "<[\"foo\"]:{x|<x>}>\n"+
            ">>\n";
        WriteFile(dir, "g.stg", g);

        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "g.stg")).Build();
        var st = group.GetInstanceOf("a");
        const string expected = "[foo]";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestAnonymousTemplateInRegionInSubdir() {
        //fails since it makes region name /region__/g/a/_r
        var dir = TmpDir;
        const string g =
            "a() ::= <<[<@r()>]>>\n" +
            "@a.r() ::= <<\n" +
            "<[\"foo\"]:{x|<x>}>\n" +
            ">>\n";
        WriteFile(dir, "g.stg", g);

        var group = _templateFactory.CreateTemplateGroupDirectory(dir).Build();
        var st = group.GetInstanceOf("g/a");
        const string expected = "[foo]";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestCantDefineEmbeddedRegionAgain() {
        var dir = TmpDir;
        const string g =
            "a() ::= <<[<@r>foo<@end>]>>\n" +
            "@a.r() ::= <<bar>>\n"; // error; dup
        WriteFile(dir, "g.stg", g);

        var errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "g.stg")).WithErrorListener(errors).Build();
        group.Load();
        var expected = $"g.stg 2:3: the explicit definition of region /a.r hides an embedded definition in the same group{newline}";
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestCantDefineEmbeddedRegionAgainInTemplate() {
        var dir = TmpDir;
        const string g =
            "a() ::= <<\n" +
            "[\n" +
            "<@r>foo<@end>\n" +
            "<@r()>\n" +
            "]\n" +
            ">>\n"; // error; dup
        WriteFile(dir, "g.stg", g);

        var errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "g.stg")).WithErrorListener(errors).Build();
        group.Load();
        Assert.AreEqual(0, errors.Errors.Count);

        var template = group.GetInstanceOf("a");
        var expected =
            $"[{newline}" +
            $"foo{newline}" +
            $"foo{newline}]";
        var result = template.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestMissingRegionName() {
        var dir = TmpDir;
        const string g = "@t.() ::= \"\"\n";
        WriteFile(dir, "g.stg", g);

        var errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "g.stg")).WithErrorListener(errors).Build();
        group.Load();
        var expected = $"g.stg 1:3: missing ID at '('{newline}";
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestIndentBeforeRegionIsIgnored() {
        var dir = TmpDir;
        const string g =
            "a() ::= <<[\n" +
            "  <@r>\n" +
            "  foo\n" +
            "  <@end>\n" +
            "]>>\n";
        WriteFile(dir, "g.stg", g);

        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "g.stg")).Build();
        var st = group.GetInstanceOf("a");
        var expected =
            $"[{newline}" +
            $"  foo{newline}]";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestRegionOverrideStripsNewlines() {
        var dir = TmpDir;
        var g =
            "a() ::= \"X<@r()>Y\"" +
            "@a.r() ::= <<\n" +
            "foo\n" +
            ">>\n";
        WriteFile(dir, "g.stg", g);

        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "g.stg")).Build();
        var sub = $"@a.r() ::= \"A<@super.r()>B\"{newline}";
        WriteFile(dir, "sub.stg", sub);
        var subGroup = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "sub.stg")).Build();
        subGroup.ImportTemplates(group);
        var st = subGroup.GetInstanceOf("a");
        var result = st.Render();
        const string expecting = "XAfooBY";
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestRegionOverrideRefSuperRegion() {
        var dir = TmpDir;
        var g = $"a() ::= \"X<@r()>Y\"@a.r() ::= \"foo\"{newline}";
        WriteFile(dir, "g.stg", g);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "g.stg")).Build();

        var sub = $"@a.r() ::= \"A<@super.r()>B\"{newline}";
        WriteFile(dir, "sub.stg", sub);
        var subGroup = _templateFactory.CreateTemplateGroupFile(dir + "/sub.stg").Build();
        subGroup.ImportTemplates(group);

        var st = subGroup.GetInstanceOf("a");
        var result = st.Render();
        const string expecting = "XAfooBY";
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestRegionOverrideRefSuperRegion2Levels() {
        const string g =
            "a() ::= \"X<@r()>Y\"\n" +
            "@a.r() ::= \"foo\"\n";
        var group = _templateFactory.CreateTemplateGroupString(g).Build();

        const string sub = "@a.r() ::= \"<@super.r()>2\"\n";
        var subGroup = _templateFactory.CreateTemplateGroupString(sub).Build();
        subGroup.ImportTemplates(group);

        var st = subGroup.GetInstanceOf("a");

        var result = st.Render();
        const string expecting = "Xfoo2Y";
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestRegionOverrideRefSuperRegion3Levels() {
        var dir = TmpDir;
        var g = $"a() ::= \"X<@r()>Y\"@a.r() ::= \"foo\"{newline}";
        WriteFile(dir, "g.stg", g);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "g.stg")).Build();

        var sub = $"@a.r() ::= \"<@super.r()>2\"{newline}";
        WriteFile(dir, "sub.stg", sub);
        var subGroup = _templateFactory.CreateTemplateGroupFile(dir + "/sub.stg").Build();
        subGroup.ImportTemplates(group);

        var subsub = $"@a.r() ::= \"<@super.r()>3\"{newline}";
        WriteFile(dir, "subsub.stg", subsub);
        var subSubGroup = _templateFactory.CreateTemplateGroupFile(dir + "/subsub.stg").Build();
        subSubGroup.ImportTemplates(subGroup);

        var st = subSubGroup.GetInstanceOf("a");

        var result = st.Render();
        const string expecting = "Xfoo23Y";
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestRegionOverrideRefSuperImplicitRegion() {
        var dir = TmpDir;
        var g = $"a() ::= \"X<@r>foo<@end>Y\"{newline}";
        WriteFile(dir, "g.stg", g);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "g.stg")).Build();

        var sub = $"@a.r() ::= \"A<@super.r()>\"{newline}";
        WriteFile(dir, "sub.stg", sub);
        var subGroup = _templateFactory.CreateTemplateGroupFile(dir + "/sub.stg").Build();
        subGroup.ImportTemplates(group);

        var st = subGroup.GetInstanceOf("a");
        var result = st.Render();
        const string expecting = "XAfooY";
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestUnknownRegionDefError() {
        var dir = TmpDir;
        var g =
            "a() ::= <<\n" +
            "X<@r()>Y\n" +
            ">>\n" +
           $"@a.q() ::= \"foo\"{newline}";
        ITemplateErrorListener errors = new ErrorBuffer();
        WriteFile(dir, "g.stg", g);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "g.stg")).WithErrorListener(errors).Build();
        var st = group.GetInstanceOf("a");
        st.Render();
        var result = errors.ToString();
        var expecting = $"g.stg 4:3: template /a doesn't have a region called q{newline}";
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestSuperRegionRefMissingOk() {
        var dir = TmpDir;
        var g =
            "a() ::= \"X<@r()>Y\"" +
           $"@a.r() ::= \"foo\"{newline}";
        WriteFile(dir, "g.stg", g);
        var errors = new ErrorBuffer();
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "g.stg")).WithErrorListener(errors).Build();

        var sub = $"@a.r() ::= \"A<@super.q()>B\"{newline}"; // allow this; trap at runtime
        WriteFile(dir, "sub.stg", sub);
        var subGroup = _templateFactory.CreateTemplateGroupFile(dir + "/sub.stg").Build();
        subGroup.ImportTemplates(group);

        var st = subGroup.GetInstanceOf("a");
        var result = st.Render();
        const string expecting = "XABY";
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestEmbeddedRegionOnOneLine() {
        var dir = TmpDir;
        const string groupFile =
            "a() ::= <<\n" +
            "[\n" +
            "  <@r>bar<@end>\n" +
            "]\n" +
            ">>\n";
        WriteFile(dir, "group.stg", groupFile);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "group.stg")).Build();
        var st = group.GetInstanceOf("a");
        TestContext.WriteLine(st.impl.ToString());
        var expected = $"[{newline}  bar{newline}]";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEmbeddedRegionTagsOnSeparateLines() {
        var dir = TmpDir;
        const string groupFile =
            "a() ::= <<\n" +
            "[\n" +
            "  <@r>\n" +
            "  bar\n" +
            "  <@end>\n" +
            "]\n" +
            ">>\n";
        WriteFile(dir, "group.stg", groupFile);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "group.stg")).Build();
        var st = group.GetInstanceOf("a");
        var expected = $"[{newline}  bar{newline}]";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [Ignore("will revisit the behavior of indented expressions spanning multiple lines for a future release")]
    [TestMethod]
    public void TestEmbeddedSubtemplate() {
        // fix so we ignore inside {...}
        var dir = TmpDir;
        const string groupFile =
            "a() ::= <<\n" +
            "[\n" +
            "  <{\n" +
            "  bar\n" +
            "  }>\n" +
            "]\n" +
            ">>\n";
        WriteFile(dir, "group.stg", groupFile);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(dir, "group.stg")).Build();
        var st = group.GetInstanceOf("a");
        var expected = $"[{newline}  bar{newline}]";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

}
