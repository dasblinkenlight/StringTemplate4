using System;
using System.Text;

namespace Antlr4.StringTemplate;

public interface ITemplateFactory {
    TemplateGroupBuilder CreateTemplateGroup();
    TemplateGroupBuilder CreateTemplateGroupDirectory(string dirName);
    TemplateGroupBuilder CreateRawGroupDirectory(string dirName);
    TemplateGroupBuilder CreateTemplateGroupFile(string fileName);
    TemplateGroupBuilder CreateTemplateGroupString(string text, string sourceName = null);
}
