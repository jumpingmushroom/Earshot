using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Earshot.Core.Model;
using Xunit;

public class DataFilesTests
{
    private static string Root([CallerFilePath] string here = "")
    {
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here), "..", ".."));
    }

    private static string Read(string relative)
    {
        return File.ReadAllText(Path.Combine(Root(), "src", "Earshot", relative));
    }

    [Fact]
    public void LabelTableParsesWithoutWarnings()
    {
        var warnings = new List<string>();
        LabelTable table = LabelTable.Parse(Read("data/labels.tsv"), warnings.Add);
        Assert.Empty(warnings);
        Assert.True(table.Count >= 20, "expected at least 20 rows, got " + table.Count);
    }

    [Fact]
    public void EveryEarshotWordHasEnglishText()
    {
        Translations en = Translations.Parse(Read("translations/English.txt"));
        LabelTable table = LabelTable.Parse(Read("data/labels.tsv"), null);
        foreach (LabelRow row in table.Rows)
        {
            if (row.Mute)
                continue;
            if (row.Source.StartsWith("$earshot_"))
                Assert.True(en.Get(row.Source.Substring(1)) != null, row.Pattern + ": no English for " + row.Source);
            else if (!row.Source.StartsWith("$") && !row.Source.StartsWith("@"))
                Assert.True(en.Get(row.Source) != null, row.Pattern + ": no English for " + row.Source);
            if (row.Action != null && row.Action != "@vanilla")
                Assert.True(en.Get(row.Action) != null, row.Pattern + ": no English for action " + row.Action);
        }
        foreach (string key in new[] { "format", "near", "raid", "settings_toggle" })
            Assert.True(en.Get(key) != null, "no English for " + key);
    }
}
