using _testapp_dotnet_web_aws.Data;
using _testapp_dotnet_web_aws.v1.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace _testapp_dotnet_web_aws.v1.Services
{
    public interface IMessageService
    {
        Task<List<RequestLogModel>> GetRequestLogs();

        bool AddMessage(string message);
    }

    public class MessageService : IMessageService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;

        public MessageService(AppDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<List<RequestLogModel>> GetRequestLogs()
        {
            var res = await _dbContext.RequestLogs.OrderBy(x => x.CreatedOn)
                .ToListAsync();

            return _mapper.Map<List<RequestLogModel>>(res);
        }

        public bool AddMessage(string message)
        {
            _dbContext.RequestLogs
                .Add(new RequestLogs
                {
                    Message = message,
                    Origin = "test",
                    CreatedOn = System.DateTime.UtcNow
                });

            _dbContext.SaveChanges();

            return true;
        }
    }
}