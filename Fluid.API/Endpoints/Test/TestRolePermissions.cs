namespace Fluid.API.Endpoints.Test;

//[Route("api/test")]
//[ApiController]
//public class TestKeyingPermission : EndpointBaseAsync.WithoutRequest.WithActionResult<TestPermissionsResponse>
//{
//    public TestKeyingPermission()
//    {
//    }

//    [HttpGet("keying")]
//    [AuthorizePermission(ApplicationPermissions.ViewOrders)]
//    [SwaggerOperation(
//        Summary = "Test Keying role permissions",
//        Description = "Test endpoint that requires ViewOrders permission. Keying, TenantAdmin, and ProductOwner roles should have access.",
//        OperationId = "TestKeyingPermission",
//        Tags = new[] { "Test" })]
//    [SwaggerResponse(200, "Success - User has ViewOrders permission", typeof(TestPermissionsResponse))]
//    [SwaggerResponse(401, "Unauthorized")]
//    [SwaggerResponse(403, "Forbidden - Missing ViewOrders permission")]
//    public override async Task<ActionResult<TestPermissionsResponse>> HandleAsync(CancellationToken cancellationToken = default)
//    {
//        var response = new TestPermissionsResponse
//        {
//            Message = "Success! You have ViewOrders permission (Keying level access).",
//            Permission = ApplicationPermissions.ViewOrders,
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

//[Route("api/test")]
//[ApiController]  
//public class TestQCPermission : EndpointBaseAsync.WithoutRequest.WithActionResult<TestPermissionsResponse>
//{
//    public TestQCPermission()
//    {
//    }

//    [HttpGet("qc")]
//    [AuthorizePermission(ApplicationPermissions.UpdateOrders)]
//    [SwaggerOperation(
//        Summary = "Test QC role permissions",
//        Description = "Test endpoint that requires UpdateOrders permission. QC, Keying, TenantAdmin, and ProductOwner roles should have access.",
//        OperationId = "TestQCPermission",
//        Tags = new[] { "Test" })]
//    [SwaggerResponse(200, "Success - User has UpdateOrders permission", typeof(TestPermissionsResponse))]
//    [SwaggerResponse(401, "Unauthorized")]
//    [SwaggerResponse(403, "Forbidden - Missing UpdateOrders permission")]
//    public override async Task<ActionResult<TestPermissionsResponse>> HandleAsync(CancellationToken cancellationToken = default)
//    {
//        var response = new TestPermissionsResponse
//        {
//            Message = "Success! You have UpdateOrders permission (QC level access).",
//            Permission = ApplicationPermissions.UpdateOrders,
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