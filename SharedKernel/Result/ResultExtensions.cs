using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace SharedKernel.Result.Extensions;
public static class ResultExtensions
{
  public static Result<T> ToSuccessResult<T>(this T value)
  {
    return Result<T>.Success(value);
  }

  public static Result<T> ToErrorResult<T>(this IEnumerable<string> errors)
  {
    return Result<T>.Error(errors.ToArray());
  }
  
  public static Result<T> ToInvalidResult<T>(this List<ValidationError> validationErrors)
  { 
    return Result<T>.Invalid(validationErrors);
  }

  /// <summary>
  /// Converts a Result to an ActionResult for ASP.NET Core controllers
  /// </summary>
  public static ActionResult<T> ToActionResult<T>(this Result<T> result)
  {
    var statusCode = GetHttpStatusCode(result.Status);
    
    if (result.IsSuccess)
    {
      return new ObjectResult(result.Value)
      {
        StatusCode = statusCode
      };
    }

    var responseObject = new
    {
      status = statusCode,
      success = result.IsSuccess,
      errors = result.Errors,
      validationErrors = result.ValidationErrors,
      message = result.Errors?.FirstOrDefault() ?? "An error occurred"
    };

    return new ObjectResult(responseObject)
    {
      StatusCode = statusCode
    };
  }

  private static int GetHttpStatusCode(ResultStatus status)
  {
    return status switch
    {
      ResultStatus.Ok => 200,
      ResultStatus.Created => 201,
      ResultStatus.NotFound => 404,
      ResultStatus.Unauthorized => 401,
      ResultStatus.Forbidden => 403,
      ResultStatus.Invalid => 400,
      ResultStatus.Error => 500,
      _ => 500
    };
  }
}
