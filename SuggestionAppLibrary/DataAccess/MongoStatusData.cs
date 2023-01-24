

using Microsoft.Extensions.Caching.Memory;

namespace SuggestionAppLibrary.DataAccess
{
    public class MongoStatusData
    {
        private readonly IMongoCollection<StatusModel> _statuses;
        private readonly IMemoryCache _cache;
        private const string cacheName = "StatusData";

        public MongoStatusData(IDbConnection db, IMemoryCache cache)
        {
            _cache = cache;
            _statuses = db.StatusCollection;
        }
    }
}
