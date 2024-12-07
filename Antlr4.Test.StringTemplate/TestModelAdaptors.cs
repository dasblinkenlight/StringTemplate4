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

using Antlr4.StringTemplate;
using Antlr4.StringTemplate.Misc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Path = System.IO.Path;

[TestClass]
public class TestModelAdaptors : BaseTest {

    private class UserAdaptor : IModelAdaptor {
        public object GetProperty(Interpreter interpreter, TemplateFrame frame, object o, object property, string propertyName) {
            return propertyName switch {
                "id" => ((User)o).id,
                "name" => ((User)o).Name,
                _ => throw new TemplateNoSuchPropertyException(null, "User." + propertyName)
            };
        }
    }

    private class UserAdaptorConst : IModelAdaptor {
        public object GetProperty(Interpreter interpreter, TemplateFrame frame, object o, object property, string propertyName) {
            return propertyName switch {
                "id" => "const id value",
                "name" => "const name value",
                _ => throw new TemplateNoSuchPropertyException(null, "User." + propertyName)
            };
        }
    }

    private class SuperUser : User {
#pragma warning disable 414 // The field 'name' is assigned but its value is never used
        private readonly int bitmask;
#pragma warning restore 414
        public SuperUser(int id, string name) : base(id, name) {
            bitmask = 0x8080;
        }
        public override string Name => $"super {base.Name}";
    }

    [TestMethod]
    public void TestSimpleAdaptor() {
        const string templates = "foo(x) ::= \"<x.id>: <x.name>\"\n";
        WriteFile(TmpDir, "foo.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "foo.stg")).Build();
        group.RegisterModelAdaptor(typeof(User), new UserAdaptor());
        var st = group.GetInstanceOf("foo");
        st.Add("x", new User(100, "parrt"));
        const string expected = "100: parrt";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestAdaptorAndBadProp() {
        var errors = new ErrorBufferAllErrors();
        const string templates = "foo(x) ::= \"<x.qqq>\"\n";
        WriteFile(TmpDir, "foo.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "foo.stg")).WithErrorListener(errors).Build();
        group.RegisterModelAdaptor(typeof(User), new UserAdaptor());
        var st = group.GetInstanceOf("foo");
        st.Add("x", new User(100, "parrt"));
        var result = st.Render();
        Assert.AreEqual(string.Empty, result);

        var msg = (TemplateRuntimeMessage)errors.Errors[0];
        var e = (TemplateNoSuchPropertyException)msg.Cause;
        Assert.AreEqual("User.qqq", e.PropertyName);
    }

    [TestMethod]
    public void TestAdaptorCoversSubclass() {
        const string templates = "foo(x) ::= \"<x.id>: <x.name>\"\n";
        WriteFile(TmpDir, "foo.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "foo.stg")).Build();
        group.RegisterModelAdaptor(typeof(User), new UserAdaptor());
        var st = group.GetInstanceOf("foo");
        st.Add("x", new SuperUser(100, "parrt")); // create subclass of User
        const string expected = "100: super parrt";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestWeCanResetAdaptorCacheInvalidatedUponAdaptorReset() {
        const string templates = "foo(x) ::= \"<x.id>: <x.name>\"\n";
        WriteFile(TmpDir, "foo.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "foo.stg")).Build();
        group.RegisterModelAdaptor(typeof(User), new UserAdaptor());
        group.GetModelAdaptor(typeof(User)); // get User, SuperUser into cache
        group.GetModelAdaptor(typeof(SuperUser));

        group.RegisterModelAdaptor(typeof(User), new UserAdaptorConst());
        // cache should be reset so we see new adaptor
        var st = group.GetInstanceOf("foo");
        st.Add("x", new User(100, "parrt"));
        const string expected = "const id value: const name value"; // sees UserAdaptorConst
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSeesMostSpecificAdaptor() {
        const string templates = "foo(x) ::= \"<x.id>: <x.name>\"\n";
        WriteFile(TmpDir, "foo.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "foo.stg")).Build();
        group.RegisterModelAdaptor(typeof(User), new UserAdaptor());
        group.RegisterModelAdaptor(typeof(SuperUser), new UserAdaptorConst()); // most specific
        var st = group.GetInstanceOf("foo");
        st.Add("x", new User(100, "parrt"));
        const string expected1 = "100: parrt";
        var result = st.Render();
        Assert.AreEqual(expected1, result);

        st.Remove("x");
        st.Add("x", new SuperUser(100, "parrt"));
        const string expected2 = "const id value: const name value"; // sees UserAdaptorConst
        result = st.Render();
        Assert.AreEqual(expected2, result);
    }

}
