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
using Antlr4.StringTemplate.Compiler;
using Antlr4.StringTemplate.Misc;
using Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestCompiler : BaseTest {

    [TestMethod]
    public void TestAttr() {
        const string template = "hi <name>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "write_str 0, " +
            "load_attr 1, " +
            "write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[hi , name]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestInclude() {
        const string template = "hi <foo()>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected = "write_str 0, new 1 0, write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[hi , foo]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestIncludeWithPassThrough() {
        const string template = "hi <foo(...)>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "write_str 0, args, passthru 1, new_box_args 1, write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[hi , foo]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestIncludeWithPartialPassThrough() {
        const string template = "hi <foo(x=y,...)>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "write_str 0, args, load_attr 1, store_arg 2, passthru 3, new_box_args 3, write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[hi , y, x, foo]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestSuperInclude() {
        const string template = "<super.foo()>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected = "super_new 0 0, write";
        code.Dump();
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[foo]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestSuperIncludeWithArgs() {
        const string template = "<super.foo(a,{b})>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "load_attr 0, new 1 0, super_new 2 2, write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[a, _sub1, foo]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestSuperIncludeWithNamedArgs() {
        const string template = "<super.foo(x=a,y={b})>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "args, load_attr 0, store_arg 1, new 2 0, store_arg 3, super_new_box_args 4, write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[a, x, _sub1, y, foo]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestIncludeWithArgs() {
        const string template = "hi <foo(a,b)>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "write_str 0, load_attr 1, load_attr 2, new 3 2, write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[hi , a, b, foo]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestAnonIncludeArgs() {
        const string template = "<({ a, b | <a><b>})>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "new 0 0, tostr, write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        var stringsExpected = "[_sub1]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestAnonIncludeArgMismatch() {
        ITemplateErrorListener errors = new ErrorBuffer();
        const string template = "<a:{foo}>";
        new TemplateCompiler(new TemplateGroup() {
            ErrorManager = new ErrorManager(errors)
        }).Compile(template);
        var expected = $"1:3: anonymous template has 0 arg(s) but mapped across 1 value(s){newline}";
        Assert.AreEqual(expected, errors.ToString());
    }

    [TestMethod]
    public void TestAnonIncludeArgMismatch2() {
        ITemplateErrorListener errors = new ErrorBuffer();
        const string template = "<a,b:{x|foo}>";
        new TemplateCompiler(new TemplateGroup {
            ErrorManager = new ErrorManager(errors)
        }).Compile(template);
        var expected = $"1:5: anonymous template has 1 arg(s) but mapped across 2 value(s){newline}";
        Assert.AreEqual(expected, errors.ToString());
    }

    [TestMethod]
    public void TestAnonIncludeArgMismatch3() {
        ITemplateErrorListener errors = new ErrorBuffer();
        var template = "<a:{x|foo},{bar}>";
        new TemplateCompiler(new TemplateGroup {
            ErrorManager = new ErrorManager(errors)
        }).Compile(template);
        var expected = $"1:11: anonymous template has 0 arg(s) but mapped across 1 value(s){newline}";
        Assert.AreEqual(expected, errors.ToString());
    }

    [TestMethod]
    public void TestIndirectIncludeWitArgs() {
        const string template = "hi <(foo)(a,b)>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "write_str 0, load_attr 1, tostr, load_attr 2, load_attr 3, new_ind 2, write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[hi , foo, a, b]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestProp() {
        const string template = "hi <a.b>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "write_str 0, load_attr 1, load_prop 2, write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[hi , a, b]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestProp2() {
        const string template = "<u.id>: <u.name>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "load_attr 0, load_prop 1, write, write_str 2, " +
            "load_attr 0, load_prop 3, write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[u, id, : , name]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestMap() {
        const string template = "<name:bold()>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected = "load_attr 0, null, new 1 1, map, write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[name, bold]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestMapAsOption() {
        const string template = "<a; wrap=name:bold()>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "load_attr 0, options, load_attr 1, null, new 2 1, map, " +
            "store_option 4, write_opt";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[a, name, bold]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestMapArg() {
        const string template = "<name:bold(x)>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected = "load_attr 0, null, load_attr 1, new 2 2, map, write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[name, x, bold]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestIndirectMapArg() {
        const string template = "<name:(t)(x)>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "load_attr 0, load_attr 1, tostr, null, load_attr 2, new_ind 2, map, write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[name, t, x]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestRepeatedMap() {
        const string template = "<name:bold():italics()>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "load_attr 0, null, new 1 1, map, null, new 2 1, map, write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[name, bold, italics]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestRepeatedMapArg() {
        const string template = "<name:bold(x):italics(x,y)>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "load_attr 0, null, load_attr 1, new 2 2, map, " +
            "null, load_attr 1, load_attr 3, new 4 3, map, write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[name, x, bold, y, italics]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestRotMap() {
        const string template = "<name:bold(),italics()>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "load_attr 0, null, new 1 1, null, new 2 1, rot_map 2, write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[name, bold, italics]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestRotMapArg() {
        const string template = "<name:bold(x),italics()>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "load_attr 0, null, load_attr 1, new 2 2, null, new 3 1, rot_map 2, write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[name, x, bold, italics]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestZipMap() {
        const string template = "<names,phones:bold()>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "load_attr 0, load_attr 1, null, null, new 2 2, zip_map 2, write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[names, phones, bold]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestZipMapArg() {
        const string template = "<names,phones:bold(x)>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "load_attr 0, load_attr 1, null, null, load_attr 2, new 3 3, zip_map 2, write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[names, phones, x, bold]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestAnonMap() {
        const string template = "<name:{n | <n>}>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "load_attr 0, null, new 1 1, map, write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        var stringsExpected = "[name, _sub1]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestAnonZipMap() {
        const string template = "<a,b:{x,y | <x><y>}>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "load_attr 0, load_attr 1, null, null, new 2 2, zip_map 2, write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[a, b, _sub1]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestIf() {
        const string template = "go: <if(name)>hi, foo<endif>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "write_str 0, load_attr 1, brf 12, write_str 2";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[go: , name, hi, foo]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestIfElse() {
        const string template = "go: <if(name)>hi, foo<else>bye<endif>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "write_str 0, " +
            "load_attr 1, " +
            "brf 15, " +
            "write_str 2, " +
            "br 18, " +
            "write_str 3";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[go: , name, hi, foo, bye]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestElseIf() {
        const string template = "go: <if(name)>hi, foo<elseif(user)>a user<endif>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "write_str 0, " +
            "load_attr 1, " +
            "brf 15, " +
            "write_str 2, " +
            "br 24, " +
            "load_attr 3, " +
            "brf 24, " +
            "write_str 4";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[go: , name, hi, foo, user, a user]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestElseIfElse() {
        const string template = "go: <if(name)>hi, foo<elseif(user)>a user<else>bye<endif>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "write_str 0, " +
            "load_attr 1, " +
            "brf 15, " +
            "write_str 2, " +
            "br 30, " +
            "load_attr 3, " +
            "brf 27, " +
            "write_str 4, " +
            "br 30, " +
            "write_str 5";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[go: , name, hi, foo, user, a user, bye]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestOption() {
        const string template = "hi <name; separator=\"x\">";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "write_str 0, load_attr 1, options, load_str 2, store_option 3, write_opt";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[hi , name, x]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestOptionAsTemplate() {
        const string template = "hi <name; separator={, }>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "write_str 0, load_attr 1, options, new 2 0, store_option 3, write_opt";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[hi , name, _sub1]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestOptions() {
        var template = "hi <name; anchor, wrap=foo(), separator=\", \">";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected =
            "write_str 0, " +
            "load_attr 1, " +
            "options, " +
            "load_str 2, " +
            "store_option 0, " +
            "new 3 0, " +
            "store_option 4, " +
            "load_str 4, " +
            "store_option 3, " +
            "write_opt";
        const string stringsExpected = // the ", , ," is the ", " separator string
            "[hi , name, true, foo, , ]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
    }

    [TestMethod]
    public void TestEmptyList() {
        const string template = "<[]>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected = "list, write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestList() {
        const string template = "<[a,b]>";
        var code = new TemplateCompiler(new TemplateGroup()).Compile(template);
        const string asmExpected = "list, load_attr 0, add, load_attr 1, add, write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[a, b]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestEmbeddedRegion() {
        const string template = "<@r>foo<@end>";
        // compile as if in root dir and in template 'a'
        var code = new TemplateCompiler(new TemplateGroup('<', '>')).Compile("a", template);
        const string asmExpected = "new 0 0, write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[/region__/a__r]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

    [TestMethod]
    public void TestRegion() {
        const string template = "x:<@r()>";
        // compile as if in root dir and in template 'a'
        var code = new TemplateCompiler(new TemplateGroup('<', '>')).Compile("a", template);
        var asmExpected =
            "write_str 0, new 1 0, write";
        var asmResult = code.GetInstructions();
        Assert.AreEqual(asmExpected, asmResult);
        const string stringsExpected = "[x:, /region__/a__r]";
        var stringsResult = code.strings.ToListString();
        Assert.AreEqual(stringsExpected, stringsResult);
    }

}
