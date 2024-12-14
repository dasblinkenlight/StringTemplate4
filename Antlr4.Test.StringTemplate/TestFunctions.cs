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

using System;

namespace Antlr4.Test.StringTemplate;

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Path = System.IO.Path;

[TestClass]
public class TestFunctions : BaseTest {

    [TestMethod]
    public void TestFirst() {
        var template = "<first(names)>";
        var st = _templateFactory.CreateTemplate(template);
        List<string> names = ["Ter", "Tom"];
        st.Add("names", names);
        var expected = "Ter";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestLength() {
        var template = "<length(names)>";
        var st = _templateFactory.CreateTemplate(template);
        List<string> names = ["Ter", "Tom"];
        st.Add("names", names);
        var expected = "2";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestLengthWithNullValues() {
        var template = "<length(names)>";
        var st = _templateFactory.CreateTemplate(template);
        List<string> names = ["Ter", null, "Tom", null];
        st.Add("names", names);
        var expected = "4";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestFirstOp() {
        var e = _templateFactory.CreateTemplate(
            "<first(names)>"
        );
        e.Add("names", "Ter");
        e.Add("names", "Tom");
        e.Add("names", "Sriram");
        var expected = "Ter";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestFirstOpList() {
        var e = _templateFactory.CreateTemplate(
            "<first(names)>"
        );
        e.Add("names", new List<string>(["Ter", "Tom", "Sriram"]));
        var expected = "Ter";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestFirstOpArray() {
        var e = _templateFactory.CreateTemplate(
            "<first(names)>"
        );
        e.Add("names", new[] { "Ter", "Tom", "Sriram" });
        var expected = "Ter";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestFirstOpPrimitiveArray() {
        var e = _templateFactory.CreateTemplate(
            "<first(names)>"
        );
        e.Add("names", new[] { 0, 1, 2 });
        var expected = "0";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestTruncOp() {
        var e = _templateFactory.CreateTemplate(
            "<trunc(names); separator=\", \">"
        );
        e.Add("names", "Ter");
        e.Add("names", "Tom");
        e.Add("names", "Sriram");
        var expected = "Ter, Tom";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestTruncOpList() {
        var e = _templateFactory.CreateTemplate(
            "<trunc(names); separator=\", \">"
        );
        e.Add("names", new List<string>(["Ter", "Tom", "Sriram"]));
        var expected = "Ter, Tom";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestTruncOpArray() {
        var e = _templateFactory.CreateTemplate(
            "<trunc(names); separator=\", \">"
        );
        e.Add("names", new[] { "Ter", "Tom", "Sriram" });
        const string expected = "Ter, Tom";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestTruncOpPrimitiveArray() {
        var e = _templateFactory.CreateTemplate(
            "<trunc(names); separator=\", \">"
        );
        e.Add("names", new[] { 0, 1, 2 });
        var expected = "0, 1";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestRestOp() {
        var e = _templateFactory.CreateTemplate(
            "<rest(names); separator=\", \">"
        );
        e.Add("names", "Ter");
        e.Add("names", "Tom");
        e.Add("names", "Sriram");
        var expected = "Tom, Sriram";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestRestOpList() {
        var e = _templateFactory.CreateTemplate(
            "<rest(names); separator=\", \">"
        );
        e.Add("names", new List<string>(["Ter", "Tom", "Sriram"]));
        const string expected = "Tom, Sriram";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestRestOpArray() {
        var e = _templateFactory.CreateTemplate(
            "<rest(names); separator=\", \">"
        );
        e.Add("names", new[] { "Ter", "Tom", "Sriram" });
        const string expected = "Tom, Sriram";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestRestOpPrimitiveArray() {
        var e = _templateFactory.CreateTemplate(
            "<rest(names); separator=\", \">"
        );
        e.Add("names", new[] { 0, 1, 2 });
        const string expected = "1, 2";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestRestOpEmptyList() {
        var e = _templateFactory.CreateTemplate(
            "<rest(names); separator=\", \">"
        );
        e.Add("names", new List<string>());
        Assert.AreEqual(string.Empty, e.Render());
    }

    [TestMethod]
    public void TestRestOpEmptyArray() {
        var e = _templateFactory.CreateTemplate(
            "<rest(names); separator=\", \">"
        );
        e.Add("names", Array.Empty<string>());
        Assert.AreEqual(string.Empty, e.Render());
    }

    [TestMethod]
    public void TestRestOpEmptyPrimitiveArray() {
        var e = _templateFactory.CreateTemplate(
            "<rest(names); separator=\", \">"
        );
        e.Add("names", Array.Empty<int>());
        Assert.AreEqual(string.Empty, e.Render());
    }

    [TestMethod]
    public void TestReUseOfRestResult() {
        var templates =
                "a(names) ::= \"<b(rest(names))>\"" + newline +
                "b(x) ::= \"<x>, <x>\"" + newline
            ;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var e = group.FindTemplate("a");
        List<string> names = [
            "Ter",
            "Tom"
        ];
        e.Add("names", names);
        const string expected = "Tom, Tom";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestReUseOfRestPrimitiveArrayResult() {
        var templates =
                "a(names) ::= \"<b(rest(names))>\"" + newline +
                "b(x) ::= \"<x>, <x>\"" + newline
            ;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(TmpDir + "/" + "t.stg").Build();
        var e = group.FindTemplate("a");
        e.Add("names", new[] { 0, 1 });
        var expected = "1, 1";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestLastOp() {
        var e = _templateFactory.CreateTemplate(
            "<last(names)>"
        );
        e.Add("names", "Ter");
        e.Add("names", "Tom");
        e.Add("names", "Sriram");
        const string expected = "Sriram";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestLastOpList() {
        var e = _templateFactory.CreateTemplate(
            "<last(names)>"
        );
        e.Add("names", new List<string>(["Ter", "Tom", "Sriram"]));
        const string expected = "Sriram";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestLastOpArray() {
        var e = _templateFactory.CreateTemplate(
            "<last(names)>"
        );
        e.Add("names", new[] { "Ter", "Tom", "Sriram" });
        const string expected = "Sriram";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestLastOpPrimitiveArray() {
        var e = _templateFactory.CreateTemplate(
            "<last(names)>"
        );
        e.Add("names", new[] { 0, 1, 2 });
        const string expected = "2";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestStripOp() {
        var e = _templateFactory.CreateTemplate(
            "<strip(names); null=\"n/a\">"
        );
        e.Add("names", null);
        e.Add("names", "Tom");
        e.Add("names", null);
        e.Add("names", null);
        e.Add("names", "Sriram");
        e.Add("names", null);
        const string expected = "TomSriram";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestStripOpList() {
        var e = _templateFactory.CreateTemplate(
            "<strip(names); null=\"n/a\">"
        );
        e.Add("names", new List<string>([null, "Tom", null, null, "Sriram", null]));
        const string expected = "TomSriram";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestStripOpArray() {
        var e = _templateFactory.CreateTemplate(
            "<strip(names); null=\"n/a\">"
        );
        e.Add("names", new[] { null, "Tom", null, null, "Sriram", null });
        const string expected = "TomSriram";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestLengthStrip() {
        var e = _templateFactory.CreateTemplate(
            "<length(strip(names))>"
        );
        e.Add("names", null);
        e.Add("names", "Tom");
        e.Add("names", null);
        e.Add("names", null);
        e.Add("names", "Sriram");
        e.Add("names", null);
        const string expected = "2";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestLengthStripList() {
        var e = _templateFactory.CreateTemplate(
            "<length(strip(names))>"
        );
        e.Add("names", new List<string>([null, "Tom", null, null, "Sriram", null]));
        const string expected = "2";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestLengthStripArray() {
        var e = _templateFactory.CreateTemplate(
            "<length(strip(names))>"
        );
        e.Add("names", new[] { null, "Tom", null, null, "Sriram", null });
        const string expected = "2";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestCombinedOp() {
        // replace first of yours with first of mine
        var e = _templateFactory.CreateTemplate(
            "<[first(mine),rest(yours)]; separator=\", \">"
        );
        e.Add("mine", "1");
        e.Add("mine", "2");
        e.Add("mine", "3");
        e.Add("yours", "a");
        e.Add("yours", "b");
        const string expected = "1, b";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestCombinedOpList() {
        // replace first of yours with first of mine
        var e = _templateFactory.CreateTemplate(
            "<[first(mine),rest(yours)]; separator=\", \">"
        );
        e.Add("mine", new List<string>(["1", "2", "3"]));
        e.Add("yours", "a");
        e.Add("yours", "b");
        const string expected = "1, b";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestCombinedOpArray() {
        // replace first of yours with first of mine
        var e = _templateFactory.CreateTemplate(
            "<[first(mine),rest(yours)]; separator=\", \">"
        );
        e.Add("mine", new[] { "1", "2", "3" });
        e.Add("yours", "a");
        e.Add("yours", "b");
        const string expected = "1, b";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestCombinedOpPrimitiveArray() {
        // replace first of yours with first of mine
        var e = _templateFactory.CreateTemplate(
            "<[first(mine),rest(yours)]; separator=\", \">"
        );
        e.Add("mine", new[] { 1, 2, 3 });
        e.Add("yours", "a");
        e.Add("yours", "b");
        const string expected = "1, b";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestCatListAndSingleAttribute() {
        // replace first of yours with first of mine
        var e = _templateFactory.CreateTemplate(
            "<[mine,yours]; separator=\", \">"
        );
        e.Add("mine", "1");
        e.Add("mine", "2");
        e.Add("mine", "3");
        e.Add("yours", "a");
        const string expected = "1, 2, 3, a";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestCatListAndSingleAttribute2() {
        // replace first of yours with first of mine
        var e = _templateFactory.CreateTemplate(
            "<[mine,yours]; separator=\", \">"
        );
        e.Add("mine", new List<string>(["1", "2", "3"]));
        e.Add("yours", "a");
        const string expected = "1, 2, 3, a";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestCatArrayAndSingleAttribute() {
        // replace first of yours with first of mine
        var e = _templateFactory.CreateTemplate(
            "<[mine,yours]; separator=\", \">"
        );
        e.Add("mine", new[] { "1", "2", "3" });
        e.Add("yours", "a");
        const string expected = "1, 2, 3, a";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestCatPrimitiveArrayAndSingleAttribute() {
        // replace first of yours with first of mine
        var e = _templateFactory.CreateTemplate(
            "<[mine,yours]; separator=\", \">"
        );
        e.Add("mine", new[] { 1, 2, 3 });
        e.Add("yours", "a");
        const string expected = "1, 2, 3, a";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestReUseOfCat() {
        var templates =
                "a(mine,yours) ::= \"<b([mine,yours])>\"" + newline +
                "b(x) ::= \"<x>, <x>\"" + newline
            ;
        WriteFile(TmpDir, "t.stg", templates);
        var group = _templateFactory.CreateTemplateGroupFile(Path.Combine(TmpDir, "t.stg")).Build();
        var e = group.FindTemplate("a");
        List<string> mine = [
            "Ter",
            "Tom"
        ];
        e.Add("mine", mine);
        List<string> yours = ["Foo"];
        e.Add("yours", yours);
        const string expected = "TerTomFoo, TerTomFoo";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestCatListAndEmptyAttributes() {
        // + is overloaded to be cat strings and cat lists so the
        // two operands (from left to right) determine which way it
        // goes.  In this case, x+mine is a list so everything from their
        // to the right becomes list cat.
        var e = _templateFactory.CreateTemplate(
            "<[x,mine,y,yours,z]; separator=\", \">"
        );
        e.Add("mine", "1");
        e.Add("mine", "2");
        e.Add("mine", "3");
        e.Add("yours", "a");
        const string expected = "1, 2, 3, a";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestCatListAndEmptyAttributes2() {
        // + is overloaded to be cat strings and cat lists so the
        // two operands (from left to right) determine which way it
        // goes.  In this case, x+mine is a list so everything from their
        // to the right becomes list cat.
        var e = _templateFactory.CreateTemplate(
            "<[x,mine,y,yours,z]; separator=\", \">"
        );
        e.Add("mine", new List<string>(["1", "2", "3"]));
        e.Add("yours", "a");
        const string expected = "1, 2, 3, a";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestCatArrayAndEmptyAttributes2() {
        // + is overloaded to be cat strings and cat lists so the
        // two operands (from left to right) determine which way it
        // goes.  In this case, x+mine is a list so everything from their
        // to the right becomes list cat.
        var e = _templateFactory.CreateTemplate(
            "<[x,mine,y,yours,z]; separator=\", \">"
        );
        e.Add("mine", new[] { "1", "2", "3" });
        e.Add("yours", "a");
        const string expected = "1, 2, 3, a";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestCatPrimitiveArrayAndEmptyAttributes() {
        // + is overloaded to be cat strings and cat lists so the
        // two operands (from left to right) determine which way it
        // goes.  In this case, x+mine is a list so everything from their
        // to the right becomes list cat.
        var e = _templateFactory.CreateTemplate(
            "<[x,mine,y,yours,z]; separator=\", \">"
        );
        e.Add("mine", new[] { 1, 2, 3 });
        e.Add("yours", "a");
        const string expected = "1, 2, 3, a";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestNestedOp() {
        var e = _templateFactory.CreateTemplate(
            "<first(rest(names))>" // gets 2nd element
        );
        e.Add("names", "Ter");
        e.Add("names", "Tom");
        e.Add("names", "Sriram");
        const string expected = "Tom";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestNestedOpList() {
        var e = _templateFactory.CreateTemplate(
            "<first(rest(names))>" // gets 2nd element
        );
        e.Add("names", new List<string>(["Ter", "Tom", "Sriram"]));
        const string expected = "Tom";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestNestedOpArray() {
        var e = _templateFactory.CreateTemplate(
            "<first(rest(names))>" // gets 2nd element
        );
        e.Add("names", new[] { "Ter", "Tom", "Sriram" });
        const string expected = "Tom";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestNestedOpPrimitiveArray() {
        var e = _templateFactory.CreateTemplate(
            "<first(rest(names))>" // gets 2nd element
        );
        e.Add("names", new[] { 0, 1, 2 });
        const string expected = "1";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestFirstWithOneAttributeOp() {
        var e = _templateFactory.CreateTemplate(
            "<first(names)>"
        );
        e.Add("names", "Ter");
        var expected = "Ter";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestLastWithOneAttributeOp() {
        var e = _templateFactory.CreateTemplate(
            "<last(names)>"
        );
        e.Add("names", "Ter");
        const string expected = "Ter";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestLastWithLengthOneListAttributeOp() {
        var e = _templateFactory.CreateTemplate(
            "<last(names)>"
        );
        e.Add("names", new List<string>() { "Ter" });
        const string expected = "Ter";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestLastWithLengthOneArrayAttributeOp() {
        var e = _templateFactory.CreateTemplate(
            "<last(names)>"
        );
        e.Add("names", new[] { "Ter" });
        const string expected = "Ter";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestLastWithLengthOnePrimitiveArrayAttributeOp() {
        var e = _templateFactory.CreateTemplate(
            "<last(names)>"
        );
        e.Add("names", new[] { 0 });
        const string expected = "0";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestRestWithOneAttributeOp() {
        var e = _templateFactory.CreateTemplate(
            "<rest(names)>"
        );
        e.Add("names", "Ter");
        Assert.AreEqual(string.Empty, e.Render());
    }

    [TestMethod]
    public void TestRestWithLengthOneListAttributeOp() {
        var e = _templateFactory.CreateTemplate(
            "<rest(names)>"
        );
        e.Add("names", new List<string> { "Ter" });
        Assert.AreEqual(string.Empty, e.Render());
    }

    [TestMethod]
    public void TestRestWithLengthOneArrayAttributeOp() {
        var e = _templateFactory.CreateTemplate(
            "<rest(names)>"
        );
        e.Add("names", new[] { "Ter" });
        Assert.AreEqual(string.Empty, e.Render());
    }

    [TestMethod]
    public void TestRestWithLengthOnePrimitiveArrayAttributeOp() {
        var e = _templateFactory.CreateTemplate(
            "<rest(names)>"
        );
        e.Add("names", new[] { 0 });
        Assert.AreEqual(string.Empty, e.Render());
    }

    [TestMethod]
    public void TestRepeatedRestOp() {
        var e = _templateFactory.CreateTemplate(
            "<rest(names)>, <rest(names)>" // gets 2nd element
        );
        e.Add("names", "Ter");
        e.Add("names", "Tom");
        const string expected = "Tom, Tom";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestRepeatedRestOpList() {
        var e = _templateFactory.CreateTemplate(
            "<rest(names)>, <rest(names)>" // gets 2nd element
        );
        e.Add("names", new List<string>(["Ter", "Tom"]));
        const string expected = "Tom, Tom";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestRepeatedRestOpArray() {
        var e = _templateFactory.CreateTemplate(
            "<rest(names)>, <rest(names)>" // gets 2nd element
        );
        e.Add("names", new[] { "Ter", "Tom" });
        const string expected = "Tom, Tom";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestRepeatedRestOpPrimitiveArray() {
        var e = _templateFactory.CreateTemplate(
            "<rest(names)>, <rest(names)>" // gets 2nd element
        );
        e.Add("names", new[] { 0, 1 });
        const string expected = "1, 1";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestIncomingLists() {
        var e = _templateFactory.CreateTemplate(
            "<rest(names)>, <rest(names)>" // gets 2nd element
        );
        e.Add("names", "Ter");
        e.Add("names", "Tom");
        const string expected = "Tom, Tom";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestFirstWithCatAttribute() {
        var e = _templateFactory.CreateTemplate(
            "<first([names,phones])>"
        );
        e.Add("names", "Ter");
        e.Add("names", "Tom");
        e.Add("phones", "1");
        e.Add("phones", "2");
        const string expected = "Ter";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestFirstWithListOfMaps() {
        var e = _templateFactory.CreateTemplate(
            "<first(maps).Ter>"
        );
        IDictionary<string, string> m1 = new Dictionary<string, string>();
        IDictionary<string, string> m2 = new Dictionary<string, string>();
        m1["Ter"] = "x5707";
        e.Add("maps", m1);
        m2["Tom"] = "x5332";
        e.Add("maps", m2);
        const string expected1 = "x5707";
        Assert.AreEqual(expected1, e.Render());

        List<IDictionary<string, string>> list = [m1, m2];
        e.Add("maps", list);
        const string expected2 = "x5707";
        Assert.AreEqual(expected2, e.Render());
    }

    [TestMethod]
    public void TestFirstWithListOfMaps2() {
        var e = _templateFactory.CreateTemplate(
            "<first(maps):{ m | <m>!}>"
        );
        IDictionary<string,string> m1 = new Dictionary<string,string>();
        IDictionary<string,string> m2 = new Dictionary<string,string>();
        m1["Ter"] = "x5707";
        e.Add("maps", m1);
        m2["Tom"] = "x5332";
        e.Add("maps", m2);
        const string expected = "Ter!";
        Assert.AreEqual(expected, e.Render());
        List<IDictionary<string,string>> list = [m1, m2];
        e.Add("maps", list);
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestTrim() {
        var e = _templateFactory.CreateTemplate(
            "<trim(name)>"
        );
        e.Add("name", " Ter  \n");
        const string expected = "Ter";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestStrlen() {
        var e = _templateFactory.CreateTemplate(
            "<strlen(name)>"
        );
        e.Add("name", "012345");
        const string expected = "6";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestReverse() {
        var e = _templateFactory.CreateTemplate(
            "<reverse(names); separator=\", \">"
        );
        e.Add("names", "Ter");
        e.Add("names", "Tom");
        e.Add("names", "Sriram");
        const string expected = "Sriram, Tom, Ter";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestReverseList() {
        var e = _templateFactory.CreateTemplate(
            "<reverse(names); separator=\", \">"
        );
        e.Add("names", new List<string>(["Ter", "Tom", "Sriram"]));
        const string expected = "Sriram, Tom, Ter";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestReverseArray() {
        var e = _templateFactory.CreateTemplate(
            "<reverse(names); separator=\", \">"
        );
        e.Add("names", new[] { "Ter", "Tom", "Sriram" });
        const string expected = "Sriram, Tom, Ter";
        Assert.AreEqual(expected, e.Render());
    }

    [TestMethod]
    public void TestReversePrimitiveArray() {
        var e = _templateFactory.CreateTemplate(
            "<reverse(names); separator=\", \">"
        );
        e.Add("names", new[] { 0, 1, 2 });
        const string expected = "2, 1, 0";
        Assert.AreEqual(expected, e.Render());
    }
}
