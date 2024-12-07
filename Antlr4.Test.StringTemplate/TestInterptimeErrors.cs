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
public class TestInterptimeErrors : BaseTest {

    public class UserHiddenName {
        protected string name;
        public UserHiddenName(string name) {
            this.name = name;
        }
        protected string getName() => name;
    }

    public class UserHiddenNameField {
        protected string name;
        public UserHiddenNameField(string name) {
            this.name = name;
        }
    }

    [TestMethod]
    public void TestMissingEmbeddedTemplate() {
        var errors = new ErrorBuffer();

        var templates = $"t() ::= \"<foo()>\"{newline}";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).WithErrorListener(errors).Build();
        var st = group.FindTemplate("t");
        st.Render();
        var expected = "context [/t] 1:1 no such template: /foo" + newline;
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestMissingSuperTemplate() {
        var errors = new ErrorBuffer();

        var templates = $"t() ::= \"<super.t()>\"{newline}";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).WithErrorListener(errors).Build();
        var templates2 = $"u() ::= \"blech\"{newline}";

        WriteFile(TmpDir, "t2.stg", templates2);
        var group2 = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t2.stg")).Build();
        group.ImportTemplates(group2);
        var st = group.FindTemplate("t");
        st.Render();
        var expected = "context [/t] 1:1 no such template: super.t" + newline;
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestNoPropertyNotError() {
        var errors = new ErrorBuffer();

        var templates = $"t(u) ::= \"<u.x>\"{newline}";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).WithErrorListener(errors).Build();
        var st = group.FindTemplate("t");
        st.Add("u", new User(32, "parrt"));
        st.Render();
        var expected = "";
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestHiddenPropertyNotError() {
        var errors = new ErrorBuffer();

        var templates = $"t(u) ::= \"<u.name>\"{newline}";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).WithErrorListener(errors).Build();
        var st = group.FindTemplate("t");
        st.Add("u", new UserHiddenName("parrt"));
        st.Render();
        var expected = "";
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestHiddenFieldNotError() {
        var errors = new ErrorBuffer();

        var templates = $"t(u) ::= \"<u.name>\"{newline}";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).WithErrorListener(errors).Build();
        var st = group.FindTemplate("t");
        st.Add("u", new UserHiddenNameField("parrt"));
        st.Render();
        var expected = "";
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSoleArg() {
        var errors = new ErrorBuffer();

        const string templates =
            "t() ::= \"<u({9})>\"\n" +
            "u(x,y) ::= \"<x>\"\n";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).WithErrorListener(errors).Build();
        var st = group.FindTemplate("t");
        st.Render();
        var expected = "context [/t] 1:1 passed 1 arg(s) to template /u with 2 declared arg(s)" + newline;
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSoleArgUsingApplySyntax() {
        var errors = new ErrorBuffer();

        const string templates =
            "t() ::= \"<{9}:u()>\"\n" +
            "u(x,y) ::= \"<x>\"\n";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).WithErrorListener(errors).Build();
        var st = group.FindTemplate("t");
        var expected = "9";
        var result = st.Render();
        Assert.AreEqual(expected, result);

        expected = "context [/t] 1:5 passed 1 arg(s) to template /u with 2 declared arg(s)" + newline;
        result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestUndefinedAttr() {
        var errors = new ErrorBuffer();

        const string templates =
            "t() ::= \"<u()>\"\n" +
            "u() ::= \"<x>\"\n";

        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).WithErrorListener(errors).Build();
        var st = group.FindTemplate("t");
        st.Render();
        var expected = "context [/t /u] 1:1 attribute x isn't defined" + newline;
        var result = errors.ToString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestParallelAttributeIterationWithMissingArgs() {
        var errors = new ErrorBuffer();
        var group = new TemplateGroup {
            Listener = errors
        };
        var e = _templateFactory.CreateTemplate(
            "<names,phones,salaries:{n,p | <n>@<p>}; separator=\", \">",
            group
        );
        e.Add("names", "Ter");
        e.Add("names", "Tom");
        e.Add("phones", "1");
        e.Add("phones", "2");
        e.Add("salaries", "big");
        e.Render();
        var errorExpecting =
            $"1:23: anonymous template has 2 arg(s) but mapped across 3 value(s){newline}" +
            $"context [anonymous] 1:23 passed 3 arg(s) to template /_sub1 with 2 declared arg(s){newline}" +
            $"context [anonymous] 1:1 iterating through 3 values in zip map but template has 2 declared arguments{newline}";
        Assert.AreEqual(errorExpecting, errors.ToString());
        var expecting = "Ter@1, Tom@2";
        Assert.AreEqual(expecting, e.Render());
    }

    [TestMethod]
    public void TestStringTypeMismatch() {
        var errors = new ErrorBuffer();
        var group = new TemplateGroup {
            Listener = errors
        };
        var e = _templateFactory.CreateTemplate("<trim(s)>", group);
        e.Add("s", 34);
        e.Render(); // generate the error
        var errorExpecting = $"context [anonymous] 1:1 function trim expects a string not System.Int32{newline}";
        Assert.AreEqual(errorExpecting, errors.ToString());
    }

    [TestMethod]
    public void TestStringTypeMismatch2() {
        var errors = new ErrorBuffer();
        var group = new TemplateGroup {
            Listener = errors
        };
        var e = _templateFactory.CreateTemplate("<strlen(s)>", group);
        e.Add("s", 34);
        e.Render(); // generate the error
        var errorExpecting = $"context [anonymous] 1:1 function strlen expects a string not System.Int32{newline}";
        Assert.AreEqual(errorExpecting, errors.ToString());
    }

}
