

using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace SuggestionAppLibrary.DataAccess
{
    public class DbConnection : IDbConnection
    {
        private readonly IConfiguration _config;
        private readonly IMongoDatabase _database;
        private string _connectionId = "MongoDB";
        public string DbName { get; private set; }
        public string CategoryCollectionName { get; private set; } = "categories";
        public string StatusCollectionName { get; private set; } = "statuses";
        public string UserCollectionName { get; private set; } = "users";
        public string SuggestionCollectionName { get; private set; } = "suggestions";

        public MongoClient Client { get; private set; }
        public IMongoCollection<CategoryModel> CategoryCollection { get; private set; }

        public IMongoCollection<StatusModel> StatusCollection { get; private set; }

        public IMongoCollection<UserModel> UserCollection { get; private set; }

        public IMongoCollection<SuggestionModel> SuggestionCollection { get; private set; }

        public DbConnection(IConfiguration config)
        {
            _config = config;
            Client = new MongoClient(_config.GetConnectionString(_connectionId));
            DbName = _config["DatabaseName"];
            _database = Client.GetDatabase(DbName);

            CategoryCollection = _database.GetCollection<CategoryModel>(CategoryCollectionName);
            StatusCollection = _database.GetCollection<StatusModel>(StatusCollectionName);
            UserCollection = _database.GetCollection<UserModel>(UserCollectionName);
            SuggestionCollection = _database.GetCollection<SuggestionModel>(SuggestionCollectionName);

        }
    }
}
