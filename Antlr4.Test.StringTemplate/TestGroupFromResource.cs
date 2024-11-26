using System;
using System.Reflection;

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
    [TestCategory(TestCategories.ST4)]
    public void TestLoadTemplateFileFromDir() {
        TemplateGroupDirectory stg = new TemplateGroupDirectory("org/antlr/templates/dir1");
        Template st = stg.GetInstanceOf("sample");
        string result = st.Render();
        string expecting = "a test";
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    [TestCategory(TestCategories.ST4)]
    public void TestLoadTemplateFileInSubdir() {
        TemplateGroupDirectory stg = new TemplateGroupDirectory("org/antlr/templates");
        Template st = stg.GetInstanceOf("dir1/sample");
        string result = st.Render();
        string expecting = "a test";
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    [TestCategory(TestCategories.ST4)]
    public void TestLoadTemplateGroupFileWithModifiedPrefix() {
        var savedPrefix = TemplateGroup.ResourceRoot;
        try {
            TemplateGroup.ResourceRoot = "Resources.org.antlr.templates.dir1";
            TemplateGroupFile stg = new TemplateGroupFile("testgroupfile.stg");
            Template st = stg.GetInstanceOf("t");
            string result = st.Render();
            string expecting = "foo";
            Assert.AreEqual(expecting, result);
        }
        finally {
            TemplateGroup.ResourceRoot = savedPrefix;
        }
    }

    [TestMethod]
    [TestCategory(TestCategories.ST4)]
    public void TestLoadTemplateGroupDirectoryWithModifiedPrefix() {
        var savedPrefix = TemplateGroup.ResourceRoot;
        try {
            TemplateGroup.ResourceRoot = "Resources.org.antlr.templates";
            TemplateGroupDirectory stg = new TemplateGroupDirectory("dir1");
            Template st = stg.GetInstanceOf("sample");
            string result = st.Render();
            string expecting = "a test";
            Assert.AreEqual(expecting, result);
        }
        finally {
            TemplateGroup.ResourceRoot = savedPrefix;
        }
    }

    [TestMethod]
    [TestCategory(TestCategories.ST4)]
    public void TestLoadTemplateGroupFileFromResource() {
        TemplateGroupFile stg = new TemplateGroupFile("org/antlr/templates/dir1/testgroupfile.stg");
        Template st = stg.GetInstanceOf("t");
        string result = st.Render();
        string expecting = "foo";
        Assert.AreEqual(expecting, result);
    }

}
