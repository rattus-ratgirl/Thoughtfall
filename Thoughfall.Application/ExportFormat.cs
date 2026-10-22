namespace Thoughfall.Application;

/// <summary>The supported diary export formats.</summary>
public enum ExportFormat
{
    /// <summary>JSON grouped by year, month, and day.</summary>
    Json,

    /// <summary>Markdown date headings with quoted entry text.</summary>
    Markdown,
}
