# Antlr4 StringTemplates for C# Library

This is a fork of the C# String Template repo that fixes a few bugs and adds a missing feature that didn't make it to the original C# rewrite.
The code has been converted to the modern version of C#, and unit tests were added back.

The old repo is [here](https://github.com/kaby76/Domemtech.StringTemplate4); the original repo is [here](https://github.com/antlr/antlrcs)

The repo keeps only StringTemplate4, its tests, and the Antlr3 runtime. Everything else has been removed.

This library is a "netstandard2.0" library, which should be compatible with up-to-date frameworks.

NuGet: [Dasblinkenlight.StringTemplate4](https://www.nuget.org/packages/Dasblinkenlight.StringTemplate4/).

## Documentation

Refer to the [StringTemplate Home](http://www.stringtemplate.org/) for
instructions on how to use. Note, the API classes and methods differ from
the doc. E.g., [ST in Java](https://github.com/antlr/stringtemplate4/blob/master/src/org/stringtemplate/v4/ST.java)
is [Template in C#](https://github.com/kaby76/stringtemplate4cs/blob/main/Antlr4.StringTemplate/Template.cs).
