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

using Antlr.Runtime;
using Antlr4.StringTemplate;
using Antlr4.StringTemplate.Compiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ArgumentException = System.ArgumentException;
using CultureInfo = System.Globalization.CultureInfo;
using DateTime = System.DateTime;
using Directory = System.IO.Directory;
using Environment = System.Environment;
using File = System.IO.File;
using Path = System.IO.Path;
using StringBuilder = System.Text.StringBuilder;
#if !NETSTANDARD
using Thread = System.Threading.Thread;
#endif

[TestClass]
public abstract class BaseTest {
    protected static string TmpDir { get; set; }
    protected static readonly string newline = Environment.NewLine;

    public TestContext TestContext { get; set; }

    protected ITemplateFactory _templateFactory = new TemplateFactory();

    [TestInitialize]
    public void SetUp() {
        // Ideally we wanted en-US, but invariant provides a suitable default for testing.
#if NETSTANDARD
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
#else
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
#endif
        TemplateGroup.DefaultGroup = (TemplateGroup)_templateFactory.CreateTemplateGroup().Build();
        TemplateCompiler.subtemplateCount = 0;

        // new output dir for each test
        TmpDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"st4-{CurrentTimeMillis()}"));
    }

    [TestCleanup]
    public void TearDown() {
        // Remove tmpdir if no error. how?
        if (TestContext is { CurrentTestOutcome: UnitTestOutcome.Passed }) {
            EraseTempDir();
        }
    }

    private void EraseTempDir() {
        if (Directory.Exists(TmpDir)) {
            Directory.Delete(TmpDir, true);
        }
    }

    private static long CurrentTimeMillis() {
        return DateTime.Now.ToFileTime() / 10000;
    }

    protected static void WriteFile(string dir, string fileName, string content) {
        if (fileName == null || Path.IsPathRooted(fileName)) {
            throw new ArgumentException("Expected a non-rooted path", nameof(fileName));
        }
        var fullPath = Path.GetFullPath(Path.Combine(dir, fileName));
        var absDir = Path.GetDirectoryName(fullPath);
        if (!Directory.Exists(absDir)) {
            Directory.CreateDirectory(absDir!);
        }
        File.WriteAllText(fullPath, content);
    }

    protected void CheckTokens(string template, string expected, char delimiterStartChar = '<', char delimiterStopChar = '>') {
        var lexer = new TemplateLexer(
            TemplateGroup.DefaultErrorManager,
            new ANTLRStringStream(template),
            null,
            delimiterStartChar,
            delimiterStopChar);
        var tokens = new CommonTokenStream(lexer);
        var buf = new StringBuilder();
        buf.Append('[');
        var i = 1;
        var t = tokens.LT(i);
        while (t.Type != CharStreamConstants.EndOfFile) {
            if (i > 1) {
                buf.Append(", ");
            }
            buf.Append(t);
            i++;
            t = tokens.LT(i);
        }
        buf.Append(']');
        var result = buf.ToString();
        Assert.AreEqual(expected, result);
    }

    public class User {
        public int id;
        public string name;
        public static string StaticField = "field_value";

        public User(int id, string name) {
            this.id = id;
            this.name = name;
        }

        public virtual bool IsManager => true;
        public virtual bool HasParkingSpot => true;
        public virtual string Name => name;
        public static string GetStaticMethod() => "method_result";
        public static string StaticProperty => "property_result";
    }

    public class HashableUser : User {
        public HashableUser(int id, string name) : base(id, name) {
        }
        public override int GetHashCode() => id;
        public override bool Equals(object o) =>
            (o is HashableUser hu) && (id == hu.id && string.Equals(name, hu.name));
    }

}
