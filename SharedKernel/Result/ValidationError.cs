namespace SharedKernel.Result;
public class ValidationError
{
  public string? Key { get; set; }
  public string? ErrorMessage { get; set; }
  public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
}
