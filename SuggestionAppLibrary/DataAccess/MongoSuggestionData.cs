using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuggestionAppLibrary.DataAccess
{
    public class MongoSuggestionData : ISuggestionData
    {
        private readonly IDbConnection _db;
        private readonly IUserData _userData;
        private readonly IMemoryCache _cache;
        private readonly IMongoCollection<SuggestionModel> _suggestions;
        private const string CacheName = "SuggestionData";

        public MongoSuggestionData(IDbConnection db, IUserData userData, IMemoryCache cache)
        {
            _db = db;
            _userData = userData;
            _cache = cache;
            _suggestions = db.SuggestionCollection;
        }

        public async Task<List<SuggestionModel>> GetAllSuggestions()
        {
            var output = _cache.Get<List<SuggestionModel>>(CacheName);
            if (output is null)
            {
                var results = await _suggestions.FindAsync(s => s.Archived == false);
                output = results.ToList();

                _cache.Set(CacheName, output, TimeSpan.FromMinutes(1));
            }
            return output;
        }

        public async Task<List<SuggestionModel>> GetAllApprovedSuggestions()
        {
            var output = await GetAllSuggestions();

            return output.Where(x => x.ApprovedForRelease).ToList();
        }

        public async Task<SuggestionModel> GetSuggestion(string id)
        {
            var results = await _suggestions.FindAsync(s => s.Id == id);
            return results.FirstOrDefault();
        }

        public async Task<List<SuggestionModel>> GetAllSuggestionWaitingForApproval()
        {
            var output = await GetAllSuggestions();

            return output.Where(x => x.ApprovedForRelease == false && x.Rejected == false).ToList();
        }

        public async Task UpdateSuggestion(SuggestionModel suggestion)
        {
            await _suggestions.ReplaceOneAsync(s => s.Id == suggestion.Id, suggestion);
            _cache.Remove(CacheName);
        }

        public async Task UpvoteSuggestion(string suggestionId, string userId)
        {
            var client = _db.Client;

            using var session = await client.StartSessionAsync();

            session.StartTransaction();
            try
            {
                var db = client.GetDatabase(_db.DbName);
                var suggestionInTransaction = db.GetCollection<SuggestionModel>(_db.SuggestionCollectionName);
                var suggestion = (await suggestionInTransaction.FindAsync(s => s.Id == suggestionId)).First();

                bool isUpvote = suggestion.UserVotes.Add(userId);
                if (isUpvote == false)
                {
                    suggestion.UserVotes.Remove(userId);
                }

                await suggestionInTransaction.ReplaceOneAsync(s => s.Id == suggestionId, suggestion);

                var userInTransaction = db.GetCollection<UserModel>(_db.UserCollectionName);

                var user = await _userData.GetUser(suggestion.Author.Id);

                if (isUpvote)
                {

                    user.VotedOnSuggestions.Add(new BasicSuggestionModel(suggestion));
                }
                else
                {
                    var suggestionToRemove = user.VotedOnSuggestions.Where(s => s.Id == suggestionId).First();
                    user.AuthoredSuggestions.Remove(suggestionToRemove);
                }

                await userInTransaction.ReplaceOneAsync(u => u.Id == userId, user);

                await session.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                //usually you are to log the error
                await session.AbortTransactionAsync();
                throw;
            }
        }

        public async Task CreateSuggestion(SuggestionModel suggestion)
        {
            var client = _db.Client;

            using var session = await client.StartSessionAsync();

            session.StartTransaction();
            try
            {
                var db = client.GetDatabase(_db.DbName);
                var suggestionInTransaction = db.GetCollection<SuggestionModel>(_db.SuggestionCollectionName);
                await suggestionInTransaction.InsertOneAsync(suggestion);

                var userInTransaction = db.GetCollection<UserModel>(_db.UserCollectionName);
                var user = await _userData.GetUser(suggestion.Author.Id);
                user.AuthoredSuggestions.Add(new BasicSuggestionModel(suggestion));

                await userInTransaction.ReplaceOneAsync(u => u.Id == user.Id, user);

                await session.CommitTransactionAsync();
            }
            catch (Exception)
            {
                await session.AbortTransactionAsync();
                throw;
            }
        }
    }
}
