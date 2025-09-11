namespace SharedKernel.Models;

public class AppDataOptions
{
    public string SystemMail { get; set; } = string.Empty;
    public string DocsPath { get; set; } = string.Empty;
    public string AutoAssignDocsPath { get; set; } = string.Empty;
    public string FsDocsPath { get; set; } = string.Empty;
    public string FsAutoAssignDocsPath { get; set; } = string.Empty;
    public string AzureBlob { get; set; } = string.Empty;
    public string AzureBlobLiberty { get; set; } = string.Empty;
    public int AutoCompleteIn { get; set; } = 10;
}
