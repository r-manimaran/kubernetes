using System.Web.Http;
using Swashbuckle.Application;
using WebActivatorEx;

[assembly: PreApplicationStartMethod(typeof(AlgorithmApi.SwaggerConfig), "Register")]

namespace AlgorithmApi
{
    public class SwaggerConfig
    {
        public static void Register()
        {
            GlobalConfiguration.Configuration
                .EnableSwagger(c =>
                {
                    c.SingleApiVersion("v1", "Algorithm API");
                })
                .EnableSwaggerUi();
        }
    }
}
