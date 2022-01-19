using Amazon;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;

namespace _testapp_dotnet_web_aws.Clients
{
    public static class SqsClientFactory
    {

		public static AmazonSQSClient CreateClient(IConfiguration configuration)
		{
			var sqsConfig = new AmazonSQSConfig
			{
				RegionEndpoint = RegionEndpoint.GetBySystemName(configuration["AWS_REGION"])
			};

			var awsCredentials = new AwsCredentials(configuration);
			return new AmazonSQSClient(awsCredentials, sqsConfig);
		}
	}
}
