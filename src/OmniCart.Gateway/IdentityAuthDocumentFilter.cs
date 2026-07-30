using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public sealed class IdentityAuthDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Paths.Add("/api/auth/register", CreateAuthPathItem(
            "Registers a customer account and returns a JWT."));

        swaggerDoc.Paths.Add("/api/auth/login", CreateAuthPathItem(
            "Authenticates a user and returns a JWT.", includeUnauthorizedResponse: true));
    }

    private static OpenApiPathItem CreateAuthPathItem(string summary, bool includeUnauthorizedResponse = false)
    {
        var responses = new OpenApiResponses
        {
            ["200"] = new OpenApiResponse
            {
                Description = "Authentication succeeded.",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Required = new HashSet<string> { "token" },
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                ["token"] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "JWT access token."
                                }
                            }
                        }
                    }
                }
            }
        };

        if (includeUnauthorizedResponse)
        {
            responses.Add("401", new OpenApiResponse
            {
                Description = "The supplied username or password is invalid."
            });
        }

        return new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Post] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> { new() { Name = "Identity" } },
                    Summary = summary,
                    RequestBody = new OpenApiRequestBody
                    {
                        Required = true,
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Type = "object",
                                    Required = new HashSet<string> { "username", "password" },
                                    Properties = new Dictionary<string, OpenApiSchema>
                                    {
                                        ["username"] = new OpenApiSchema { Type = "string" },
                                        ["password"] = new OpenApiSchema { Type = "string", Format = "password" }
                                    }
                                }
                            }
                        }
                    },
                    Responses = responses
                }
            }
        };
    }
}
