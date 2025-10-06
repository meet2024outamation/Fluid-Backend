namespace Fluid.API.Endpoints.Test;

//[Route("api/test")]
//[ApiController]
//public class TestPermissions : EndpointBaseAsync.WithoutRequest.WithActionResult<TestPermissionsResponse>
//{
//    public TestPermissions()
//    {
//    }

//    [HttpGet("permissions")]
//    [AuthorizePermission(ApplicationPermissions.SystemAdmin)]
//    [SwaggerOperation(
//        Summary = "Test system admin permissions",
//        Description = "Test endpoint that requires SystemAdmin permission. Only Product Owners should have access.",
//        OperationId = "TestPermissions",
//        Tags = new[] { "Test" })]
//    [SwaggerResponse(200, "Success - User has SystemAdmin permission", typeof(TestPermissionsResponse))]
//    [SwaggerResponse(401, "Unauthorized")]
//    [SwaggerResponse(403, "Forbidden - Missing SystemAdmin permission")]
//    public override async Task<ActionResult<TestPermissionsResponse>> HandleAsync(CancellationToken cancellationToken = default)
//    {
//        var response = new TestPermissionsResponse
//        {
//            Message = "Success! You have SystemAdmin permission.",
//            Permission = ApplicationPermissions.SystemAdmin,
//            Timestamp = DateTimeOffset.UtcNow,
//            UserInfo = new UserTestInfo
//            {
//                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
//                Claims = User.Claims.Select(c => new ClaimInfo 
//                { 
//                    Type = c.Type, 
//                    Value = c.Value 
//                }).ToList()
//            }
//        };

//        var result = Result<TestPermissionsResponse>.Success(response, "Permission test successful");
//        return result.ToActionResult();
//    }
//}

//public class TestPermissionsResponse
//{
//    public string Message { get; set; } = string.Empty;
//    public string Permission { get; set; } = string.Empty;
//    public DateTimeOffset Timestamp { get; set; }
//    public UserTestInfo UserInfo { get; set; } = new();
//}

//public class UserTestInfo
//{
//    public bool IsAuthenticated { get; set; }
//    public List<ClaimInfo> Claims { get; set; } = new();
//}

//public class ClaimInfo
//{
//    public string Type { get; set; } = string.Empty;
//    public string Value { get; set; } = string.Empty;
//}