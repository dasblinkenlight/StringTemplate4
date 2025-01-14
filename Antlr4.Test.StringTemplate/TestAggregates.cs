﻿/*
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

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestAggregates : BaseTest {

    [TestMethod]
    public void TestApplyAnonymousTemplateToAggregateAttribute() {
        var st = _templateFactory.CreateTemplate("<items:{it|<it.id>: <it.lastName>, <it.firstName>\n}>");
        // also testing wacky spaces in aggregate spec
        st.AddMany("items.{ firstName ,lastName, id }", "Ter", "Parr", 99);
        st.AddMany("items.{firstName, lastName ,id}", "Tom", "Burns", 34);
        var expecting =
            "99: Parr, Ter" + newline +
            "34: Burns, Tom" + newline;
        Assert.AreEqual(expecting, st.Render());
    }

    private class Decl {
        public Decl(string name, string type) {
            Name = name;
            Type = type;
        }
        public string Name { get; }
        public string Type { get; }
    }

    [TestMethod]
    public void TestComplicatedIndirectTemplateApplication() {
        var templates =
                "group Java;" + newline +
                "" + newline +
                "file(variables) ::= <<" +
                "<variables:{ v | <v.decl:(v.format)()>}; separator=\"\\n\">" + newline +
                ">>" + newline +
                "intdecl(decl) ::= \"int <decl.name> = 0;\"" + newline +
                "intarray(decl) ::= \"int[] <decl.name> = null;\"" + newline
            ;
        var group = _templateFactory.CreateTemplateGroupString(templates).Build();
        var f = group.FindTemplate("file");
        f.AddMany("variables.{ decl,format }", new Decl("i", "int"), "intdecl");
        f.AddMany("variables.{decl ,  format}", new Decl("a", "int-array"), "intarray");
        //System.out.println("f='"+f+"'");
        var expecting = "int i = 0;" + newline +
                        "int[] a = null;";
        Assert.AreEqual(expecting, f.Render());
    }

}
