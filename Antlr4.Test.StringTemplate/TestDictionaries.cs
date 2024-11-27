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
using Antlr4.StringTemplate.Misc;
using Extensions;
using Path = System.IO.Path;

[TestClass]
public class TestDictionaries : BaseTest {

    [TestMethod]
    public void TestDict() {
        var templates =
                "typeInit ::= [\"int\":\"0\", \"float\":\"0.0\"] " + newline +
                "var(type,name) ::= \"<type> <name> = <typeInit.(type)>;\"" + newline;
        WriteFile(TmpDir, "test.stg", templates);
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "test.stg"));
        var st = group.GetInstanceOf("var");
        st.Add("type", "int");
        st.Add("name", "x");
        var expected = "int x = 0;";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestEmptyDictionary() {
        var templates =
            "d ::= []\n";
        WriteFile(TmpDir, "t.stg", templates);

        var errors = new ErrorBuffer();
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "t.stg")) {
            Listener = errors
        };
        group.Load(); // force load
        Assert.AreEqual(0, errors.Errors.Count);
    }

    [TestMethod]
    public void TestDictValuesAreTemplates() {
        var templates =
            "typeInit ::= [\"int\":{0<w>}, \"float\":{0.0<w>}] " + newline +
            "var(type,w,name) ::= \"<type> <name> = <typeInit.(type)>;\"" + newline;
        WriteFile(TmpDir, "test.stg", templates);
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "test.stg"));
        var st = group.GetInstanceOf("var");
        st.impl.Dump();
        st.Add("w", "L");
        st.Add("type", "int");
        st.Add("name", "x");
        var expected = "int x = 0L;";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDictKeyLookupViaTemplate() {
        // Make sure we try rendering stuff to string if not found as regular object
        var templates =
            "typeInit ::= [\"int\":{0<w>}, \"float\":{0.0<w>}] " + newline +
            "var(type,w,name) ::= \"<type> <name> = <typeInit.(type)>;\"" + newline;
        WriteFile(TmpDir, "test.stg", templates);
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "test.stg"));
        var st = group.GetInstanceOf("var");
        st.Add("w", "L");
        st.Add("type", new Template("int"));
        st.Add("name", "x");
        var expected = "int x = 0L;";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDictKeyLookupAsNonToStringableObject() {
        // Make sure we try rendering stuff to string if not found as regular object
        var templates =
                "foo(m,k) ::= \"<m.(k)>\"" + newline
            ;
        WriteFile(TmpDir, "test.stg", templates);
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "test.stg"));
        var st = group.GetInstanceOf("foo");
        IDictionary<HashableUser, string> m = new Dictionary<HashableUser, string>();
        m[new HashableUser(99, "parrt")] = "first";
        m[new HashableUser(172036, "tombu")] = "second";
        m[new HashableUser(391, "sriram")] = "third";
        st.Add("m", m);
        st.Add("k", new HashableUser(172036, "tombu"));
        var expected = "second";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDictMissingDefaultValueIsEmpty() {
        var templates =
                "typeInit ::= [\"int\":\"0\", \"float\":\"0.0\"] " + newline +
                "var(type,w,name) ::= \"<type> <name> = <typeInit.(type)>;\"" + newline
            ;
        WriteFile(TmpDir, "test.stg", templates);
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "test.stg"));
        var st = group.GetInstanceOf("var");
        st.Add("w", "L");
        st.Add("type", "double"); // double not in typeInit map
        st.Add("name", "x");
        var expected = "double x = ;";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDictMissingDefaultValueIsEmptyForNullKey() {
        var templates =
                "typeInit ::= [\"int\":\"0\", \"float\":\"0.0\"] " + newline +
                "var(type,w,name) ::= \"<type> <name> = <typeInit.(type)>;\"" + newline
            ;
        WriteFile(TmpDir, "test.stg", templates);
        TemplateGroup group = new TemplateGroupFile(Path.Combine(TmpDir, "test.stg"));
        var st = group.GetInstanceOf("var");
        st.Add("w", "L");
        st.Add("type", null); // double not in typeInit map
        st.Add("name", "x");
        var expected = " x = ;";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDictHiddenByFormalArg() {
        var templates =
                "typeInit ::= [\"int\":\"0\", \"float\":\"0.0\"] " + newline +
                "var(typeInit,type,name) ::= \"<type> <name> = <typeInit.(type)>;\"" + newline
            ;
        WriteFile(TmpDir, "test.stg", templates);
        TemplateGroup group = new TemplateGroupFile(Path.Combine(TmpDir, "test.stg"));
        var st = group.GetInstanceOf("var");
        st.Add("type", "int");
        st.Add("name", "x");
        var expected = "int x = ;";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDictEmptyValueAndAngleBracketStrings() {
        var templates =
                "typeInit ::= [\"int\":\"0\", \"float\":, \"double\":<<0.0L>>] " + newline +
                "var(type,name) ::= \"<type> <name> = <typeInit.(type)>;\"" + newline
            ;
        WriteFile(TmpDir, "test.stg", templates);
        TemplateGroup group = new TemplateGroupFile(Path.Combine(TmpDir, "test.stg"));
        var st = group.GetInstanceOf("var");
        st.Add("type", "float");
        st.Add("name", "x");
        const string expected = "float x = ;";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDictDefaultValue() {
        var templates =
                "typeInit ::= [\"int\":\"0\", default:\"null\"] " + newline +
                "var(type,name) ::= \"<type> <name> = <typeInit.(type)>;\"" + newline
            ;
        WriteFile(TmpDir, "test.stg", templates);
        TemplateGroup group = new TemplateGroupFile(Path.Combine(TmpDir, "test.stg"));
        var st = group.GetInstanceOf("var");
        st.Add("type", "UserRecord");
        st.Add("name", "x");
        const string expected = "UserRecord x = null;";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDictNullKeyGetsDefaultValue() {
        var templates =
                "typeInit ::= [\"int\":\"0\", default:\"null\"] " + newline +
                "var(type,name) ::= \"<type> <name> = <typeInit.(type)>;\"" + newline
            ;
        WriteFile(TmpDir, "test.stg", templates);
        TemplateGroup group = new TemplateGroupFile(Path.Combine(TmpDir, "test.stg"));
        var st = group.GetInstanceOf("var");
        // missing or set to null: st.Add("type", null);
        st.Add("name", "x");
        const string expected = " x = null;";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDictEmptyDefaultValue() {
        var templates =
                "typeInit ::= [\"int\":\"0\", default:] " + newline +
                "var(type,name) ::= \"<type> <name> = <typeInit.(type)>;\"" + newline
            ;
        WriteFile(TmpDir, "test.stg", templates);
        var errors = new ErrorBuffer();
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "test.stg")) {
            Listener = errors
        };
        group.Load();
        const string expected = "[test.stg 1:33: missing value for key at ']']";
        var result = errors.Errors.ToListString();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDictDefaultValueIsKey() {
        var templates =
                "typeInit ::= [\"int\":\"0\", default:key] " + newline +
                "var(type,name) ::= \"<type> <name> = <typeInit.(type)>;\"" + newline
            ;
        WriteFile(TmpDir, "test.stg", templates);
        TemplateGroup group = new TemplateGroupFile(Path.Combine(TmpDir, "test.stg"));
        var st = group.GetInstanceOf("var");
        st.Add("type", "UserRecord");
        st.Add("name", "x");
        const string expected = "UserRecord x = UserRecord;";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDictWithoutIteration() {
        var templates =
            "t2(adr,line2={<adr.zip> <adr.city>}) ::= <<" + newline +
            "<adr.firstname> <adr.lastname>" + newline +
            "<line2>" + newline +
            ">>";

        WriteFile(TmpDir, "test.stg", templates);
        TemplateGroup group = new TemplateGroupFile(Path.Combine(TmpDir, "test.stg"));
        var st = group.GetInstanceOf("t2");
        st.Add("adr", new Dictionary<string, string>()
        {
            {"firstname","Terence"},
            {"lastname","Parr"},
            {"zip","99999"},
            {"city","San Francisco"},
        });
        var expected =
            "Terence Parr" + newline +
            "99999 San Francisco";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDictWithoutIteration2() {
        var templates =
            "t2(adr,line2={<adr.zip> <adr.city>}) ::= <<" + newline +
            "<adr.firstname> <adr.lastname>" + newline +
            "<line2>" + newline +
            ">>";

        WriteFile(TmpDir, "test.stg", templates);
        TemplateGroup group = new TemplateGroupFile(Path.Combine(TmpDir, "test.stg"));
        var st = group.GetInstanceOf("t2");
        st.Add("adr", new Dictionary<string, string>()
        {
            {"firstname","Terence"},
            {"lastname","Parr"},
            {"zip","99999"},
            {"city","San Francisco"},
        });
        st.Add("line2", new Template("<adr.city>, <adr.zip>"));
        var expected =
            "Terence Parr" + newline +
            "San Francisco, 99999";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDictWithoutIteration3() {
        var templates =
            "t2(adr,line2={<adr.zip> <adr.city>}) ::= <<" + newline +
            "<adr.firstname> <adr.lastname>" + newline +
            "<line2>" + newline +
            ">>" + newline +
            "t3(adr) ::= <<" + newline +
            "<t2(adr=adr,line2={<adr.city>, <adr.zip>})>" + newline +
            ">>" + newline;

        WriteFile(TmpDir, "test.stg", templates);
        TemplateGroup group = new TemplateGroupFile(Path.Combine(TmpDir, "test.stg"));
        var st = group.GetInstanceOf("t3");
        st.Add("adr", new Dictionary<string, string>()
        {
            {"firstname","Terence"},
            {"lastname","Parr"},
            {"zip","99999"},
            {"city","San Francisco"},
        });
        var expected =
            "Terence Parr" + newline +
            "San Francisco, 99999";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    /**
     * Test that a map can have only the default entry.
     */
    [TestMethod]
    public void TestDictDefaultStringAsKey() {
        var templates =
                "typeInit ::= [\"default\":\"foo\"] " + newline +
                "var(type,name) ::= \"<type> <name> = <typeInit.(type)>;\"" + newline
            ;
        WriteFile(TmpDir, "test.stg", templates);
        TemplateGroup group = new TemplateGroupFile(Path.Combine(TmpDir, "test.stg"));
        var st = group.GetInstanceOf("var");
        st.Add("type", "default");
        st.Add("name", "x");
        const string expected = "default x = foo;";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    /**
     * Test that a map can return a <b>string</b> with the word: default.
     */
    [TestMethod]
    public void TestDictDefaultIsDefaultString() {
        var templates =
                "map ::= [default: \"default\"] " + newline +
                "t() ::= << <map.(\"1\")> >>" + newline
            ;
        WriteFile(TmpDir, "test.stg", templates);
        TemplateGroup group = new TemplateGroupFile(Path.Combine(TmpDir, "test.stg"));
        var st = group.GetInstanceOf("t");
        const string expected = " default ";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDictViaEnclosingTemplates() {
        var templates =
                "typeInit ::= [\"int\":\"0\", \"float\":\"0.0\"] " + newline +
                "intermediate(type,name) ::= \"<var(type,name)>\"" + newline +
                "var(type,name) ::= \"<type> <name> = <typeInit.(type)>;\"" + newline
            ;
        WriteFile(TmpDir, "test.stg", templates);
        TemplateGroup group = new TemplateGroupFile(Path.Combine(TmpDir, "test.stg"));
        var st = group.GetInstanceOf("intermediate");
        st.Add("type", "int");
        st.Add("name", "x");
        const string expected = "int x = 0;";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDictViaEnclosingTemplates2() {
        var templates =
                "typeInit ::= [\"int\":\"0\", \"float\":\"0.0\"] " + newline +
                "intermediate(stuff) ::= \"<stuff>\"" + newline +
                "var(type,name) ::= \"<type> <name> = <typeInit.(type)>;\"" + newline
            ;
        WriteFile(TmpDir, "test.stg", templates);
        TemplateGroup group = new TemplateGroupFile(Path.Combine(TmpDir, "test.stg"));
        var interm = group.GetInstanceOf("intermediate");
        var var = group.GetInstanceOf("var");
        var.Add("type", "int");
        var.Add("name", "x");
        interm.Add("stuff", var);
        const string expected = "int x = 0;";
        var result = interm.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestAccessDictionaryFromAnonymousTemplate() {
        var dir = TmpDir;
        const string g =
            "a() ::= <<[<[\"foo\",\"a\"]:{x|<if(values.(x))><x><endif>}>]>>\n" +
            "values ::= [\n" +
            "    \"a\":false,\n" +
            "    default:true\n" +
            "]\n";
        WriteFile(dir, "g.stg", g);

        TemplateGroup group = new TemplateGroupFile(Path.Combine(dir, "g.stg"));
        var st = group.GetInstanceOf("a");
        const string expected = "[foo]";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestAccessDictionaryFromAnonymousTemplateInRegion() {
        var dir = TmpDir;
        const string g =
            "a() ::= <<[<@r()>]>>\n" +
            "@a.r() ::= <<\n" +
            "<[\"foo\",\"a\"]:{x|<if(values.(x))><x><endif>}>\n" +
            ">>\n" +
            "values ::= [\n" +
            "    \"a\":false,\n" +
            "    default:true\n" +
            "]\n";
        WriteFile(dir, "g.stg", g);

        TemplateGroup group = new TemplateGroupFile(Path.Combine(dir, "g.stg"));
        var st = group.GetInstanceOf("a");
        const string expected = "[foo]";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestStringsInDictionary() {
        const string templates =
            "auxMap ::= [\n" +
            "   \"E\": \"electric <field>\",\n" +
            "   \"I\": \"in <field> between\",\n" +
            "   \"F\": \"<field> force\",\n" +
            "   default: \"<field>\"\n" +
            "]\n" +
            "\n" +
            "makeTmpl(type, field) ::= <<\n" +
            "<auxMap.(type)>\n" +
            ">>\n" +
            "\n" +
            "top() ::= <<\n" +
            "  <makeTmpl(\"E\", \"foo\")>\n" +
            "  <makeTmpl(\"F\", \"foo\")>\n" +
            "  <makeTmpl(\"I\", \"foo\")>\n" +
            ">>\n";
        WriteFile(TmpDir, "t.stg", templates);
        TemplateGroup group = new TemplateGroupFile(TmpDir + Path.DirectorySeparatorChar + "t.stg");
        var st = group.GetInstanceOf("top");
        Assert.IsNotNull(st);
        var expected =
            "  electric <field>" + newline +
            "  <field> force" + newline +
            "  in <field> between";
        Assert.AreEqual(expected, st.Render());
    }

    [TestMethod]
    public void TestTemplatesInDictionary() {
        const string templates =
            "auxMap ::= [\n" +
            "   \"E\": {electric <field>},\n" +
            "   \"I\": {in <field> between},\n" +
            "   \"F\": {<field> force},\n" +
            "   default: {<field>}\n" +
            "]\n" +
            "\n" +
            "makeTmpl(type, field) ::= <<\n" +
            "<auxMap.(type)>\n" +
            ">>\n" +
            "\n" +
            "top() ::= <<\n" +
            "  <makeTmpl(\"E\", \"foo\")>\n" +
            "  <makeTmpl(\"F\", \"foo\")>\n" +
            "  <makeTmpl(\"I\", \"foo\")>\n" +
            ">>\n";
        WriteFile(TmpDir, "t.stg", templates);
        TemplateGroup group = new TemplateGroupFile(TmpDir + Path.DirectorySeparatorChar + "t.stg");
        var st = group.GetInstanceOf("top");
        Assert.IsNotNull(st);
        var expected =
            "  electric foo" + newline +
            "  foo force" + newline +
            "  in foo between";
        Assert.AreEqual(expected, st.Render());
    }

    [TestMethod]
    public void TestDictionaryBehaviorTrue() {
        const string templates =
            "d ::= [\n" +
            "	\"x\" : true,\n" +
            "	default : false,\n" +
            "]\n" +
            "\n" +
            "t() ::= <<\n" +
            "<d.(\"x\")><if(d.(\"x\"))>+<else>-<endif>\n" +
            ">>\n";

        WriteFile(TmpDir, "t.stg", templates);
        TemplateGroup group = new TemplateGroupFile(TmpDir + Path.DirectorySeparatorChar + "t.stg");
        var st = group.GetInstanceOf("t");
        const string expected = "true+";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDictionaryBehaviorFalse() {
        const string templates =
            "d ::= [\n" +
            "	\"x\" : false,\n" +
            "	default : false,\n" +
            "]\n" +
            "\n" +
            "t() ::= <<\n" +
            "<d.(\"x\")><if(d.(\"x\"))>+<else>-<endif>\n" +
            ">>\n";

        WriteFile(TmpDir, "t.stg", templates);
        TemplateGroup group = new TemplateGroupFile(TmpDir + Path.DirectorySeparatorChar + "t.stg");
        var st = group.GetInstanceOf("t");
        const string expected = "false-";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDictionaryBehaviorEmptyTemplate() {
        const string templates =
            "d ::= [\n" +
            "	\"x\" : {},\n" +
            "	default : false,\n" +
            "]\n" +
            "\n" +
            "t() ::= <<\n" +
            "<d.(\"x\")><if(d.(\"x\"))>+<else>-<endif>\n" +
            ">>\n";

        WriteFile(TmpDir, "t.stg", templates);
        TemplateGroup group = new TemplateGroupFile(TmpDir + Path.DirectorySeparatorChar + "t.stg");
        var st = group.GetInstanceOf("t");
        const string expected = "+";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDictionaryBehaviorEmptyList() {
        const string templates =
            "d ::= [\n" +
            "	\"x\" : [],\n" +
            "	default : false,\n" +
            "]\n" +
            "\n" +
            "t() ::= <<\n" +
            "<d.(\"x\")><if(d.(\"x\"))>+<else>-<endif>\n" +
            ">>\n";

        WriteFile(TmpDir, "t.stg", templates);
        TemplateGroup group = new TemplateGroupFile(TmpDir + Path.DirectorySeparatorChar + "t.stg");
        var st = group.GetInstanceOf("t");
        const string expected = "-";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    /// <summary>
    /// This is a regression test for antlr/stringtemplate4#114. Before the fix the following test would return
    /// %hi%.
    /// </summary>
    /// <seealso href="https://github.com/antlr/stringtemplate4/issues/114">dictionary value using &lt;% %&gt; is broken</seealso>
    [TestMethod]
    public void TestDictionaryBehaviorNoNewlineTemplate() {
        const string templates =
            "d ::= [\n" +
            "	\"x\" : <%hi%>\n" +
            "]\n" +
            "\n" +
            "t() ::= <<\n" +
            "<d.x>\n" +
            ">>\n";

        WriteFile(TmpDir, "t.stg", templates);
        TemplateGroup group = new TemplateGroupFile(TmpDir + Path.DirectorySeparatorChar + "t.stg");
        var st = group.GetInstanceOf("t");
        const string expected = "hi";
        var result = st.Render();
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TestDictionarySpecialValues() {
        const string templates = @"
t(id) ::= <<
<identifier.(id)>
>>

identifier ::= [
    ""keyword"" : ""@keyword"",
    default : key
]
";

        WriteFile(TmpDir, "t.stg", templates);
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "t.stg"));

        // try with mapped values
        var template = group.GetInstanceOf("t").Add("id", "keyword");
        Assert.AreEqual("@keyword", template.Render());

        // try with non-mapped values
        template = group.GetInstanceOf("t").Add("id", "nonkeyword");
        Assert.AreEqual("nonkeyword", template.Render());

        // try with non-mapped values that might break (Substring here guarantees unique instances)
        template = group.GetInstanceOf("t").Add("id", "_default".Substring(1));
        Assert.AreEqual("default", template.Render());

        template = group.GetInstanceOf("t").Add("id", "_keys".Substring(1));
        Assert.AreEqual("keyworddefault", template.Render());

        template = group.GetInstanceOf("t").Add("id", "_values".Substring(1));
        Assert.AreEqual("@keywordkey", template.Render());
    }

    [TestMethod]
    public void TestDictionarySpecialValuesOverride() {
        const string templates = @"
t(id) ::= <<
<identifier.(id)>
>>

identifier ::= [
    ""keyword"" : ""@keyword"",
    ""keys"" : ""keys"",
    ""values"" : ""values"",
    default : key
]
";

        WriteFile(TmpDir, "t.stg", templates);
        var group = new TemplateGroupFile(Path.Combine(TmpDir, "t.stg"));

        // try with mapped values
        var template = group.GetInstanceOf("t").Add("id", "keyword");
        Assert.AreEqual("@keyword", template.Render());

        // try with non-mapped values
        template = group.GetInstanceOf("t").Add("id", "nonkeyword");
        Assert.AreEqual("nonkeyword", template.Render());

        // try with non-mapped values that might break (Substring here guarantees unique instances)
        template = group.GetInstanceOf("t").Add("id", "_default".Substring(1));
        Assert.AreEqual("default", template.Render());

        template = group.GetInstanceOf("t").Add("id", "_keys".Substring(1));
        Assert.AreEqual("keys", template.Render());

        template = group.GetInstanceOf("t").Add("id", "_values".Substring(1));
        Assert.AreEqual("values", template.Render());
    }

}
