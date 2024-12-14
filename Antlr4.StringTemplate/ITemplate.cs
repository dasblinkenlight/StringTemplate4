using System.Globalization;

namespace Antlr4.StringTemplate;

public interface ITemplate {
    ITemplate Add(string name, object value);
    void AddMany(string aggrSpec, params object[] values);
    void Remove(string name);
    string Render(int lineWidth = AutoIndentWriter.NoWrap, CultureInfo cultureInfo = null);
    int Write(ITemplateWriter writer);
}
