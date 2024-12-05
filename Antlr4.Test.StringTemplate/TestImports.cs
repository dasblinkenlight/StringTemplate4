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

using System.Runtime.CompilerServices;
using Antlr4.StringTemplate;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ErrorBuffer = Antlr4.StringTemplate.Misc.ErrorBuffer;
using Path = System.IO.Path;

[TestClass]
public class TestImports : BaseTest {

    [TestMethod]
    public void TestImportDir() {
        /*
        dir1
            g.stg has a() that imports dir2 with absolute path
        dir2
            a.st
            b.st
         */
        var dir1 = Path.Combine(TmpDir, "dir1");
        var dir2 = Path.Combine(TmpDir, "dir2");
        var gstr =
            "import \"" + dir2 + "\"\n" +
            "a() ::= <<dir1 a>>\n";
        WriteFile(dir1, "g.stg", gstr);

        const string a = "a() ::= <<dir2 a>>\n";
        const string b = "b() ::= <<dir2 b>>\n";
        WriteFile(dir2, "a.st", a);
        WriteFile(dir2, "b.st", b);

        var group = new TemplateGroupFile(Path.Combine(dir1, "g.stg"));
        var st = group.GetInstanceOf("b"); // visible only if import worked
        const string expected = "dir2 b";
        var result = st?.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestImportRelativeDir() {
        /*
        dir
            g.stg has a() that imports subdir with relative path
            subdir
                a.st
                b.st
                c.st
         */
        var dir = TmpDir;
        const string gstr =
            "import \"subdir\"\n" + // finds subdir in dir
            "a() ::= <<dir1 a>>\n";
        WriteFile(dir, "g.stg", gstr);

        const string a = "a() ::= <<subdir a>>\n";
        const string b = "b() ::= <<subdir b>>\n";
        const string c = "c() ::= <<subdir b>>\n";
        WriteFile(dir, Path.Combine("subdir", "a.st"), a);
        WriteFile(dir, Path.Combine("subdir", "b.st"), b);
        WriteFile(dir, Path.Combine("subdir", "c.st"), c);

        var group = new TemplateGroupFile(Path.Combine(dir, "g.stg"));
        var st = group.GetInstanceOf("b"); // visible only if import worked
        const string expected = "subdir b";
        var result = st?.Render();
        Assert.AreEqual(expected, result);
        st = group.GetInstanceOf("c");
        result = st?.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEmptyGroupImportGroupFileSameDir() {
        /*
        dir
            group1.stg		that imports group2.stg in same dir with just filename
            group2.stg		has c()
         */
        var dir = TmpDir;
        const string groupFile1 = "import \"group2.stg\"\n";
        WriteFile(dir, "group1.stg", groupFile1);

        const string groupFile2 = "c() ::= \"g2 c\"\n";
        WriteFile(dir, "group2.stg", groupFile2);

        var group1 = new TemplateGroupFile(Path.Combine(dir, "group1.stg"));
        var st = group1.GetInstanceOf("c"); // should see c()
        const string expected = "g2 c";
        var result = st?.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestImportGroupFileSameDir() {
        /*
        dir
            group1.stg		that imports group2.stg in same dir with just filename
            group2.stg		has c()
         */
        var dir = TmpDir;
        const string groupFile1 =
            "import \"group2.stg\"\n" +
            "a() ::= \"g1 a\"\n" +
            "b() ::= \"<c()>\"\n";
        WriteFile(dir, "group1.stg", groupFile1);

        const string groupFile2 = "c() ::= \"g2 c\"\n";
        WriteFile(dir, "group2.stg", groupFile2);

        TemplateGroup group1 = new TemplateGroupFile(Path.Combine(dir, "group1.stg"));
        var st = group1.GetInstanceOf("c"); // should see c()
        const string expected = "g2 c";
        var result = st?.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestImportRelativeGroupFile() {
        /*
        dir
            group1.stg		that imports group2.stg in same dir with just filename
            subdir
                group2.stg	has c()
         */
        var dir = TmpDir;
        const string groupFile1 =
            "import \"subdir/group2.stg\"\n" +
            "a() ::= \"g1 a\"\n" +
            "b() ::= \"<c()>\"\n";
        WriteFile(dir, "group1.stg", groupFile1);

        const string groupFile2 =
            "c() ::= \"g2 c\"\n";
        WriteFile(dir, Path.Combine("subdir", "group2.stg"), groupFile2);

        var group1 = new TemplateGroupFile(Path.Combine(dir, "group1.stg"));
        var st = group1.GetInstanceOf("c"); // should see c()
        const string expected = "g2 c";
        var result = st?.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestImportTemplateFileSameDir() {
        /*
        dir
            group1.stg		(that imports c.st)
            c.st
         */
        var dir = TmpDir;
        const string groupFile =
            "import \"c.st\"\n" +
            "a() ::= \"g1 a\"\n" +
            "b() ::= \"<c()>\"\n";
        WriteFile(dir, "group1.stg", groupFile);
        WriteFile(dir, "c.st", "c() ::= \"c\"\n");

        TemplateGroup group1 = new TemplateGroupFile(Path.Combine(dir, "group1.stg"));
        var st = group1.GetInstanceOf("c"); // should see c()
        const string expected = "c";
        var result = st?.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestImportRelativeTemplateFile() {
        /*
        dir
            group1.stg		that imports c.st
            subdir
                c.st
         */
        var dir = TmpDir;
        const string groupFile =
            "import \"subdir/c.st\"\n" +
            "a() ::= \"g1 a\"\n" +
            "b() ::= \"<c()>\"\n";
        WriteFile(dir, "group1.stg", groupFile);

        const string stFile = "c() ::= \"c\"\n";
        WriteFile(dir, Path.Combine("subdir", "c.st"), stFile);

        TemplateGroup group1 = new TemplateGroupFile(Path.Combine(dir, "group1.stg"));
        var st = group1.GetInstanceOf("c"); // should see c()
        var expected = "c";
        var result = st?.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestImportTemplateFromAnotherGroupObject() {
        /*
        dir1
            a.st
            b.st
        dir2
            a.st
         */
        var dir1 = TmpDir;
        const string a1 = "a() ::= <<dir1 a>>\n";
        const string b = "b() ::= <<dir1 b>>\n";
        WriteFile(dir1, "a.st", a1);
        WriteFile(dir1, "b.st", b);
        var dir2 = TmpDir;
        const string a2 = "a() ::= << <b()> >>\n";
        WriteFile(dir2, "a.st", a2);

        TemplateGroup group1 = new TemplateGroupDirectory(dir1);
        TemplateGroup group2 = new TemplateGroupDirectory(dir2);
        group2.ImportTemplates(group1);
        var st = group2.GetInstanceOf("b");
        const string expected1 = "dir1 b";
        var result = st?.Render();
        Assert.AreEqual(expected1, result);

        // do it again, but make a template ref imported template
        st = group2.GetInstanceOf("a");
        const string expected2 = " dir1 b ";
        result = st?.Render();
        Assert.AreEqual(expected2, result);
    }

    [TestMethod]
    public void TestImportTemplateInGroupFileFromDir() {
        var dir = TmpDir;
        const string a = "a() ::= << <b()> >>\n";
        WriteFile(dir, "x/a.st", a);

        const string groupFile =
            "b() ::= \"group file b\"\n" +
            "c() ::= \"group file c\"\n";
        WriteFile(dir, Path.Combine("y", "group.stg"), groupFile);

        var group1 = new TemplateGroupDirectory(Path.Combine(dir, "x"));
        var group2 = new TemplateGroupFile(Path.Combine(dir, "y", "group.stg"));
        group1.ImportTemplates(group2);
        var st = group1.GetInstanceOf("a");
        TestContext.WriteLine(st.impl.ToString());
        const string expected = " group file b ";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestImportTemplateInGroupFileFromGroupFile() {
        var dir = TmpDir;
        const string groupFile1 =
            "a() ::= \"g1 a\"\n" +
            "b() ::= \"<c()>\"\n";
        WriteFile(dir, Path.Combine("x", "group.stg"), groupFile1);

        const string groupFile2 =
            "b() ::= \"g2 b\"\n" +
            "c() ::= \"g2 c\"\n";
        WriteFile(dir, Path.Combine("y", "group.stg"), groupFile2);

        TemplateGroup group1 = new TemplateGroupFile(Path.Combine(dir, "x", "group.stg"));
        TemplateGroup group2 = new TemplateGroupFile(Path.Combine(dir, "y", "group.stg"));
        group1.ImportTemplates(group2);
        var st = group1.GetInstanceOf("b");
        var expected = "g2 c";
        var result = st?.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestImportTemplateFromSubdir() {
        // /randomdir/x/subdir/a and /randomdir/y/subdir/b
        var dir = TmpDir;
        const string a = "a() ::= << </subdir/b()> >>\n";
        const string b = "b() ::= <<x's subdir/b>>\n";
        WriteFile(dir, Path.Combine("x", "subdir", "a.st"), a);
        WriteFile(dir, Path.Combine("y", "subdir", "b.st"), b);

        TemplateGroup group1 = new TemplateGroupDirectory(Path.Combine(dir, "x"));
        TemplateGroup group2 = new TemplateGroupDirectory(Path.Combine(dir, "y"));
        group1.ImportTemplates(group2);
        var st = group1.GetInstanceOf("subdir/a");
        const string expected = " x's subdir/b ";
        var result = st?.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestImportTemplateFromGroupFile() {
        // /randomdir/x/subdir/a and /randomdir/y/subdir.stg which has a and b
        var dir = TmpDir;
        var a = "a() ::= << </subdir/b()> >>\n"; // get b imported from subdir.stg
        WriteFile(dir, Path.Combine("x", "subdir", "a.st"), a);

        const string groupFile =
            "a() ::= \"group file: a\"\n" +
            "b() ::= \"group file: b\"\n";
        WriteFile(dir, Path.Combine("y", "subdir.stg"), groupFile);

        TemplateGroup group1 = new TemplateGroupDirectory(Path.Combine(dir, "x"));
        TemplateGroup group2 = new TemplateGroupDirectory(Path.Combine(dir, "y"));
        group1.ImportTemplates(group2);

        var st = group1.GetInstanceOf("subdir/a");

        Assert.IsNotNull(st);
        Assert.IsNotNull(group1.GetInstanceOf("subdir/b"));

        const string expected = " group file: b ";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestPolymorphicTemplateReference() {
        var dir1 = Path.Combine(TmpDir, "d1");
        var b = "b() ::= <<dir1 b>>\n";
        WriteFile(dir1, "b.st", b);
        var dir2 = Path.Combine(TmpDir, "d2");
        const string a = "a() ::= << <b()> >>\n";
        b = "b() ::= <<dir2 b>>\n";
        WriteFile(dir2, "a.st", a);
        WriteFile(dir2, "b.st", b);

        TemplateGroup group1 = new TemplateGroupDirectory(dir1);
        TemplateGroup group2 = new TemplateGroupDirectory(dir2);
        group1.ImportTemplates(group2);

        // normal lookup; a created from dir2 calls dir2.b
        var st = group2.GetInstanceOf("a");
        const string expected1 = " dir2 b ";
        var result = st?.Render();
        Assert.AreEqual(expected1, result);

        // polymorphic lookup; a created from dir1 calls dir2.a which calls dir1.b
        st = group1.GetInstanceOf("a");
        const string expected2 = " dir1 b ";
        result = st?.Render();
        Assert.AreEqual(expected2, result);
    }

    [TestMethod]
    public void TestSuper() {
        var dir1 = Path.Combine(TmpDir, "d1");
        var a = "a() ::= <<dir1 a>>\n";
        var b = "b() ::= <<dir1 b>>\n";
        WriteFile(dir1, "a.st", a);
        WriteFile(dir1, "b.st", b);
        var dir2 = Path.Combine(TmpDir, "d2");
        a = "a() ::= << [<super.a()>] >>\n";
        WriteFile(dir2, "a.st", a);

        TemplateGroup group1 = new TemplateGroupDirectory(dir1);
        TemplateGroup group2 = new TemplateGroupDirectory(dir2);
        group2.ImportTemplates(group1);
        var st = group2.GetInstanceOf("a");
        var expected = " [dir1 a] ";
        var result = st?.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestUnloadImportedTemplate() {
        var dir1 = Path.Combine(TmpDir, "d1");
        const string a1 = "a() ::= <<dir1 a>>\n";
        const string b = "b() ::= <<dir1 b>>\n";
        WriteFile(dir1, "a.st", a1);
        WriteFile(dir1, "b.st", b);
        var dir2 = Path.Combine(TmpDir, "d2");
        const string a2 = "a() ::= << <b()> >>\n";
        WriteFile(dir2, "a.st", a2);

        TemplateGroup group1 = new TemplateGroupDirectory(dir1);
        TemplateGroup group2 = new TemplateGroupDirectory(dir2);
        group2.ImportTemplates(group1);

        var st = group2.GetInstanceOf("a");
        var st2 = group2.GetInstanceOf("b");
        var originalHashCode = RuntimeHelpers.GetHashCode(st);
        var originalHashCode2 = RuntimeHelpers.GetHashCode(st2);
        group1.Unload(); // blast cache
        st = group2.GetInstanceOf("a");
        var newHashCode = RuntimeHelpers.GetHashCode(st);
        Assert.AreEqual(originalHashCode == newHashCode, false); // diff objects

        const string expected1 = " dir1 b ";
        var result = st?.Render();
        Assert.AreEqual(expected1, result);

        st = group2.GetInstanceOf("b");
        var newHashCode2 = RuntimeHelpers.GetHashCode(st);
        Assert.AreEqual(originalHashCode2 == newHashCode2, false); // diff objects
        result = st?.Render();
        const string expected2 = "dir1 b";
        Assert.AreEqual(expected2, result);
    }

    [TestMethod]
    public void TestUnloadImportedTemplatedSpecifiedInGroupFile() {
        WriteFile(TmpDir, "t.stg",
            "import \"g1.stg\"\n\nmain() ::= <<\nv1-<f()>\n>>");
        WriteFile(TmpDir, "g1.stg", "f() ::= \"g1\"");
        WriteFile(TmpDir, "g2.stg", "f() ::= \"g2\"\nf2() ::= \"f2\"\n");
        var group = new TemplateGroupFile(TmpDir + "/t.stg");
        var st = group.GetInstanceOf("main");
        Assert.AreEqual("v1-g1", st?.Render());

        // Change the imports of group t.
        WriteFile(TmpDir, "t.stg",
            "import \"g2.stg\"\n\nmain() ::= <<\nv2-<f()>;<f2()>\n>>");
        group.Unload(); // will also unload already imported groups
        st = group.GetInstanceOf("main");
        Assert.AreEqual("v2-g2;f2", st?.Render());
    }

    /** Cannot import from a group file unless it's the root.
     */
    [TestMethod]
    public void TestGroupFileInDirImportsAnotherGroupFile() {
        // /randomdir/group.stg with a() imports /randomdir/imported.stg with b()
        // can't have groupdir then groupfile inside that imports
        var dir = TmpDir;
        const string groupFile =
            "import \"imported.stg\"\n" +
            "a() ::= \"a: <b()>\"\n";
        WriteFile(dir, "group.stg", groupFile);
        var importedFile =
            "b() ::= \"b\"\n";
        WriteFile(dir, "imported.stg", importedFile);
        ITemplateErrorListener errors = new ErrorBuffer();
        var group = new TemplateGroupDirectory(dir);
        group.Listener = errors;
        group.GetInstanceOf("/group/a");
        var result = errors.ToString();
        const string substring =
            "import illegal in group files embedded in TemplateGroupDirectory; import \"imported.stg\" in TemplateGroupDirectory";
        StringAssert.Contains(result, substring);
    }

    [TestMethod]
    public void TestGroupFileInDirImportsAGroupDir() {
        /*
        dir
            g.stg has a() that imports subdir with relative path
            subdir
                b.st
                c.st
         */
        var dir = TmpDir;
        const string gstr =
            "import \"subdir\"\n" + // finds subdir in dir
            "a() ::= \"a: <b()>\"\n";
        WriteFile(dir, "g.stg", gstr);

        WriteFile(dir, "subdir/b.st", "b() ::= \"b: <c()>\"\n");
        WriteFile(dir, "subdir/c.st", "c() ::= <<subdir c>>\n");

        var group = new TemplateGroupFile(dir + "/g.stg");
        var st = group.GetInstanceOf("a");
        const string expected = "a: b: subdir c";
        var result = st?.Render();
        Assert.AreEqual(expected, result);
    }

}
