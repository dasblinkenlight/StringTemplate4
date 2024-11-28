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

namespace Antlr4.StringTemplate.Compiler;

using Antlr.Runtime;

/** Represents the name of a formal argument defined in a template:
 *
 *  test(a,b,x=defaultvalue) ::= "&lt;a&gt; &lt;n&gt; &lt;x&gt;"
 *
 *  Each template has a set of these formal arguments or uses
 *  a placeholder object: UNKNOWN (indicating that no arguments
 *  were specified such as when we create a template with "new Template(...)").
 *
 *  Note: originally, I tracked cardinality as well as the name of an
 *  attribute.  I'm leaving the code here as I suspect something may come
 *  of it later.  Currently, though, cardinality is not used.
 */
public class FormalArgument {
    /*
        // the following represent bit positions emulating a cardinality bitset.
        public static final int OPTIONAL = 1;     // a?
        public static final int REQUIRED = 2;     // a
        public static final int ZERO_OR_MORE = 4; // a*
        public static final int ONE_OR_MORE = 8;  // a+
        public static final String[] suffixes = {
            null,
            "?",
            "",
            null,
            "*",
            null,
            null,
            null,
            "+"
        };
        protected int cardinality = REQUIRED;
         */

    public FormalArgument(string name) {
        Name = name;
    }

    public FormalArgument(string name, IToken defaultValueToken) {
        Name = name;
        DefaultValueToken = defaultValueToken;
    }

    public string Name { get; }

    public int Index { get; internal set; }

    /** If they specified default value x=y, store the token here */
    public IToken DefaultValueToken { get; }

    public object DefaultValue { get; set; }

    public CompiledTemplate CompiledDefaultValue { get; internal set; }

    public override bool Equals(object o) {
        if (o == null || !(o is FormalArgument other)) {
            return false;
        }

        if (!Name.Equals(other.Name)) {
            return false;
        }

        // only check if there is a default value; that's all
        return !((DefaultValueToken != null && other.DefaultValueToken == null) ||
                 (DefaultValueToken == null && other.DefaultValueToken != null));
    }

    public override int GetHashCode() {
        return Name.GetHashCode() + DefaultValueToken.GetHashCode();
    }

    public override string ToString() {
        if (DefaultValueToken != null) {
            return Name + "=" + DefaultValueToken.Text;
        }
        return Name;
    }

}
