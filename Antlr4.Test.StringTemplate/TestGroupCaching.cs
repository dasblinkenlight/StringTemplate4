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

using Antlr4.StringTemplate;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Antlr4.Test.StringTemplate;

[TestClass]
public class TestGroupCaching : BaseTest {

    private string originalRoot;

    [TestInitialize]
    public void Initialize() {
        TemplateGroup.ResourceAssembly = typeof(TestGroupCaching).Assembly;
        originalRoot = TemplateGroup.ResourceRoot;
        TemplateGroup.ResourceRoot = "Resources/caching";

        // Warm up cache
        var tgDir = _templateFactory.CreateTemplateGroupDirectory("").WithCaching().Build();
        Assert.IsTrue(tgDir.IsDefined("cachingtemplate"));
        var templateGroup = _templateFactory.CreateTemplateGroupFile("cachinggroup.stg").WithCaching().Build();
        Assert.IsNotNull(templateGroup);
        var st = templateGroup.GetInstanceOf("a");
        Assert.IsNotNull(st);
    }

    [TestCleanup]
    public void Cleanup() {
        TemplateGroup.ResourceAssembly = null;
        TemplateGroup.ResourceRoot = originalRoot;
    }

    [TestMethod]
    public void TestLoadTemplateGroupFromCache() {
        var stg = _templateFactory.CreateTemplateGroupFile("cachinggroup.stg").WithCaching().Build();
        Assert.IsTrue(stg.IsDefined("a"));
        var st = stg.GetInstanceOf("a");
        st.Add("x", new[] { "one", "two", "three" });
        var result = st.Render();
        const string expecting = "foo [one:one] [two:two] [three:three] bar";
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestLoadTemplateUnknownGroup() {
        var tgDir = _templateFactory.CreateTemplateGroupDirectory("").WithCaching().Build();
        var st = tgDir.GetInstanceOf("cachingtemplate");
        Assert.IsNotNull(st);
        var result = st.Render();
        const string expecting = "hello world";
        Assert.AreEqual(expecting, result);
    }

}
