/*
 * [The "BSD licence"]
 * Copyright (c) 2011 Terence Parr
 * All rights reserved.
 *
 * Conversion to C#:
 * Copyright (c) 2024 Sergey Kalinichenko
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

[TestClass]
public class TestGroupFromResource : BaseTest {

    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext testContext) {
        TemplateGroup.ResourceAssembly = typeof(TestGroupFromResource).Assembly;
    }

    [AssemblyCleanup]
    public static void AssemblyCleanup() {
        TemplateGroup.ResourceAssembly = null;
    }

    [TestMethod]
    public void TestLoadTemplateFileFromDir() {
        var stg = new TemplateGroupDirectory("org/antlr/templates/dir1");
        var st = stg.GetInstanceOf("sample");
        var result = st.Render();
        const string expecting = "a test";
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestLoadTemplateFileInSubdir() {
        var stg = new TemplateGroupDirectory("org/antlr/templates");
        var st = stg.GetInstanceOf("dir1/sample");
        var result = st.Render();
        const string expecting = "a test";
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestLoadTemplateGroupFileWithModifiedPrefix() {
        var savedPrefix = TemplateGroup.ResourceRoot;
        try {
            TemplateGroup.ResourceRoot = "Resources.org.antlr.templates.dir1";
            var stg = new TemplateGroupFile("testgroupfile.stg");
            var st = stg.GetInstanceOf("t");
            var result = st.Render();
            const string expecting = "foo";
            Assert.AreEqual(expecting, result);
        }
        finally {
            TemplateGroup.ResourceRoot = savedPrefix;
        }
    }

    [TestMethod]
    public void TestLoadTemplateGroupDirectoryWithModifiedPrefix() {
        var savedPrefix = TemplateGroup.ResourceRoot;
        try {
            TemplateGroup.ResourceRoot = "Resources.org.antlr.templates";
            var stg = new TemplateGroupDirectory("dir1");
            var st = stg.GetInstanceOf("sample");
            var result = st.Render();
            const string expecting = "a test";
            Assert.AreEqual(expecting, result);
        }
        finally {
            TemplateGroup.ResourceRoot = savedPrefix;
        }
    }

    [TestMethod]
    public void TestLoadTemplateGroupFileFromResource() {
        var stg = new TemplateGroupFile("org/antlr/templates/dir1/testgroupfile.stg");
        var st = stg.GetInstanceOf("t");
        var result = st.Render();
        const string expecting = "foo";
        Assert.AreEqual(expecting, result);
    }

}
