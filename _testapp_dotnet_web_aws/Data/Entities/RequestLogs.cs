using System;

namespace _testapp_dotnet_web_aws.Data
{
    public partial class RequestLogs
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public string Origin { get; set; }
        public DateTime? CreatedOn { get; set; }
    }
}