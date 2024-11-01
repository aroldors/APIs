using Microsoft.AspNetCore.Mvc;

namespace DecriptPfx.EndpointValidation
{
    public class ApiKeyAttribute : ServiceFilterAttribute
    {
        public ApiKeyAttribute()
            : base(typeof(ApiKeyAuthFilter))
        {}
    }    
}