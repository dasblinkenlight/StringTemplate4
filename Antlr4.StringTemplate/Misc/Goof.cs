using System.Dynamic;

namespace Antlr4.StringTemplate.Misc;

public class Goof : GetMemberBinder
{
    public Goof(string name, bool ignoreCase) : base(name, ignoreCase)
    {
    }

    public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
    {
        return null;
    }
}