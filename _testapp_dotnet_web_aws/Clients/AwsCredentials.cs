using Amazon.Runtime;
using Microsoft.Extensions.Configuration;

namespace _testapp_dotnet_web_aws.Clients
{
    public class AwsCredentials : AWSCredentials
    {
        private IConfiguration _configuration;

        public AwsCredentials(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public override ImmutableCredentials GetCredentials()
        {
            return new ImmutableCredentials(_configuration["AWS_CLIENT_ID"], _configuration["AWS_SECRET"], null);
        }
    }
}