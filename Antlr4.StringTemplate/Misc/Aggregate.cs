﻿/*
 * [The "BSD license"]
 *  Copyright (c) 2011 Terence Parr
 *  All rights reserved.
 *
 * Conversion to C#:
 * Copyright (c) 2011 Sam Harwell, Tunnel Vision Laboratories, LLC
 * All rights reserved.
 *
 *  Redistribution and use in source and binary forms, with or without
 *  modification, are permitted provided that the following conditions
 *  are met:
 *  1. Redistributions of source code must retain the above copyright
 *     notice, this list of conditions and the following disclaimer.
 *  2. Redistributions in binary form must reproduce the above copyright
 *     notice, this list of conditions and the following disclaimer in the
 *     documentation and/or other materials provided with the distribution.
 *  3. The name of the author may not be used to endorse or promote products
 *     derived from this software without specific prior written permission.
 *
 *  THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
 *  IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 *  OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 *  IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
 *  INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 *  NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 *  DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 *  THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 *  (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 *  THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

namespace Antlr4.StringTemplate.Misc;

using System.Collections.Generic;

/** An automatically created aggregate of properties.
 *
 *  I often have lists of things that need to be formatted, but the list
 *  items are actually pieces of data that are not already in an object.  I
 *  need ST to do something like:
 *
 *  Ter=3432
 *  Tom=32234
 *  ....
 *
 *  using template:
 *
 *  $items:{it.name$=$it.type$}$
 *
 *  This example will call getName() on the objects in items attribute, but
 *  what if they aren't objects?  I have perhaps two parallel arrays
 *  instead of a single array of objects containing two fields.  One
 *  solution is allowing Maps to be handled like properties so that it.name
 *  would fail getName() but then see that it's a Map and do
 *  it.get("name") instead.
 *
 *  This very clean approach is espoused by some, but the problem is that
 *  it's a hole in my separation rules. People can put the logic in the
 *  view because you could say: "go get bob's data" in the view:
 *
 *  Bob's Phone: $db.bob.phone$
 *
 *  A view should not be part of the program and hence should never be able
 *  to go ask for a specific person's data.
 *
 *  After much thought, I finally decided on a simple solution.  I've
 *  added setAttribute variants that pass in multiple property values,
 *  with the property names specified as part of the name using a special
 *  attribute name syntax: "name.{propName1,propName2,...}".  This
 *  object is a special kind of HashMap that hopefully prevents people
 *  from passing a subclass or other variant that they have created as
 *  it would be a loophole.  Anyway, the ASTExpr.getObjectProperty()
 *  method looks for Aggregate as a special case and does a get() instead
 *  of getPropertyName.
 */
public class Aggregate {

    private readonly Dictionary<string, object> _properties = new();

    public IDictionary<string, object> Properties => _properties;

    /** Allow StringTemplate to add values, but prevent the end
     *  user from doing so.
     */
    public object this[string propertyName] {
        get => _properties[propertyName];

        internal set => _properties[propertyName] = value;
    }

}
