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

namespace Antlr4.Test.StringTemplate;

using Antlr4.StringTemplate;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using NotImplementedException = System.NotImplementedException;
using StringWriter = System.IO.StringWriter;

[TestClass]
public class TestNullAndEmptyValues : BaseTest {

    private class T {
        public string template;
        public object x;
        public string expecting;

        public string result;

        public T(string template, object x, string expecting) {
            this.template = template;
            this.x = x;
            this.expecting = expecting;
        }

        public T(T t) {
            template = t.template;
            x = t.x;
            expecting = t.expecting;
        }

        public override string ToString() {
            var s = x.ToString();
            if (x.GetType().IsArray) {
                throw new NotImplementedException();
                //s = Arrays.toString((object[])x);
            }
            return "('" + template + "', " + s + ", '" + expecting + "', '" + result + "')";
        }
    }

    private static readonly object UNDEF = "<undefined>";
    private static readonly IList<object> LIST0 = new List<object>();

    private static readonly T[] singleValuedTests = [
        new ("<x>", UNDEF, ""),
        new ("<x>", null, ""),
        new ("<x>", "", ""),
        new ("<x>", LIST0, ""),

        new ("<x:t()>", UNDEF, ""),
        new ("<x:t()>", null, ""),
        new ("<x:t()>", "", ""),
        new ("<x:t()>", LIST0, ""),

        new ("<x; null={y}>", UNDEF, "y"),
        new ("<x; null={y}>", null, "y"),
        new ("<x; null={y}>", "", ""),
        new ("<x; null={y}>", LIST0, ""),

        new ("<x:t(); null={y}>", UNDEF, "y"),
        new ("<x:t(); null={y}>", null, "y"),
        new ("<x:t(); null={y}>", "", ""),
        new ("<x:t(); null={y}>", LIST0, ""),

        new ("<if(x)>y<endif>", UNDEF, ""),
        new ("<if(x)>y<endif>", null, ""),
        new ("<if(x)>y<endif>", "", "y"),
        new ("<if(x)>y<endif>", LIST0, ""),

        new ("<if(x)>y<else>z<endif>", UNDEF, "z"),
        new ("<if(x)>y<else>z<endif>", null, "z"),
        new ("<if(x)>y<else>z<endif>", "", "y"),
        new ("<if(x)>y<else>z<endif>", LIST0, "z")
    ];

    private static readonly string[] LISTa = ["a"];
    private static readonly string[] LISTab = ["a", "b"];
    private static readonly string[] LISTnull = [null];
    private static readonly string[] LISTa_null = ["a", null];
    private static readonly string[] LISTnull_b = [null, "b"];
    private static readonly string[] LISTa_null_b = ["a", null, "b"];

    private static readonly T[] multiValuedTests = [
        new ("<x>", LIST0,        ""),
        new ("<x>", LISTa,        "a"),
        new ("<x>", LISTab,       "ab"),
        new ("<x>", LISTnull,     ""),
        new ("<x>", LISTnull_b,   "b"),
        new ("<x>", LISTa_null,   "a"),
        new ("<x>", LISTa_null_b, "ab"),

        new ("<x; null={y}>", LIST0,        ""),
        new ("<x; null={y}>", LISTa,        "a"),
        new ("<x; null={y}>", LISTab,       "ab"),
        new ("<x; null={y}>", LISTnull,     "y"),
        new ("<x; null={y}>", LISTnull_b,   "yb"),
        new ("<x; null={y}>", LISTa_null,   "ay"),
        new ("<x; null={y}>", LISTa_null_b, "ayb"),

        new ("<x; separator={,}>", LIST0,        ""),
        new ("<x; separator={,}>", LISTa,        "a"),
        new ("<x; separator={,}>", LISTab,       "a,b"),
        new ("<x; separator={,}>", LISTnull,     ""),
        new ("<x; separator={,}>", LISTnull_b,   "b"),
        new ("<x; separator={,}>", LISTa_null,   "a"),
        new ("<x; separator={,}>", LISTa_null_b, "a,b"),

        new ("<x; null={y}, separator={,}>", LIST0,        ""),
        new ("<x; null={y}, separator={,}>", LISTa,        "a"),
        new ("<x; null={y}, separator={,}>", LISTab,       "a,b"),
        new ("<x; null={y}, separator={,}>", LISTnull,     "y"),
        new ("<x; null={y}, separator={,}>", LISTnull_b,   "y,b"),
        new ("<x; null={y}, separator={,}>", LISTa_null,   "a,y"),
        new ("<x; null={y}, separator={,}>", LISTa_null_b, "a,y,b"),

        new ("<if(x)>y<endif>", LIST0,        ""),
        new ("<if(x)>y<endif>", LISTa,        "y"),
        new ("<if(x)>y<endif>", LISTab,       "y"),
        new ("<if(x)>y<endif>", LISTnull,     "y"),
        new ("<if(x)>y<endif>", LISTnull_b,   "y"),
        new ("<if(x)>y<endif>", LISTa_null,   "y"),
        new ("<if(x)>y<endif>", LISTa_null_b, "y"),

        new ("<x:{it | <it>}>", LIST0,        ""),
        new ("<x:{it | <it>}>", LISTa,        "a"),
        new ("<x:{it | <it>}>", LISTab,       "ab"),
        new ("<x:{it | <it>}>", LISTnull,     ""),
        new ("<x:{it | <it>}>", LISTnull_b,   "b"),
        new ("<x:{it | <it>}>", LISTa_null,   "a"),
        new ("<x:{it | <it>}>", LISTa_null_b, "ab"),

        new ("<x:{it | <it>}; null={y}>", LIST0,        ""),
        new ("<x:{it | <it>}; null={y}>", LISTa,        "a"),
        new ("<x:{it | <it>}; null={y}>", LISTab,       "ab"),
        new ("<x:{it | <it>}; null={y}>", LISTnull,     "y"),
        new ("<x:{it | <it>}; null={y}>", LISTnull_b,   "yb"),
        new ("<x:{it | <it>}; null={y}>", LISTa_null,   "ay"),
        new ("<x:{it | <it>}; null={y}>", LISTa_null_b, "ayb"),

        new ("<x:{it | <i>.<it>}>", LIST0,        ""),
        new ("<x:{it | <i>.<it>}>", LISTa,        "1.a"),
        new ("<x:{it | <i>.<it>}>", LISTab,       "1.a2.b"),
        new ("<x:{it | <i>.<it>}>", LISTnull,     ""),
        new ("<x:{it | <i>.<it>}>", LISTnull_b,   "1.b"),
        new ("<x:{it | <i>.<it>}>", LISTa_null,   "1.a"),
        new ("<x:{it | <i>.<it>}>", LISTa_null_b, "1.a2.b"),

        new ("<x:{it | <i>.<it>}; null={y}>", LIST0,        ""),
        new ("<x:{it | <i>.<it>}; null={y}>", LISTa,        "1.a"),
        new ("<x:{it | <i>.<it>}; null={y}>", LISTab,       "1.a2.b"),
        new ("<x:{it | <i>.<it>}; null={y}>", LISTnull,     "y"),
        new ("<x:{it | <i>.<it>}; null={y}>", LISTnull_b,   "y1.b"),
        new ("<x:{it | <i>.<it>}; null={y}>", LISTa_null,   "1.ay"),
        new ("<x:{it | <i>.<it>}; null={y}>", LISTa_null_b, "1.ay2.b"),

        new ("<x:{it | x<if(!it)>y<endif>}; null={z}>", LIST0,        ""),
        new ("<x:{it | x<if(!it)>y<endif>}; null={z}>", LISTa,        "x"),
        new ("<x:{it | x<if(!it)>y<endif>}; null={z}>", LISTab,       "xx"),
        new ("<x:{it | x<if(!it)>y<endif>}; null={z}>", LISTnull,     "z"),
        new ("<x:{it | x<if(!it)>y<endif>}; null={z}>", LISTnull_b,   "zx"),
        new ("<x:{it | x<if(!it)>y<endif>}; null={z}>", LISTa_null,   "xz"),
        new ("<x:{it | x<if(!it)>y<endif>}; null={z}>", LISTa_null_b, "xzx"),

        new ("<x:t():u(); null={y}>", LIST0,        ""),
        new ("<x:t():u(); null={y}>", LISTa,        "a"),
        new ("<x:t():u(); null={y}>", LISTab,       "ab"),
        new ("<x:t():u(); null={y}>", LISTnull,     "y"),
        new ("<x:t():u(); null={y}>", LISTnull_b,   "yb"),
        new ("<x:t():u(); null={y}>", LISTa_null,   "ay"),
        new ("<x:t():u(); null={y}>", LISTa_null_b, "ayb")
    ];

    private static readonly T[] listTests = [
        new ("<[]>", UNDEF, ""),
        new ("<[]; null={x}>", UNDEF, ""),
        new ("<[]:{it | x}>", UNDEF, ""),
        new ("<[[],[]]:{it| x}>", UNDEF, ""),
        new ("<[]:t()>", UNDEF, "")
    ];

    [TestMethod]
    public void TestSingleValued() {
        var failed = TestMatrix(singleValuedTests);
        IList<T> expecting = new List<T>();
        CollectionAssert.AreEqual(expecting.ToList(), failed.ToList());
    }

    [TestMethod]
    public void TestMultiValued() {
        var failed = TestMatrix(multiValuedTests);
        IList<T> expecting = new List<T>();
        CollectionAssert.AreEqual(expecting.ToList(), failed.ToList());
    }

    [TestMethod]
    public void TestLists() {
        var failed = TestMatrix(listTests);
        IList<T> expecting = new List<T>();
        CollectionAssert.AreEqual(expecting.ToList(), failed.ToList());
    }

    private IList<T> TestMatrix(T[] tests) {
        IList<T> failed = new List<T>();
        foreach (var t in tests) {
            var test = new T(t); // dup since we might mod with result
            var group = _templateFactory.CreateTemplateGroup().Build();
            //System.out.println("running "+test);
            group.DefineTemplate("t", "<x>", ["x"]);
            group.DefineTemplate("u", "<x>", ["x"]);
            group.DefineTemplate("test", test.template, ["x"]);
            var st = group.GetInstanceOf("test");
            if (test.x != UNDEF) {
                st.Add("x", test.x);
            }
            var result = st.Render();
            if (!result.Equals(test.expecting)) {
                test.result = result;
                failed.Add(test);
            }
        }
        return failed;
    }

    [TestMethod]
    public void TestSeparatorWithNullFirstValue() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "hi <name; separator=\", \">!", ["name"]);
        var st = group.GetInstanceOf("test");
        st.Add("name", null); // null is added to list, but ignored in iteration
        st.Add("name", "Tom");
        st.Add("name", "Sumana");
        var expected = "hi Tom, Sumana!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestTemplateAppliedToNullIsEmpty() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<name:t()>", ["name"]);
        group.DefineTemplate("t", "<x>", ["x"]);
        var st = group.GetInstanceOf("test");
        st.Add("name", null); // null is added to list, but ignored in iteration
        var expected = "";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestTemplateAppliedToMissingValueIsEmpty() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<name:t()>", ["name"]);
        group.DefineTemplate("t", "<x>", ["x"]);
        var st = group.GetInstanceOf("test");
        var result = st.Render();
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void TestSeparatorWithNull2ndValue() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "hi <name; separator=\", \">!", ["name"]);
        var st = group.GetInstanceOf("test");
        st.Add("name", "Ter");
        st.Add("name", null);
        st.Add("name", "Sumana");
        var expected = "hi Ter, Sumana!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSeparatorWithNullLastValue() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "hi <name; separator=\", \">!", ["name"]);
        var st = group.GetInstanceOf("test");
        st.Add("name", "Ter");
        st.Add("name", "Tom");
        st.Add("name", null);
        var expected = "hi Ter, Tom!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSeparatorWithTwoNullValuesInRow() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "hi <name; separator=\", \">!", ["name"]);
        var st = group.GetInstanceOf("test");
        st.Add("name", "Ter");
        st.Add("name", "Tom");
        st.Add("name", null);
        st.Add("name", null);
        st.Add("name", "Sri");
        var expected = "hi Ter, Tom, Sri!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestTwoNullValues() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "hi <name; null=\"x\">!", ["name"]);
        var st = group.GetInstanceOf("test");
        st.Add("name", null);
        st.Add("name", null);
        var expected = "hi xx!";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestNullListItemNotCountedForIteratorIndex() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<name:{n | <i>:<n>}>", ["name"]);
        var st = group.GetInstanceOf("test");
        st.Add("name", "Ter");
        st.Add("name", null);
        st.Add("name", null);
        st.Add("name", "Jesse");
        var expected = "1:Ter2:Jesse";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestSizeZeroButNonNullListGetsNoOutput() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test",
            "begin\n" +
            "<users>\n" +
            "end\n", ["users"]);
        var t = group.GetInstanceOf("test");
        t.Add("users", null);
        var expecting = "begin" + newline + "end";
        var result = t.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestNullListGetsNoOutput() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test",
            "begin\n" +
            "<users:{u | name: <u>}; separator=\", \">\n" +
            "end\n", ["users"]);
        var t = group.GetInstanceOf("test");
        var expecting = "begin" + newline + "end";
        var result = t.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestEmptyListGetsNoOutput() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test",
            "begin\n" +
            "<users:{u | name: <u>}; separator=\", \">\n" +
            "end\n", ["users"]);
        var t = group.GetInstanceOf("test");
        t.Add("users", new List<string>());
        var expecting = "begin" + newline + "end";
        var result = t.Render();
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestMissingDictionaryValue() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<m.foo>", ["m"]);
        var t = group.GetInstanceOf("test");
        t.Add("m", new Dictionary<string, string>());
        var result = t.Render();
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void TestMissingDictionaryValue2() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<if(m.foo)>[<m.foo>]<endif>", ["m"]);
        var t = group.GetInstanceOf("test");
        t.Add("m", new Dictionary<string, string>());
        var result = t.Render();
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void TestMissingDictionaryValue3() {
        var group = _templateFactory.CreateTemplateGroup().Build();
        group.DefineTemplate("test", "<if(m.foo)>[<m.foo>]<endif>", ["m"]);
        var t = group.GetInstanceOf("test");
        t.Add("m", new Dictionary<string, string>() { { "foo", null } });
        var result = t.Render();
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void TestSeparatorEmittedForEmptyIteratorValue() {
        var st = new Template(
            "<values:{v|<if(v)>x<endif>}; separator=\" \">"
        );
        st.Add("values", new[] { true, false, true });
        var sw = new StringWriter();
        st.Write(new AutoIndentWriter(sw));
        var result = sw.ToString();
        const string expecting = "x  x";
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestSeparatorEmittedForEmptyIteratorValu3333e() {
        var dir = TmpDir;
        const string groupFile =
            "filter ::= [\"b\":, default: key]\n" +
            "t() ::= <%<[\"a\", \"b\", \"c\", \"b\"]:{it | <filter.(it)>}; separator=\",\">%>\n";
        WriteFile(dir, "group.stg", groupFile);
        var group = _templateFactory.CreateTemplateGroupFile(dir + "/group.stg").Build();

        var st = group.GetInstanceOf("t");
        var sw = new StringWriter();
        st.Write(new AutoIndentWriter(sw));
        var result = sw.ToString();
        const string expecting = "a,,c,";
        Assert.AreEqual(expecting, result);
    }

    [TestMethod]
    public void TestSeparatorEmittedForEmptyIteratorValue2() {
        var st = new Template(
            "<values; separator=\" \">"
        );
        st.Add("values", new[] { "x", string.Empty, "y" });
        var sw = new StringWriter();
        st.Write(new AutoIndentWriter(sw));
        var result = sw.ToString();
        const string expecting = "x  y";
        Assert.AreEqual(expecting, result);
    }

}
