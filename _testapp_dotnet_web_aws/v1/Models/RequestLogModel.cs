using System;

namespace _testapp_dotnet_web_aws.v1.Models
{
    public class RequestLogModel
    {
        public string Message { get; set; }
        public string Origin { get; set; }
        public DateTime? CreatedOn { get; set; }
    }
}
