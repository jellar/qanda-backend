using Microsoft.Extensions.Configuration;
using QandA.Data.Models;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QandA.Data
{
    using static Dapper.SqlMapper;
    public class DataRepository : IDataRepository
    {
        private readonly string _connectionString;
        public DataRepository(IConfiguration configuration)
        {
            _connectionString = configuration["ConnectionStrings:DefaultConnection"];
        }        

        public async Task<IEnumerable<QuestionGetManyResponse>> GetQuestions()
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.QueryAsync<QuestionGetManyResponse>(@"exec dbo.Question_GetMany");
        }

        public async Task<IEnumerable<QuestionGetManyResponse>> GetQuestionsWithAnswers()
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var questionDictionary = new Dictionary<int, QuestionGetManyResponse>();

            return (await connection.QueryAsync<QuestionGetManyResponse, AnswerGetResponse, QuestionGetManyResponse>(
                @"exec dbo.Question_GetMany_WithAnswers", map: (q, a) =>
                {
                    QuestionGetManyResponse question;
                    if (!questionDictionary.TryGetValue(q.QuestionId, out question))
                    {
                        question = q;
                        question.Answers = new List<AnswerGetResponse>();
                        questionDictionary.Add(question.QuestionId, question);
                    }
                    question.Answers.Add(a);
                    return question;
                }, splitOn: "QuestionId")).Distinct().ToList();
        }

        public async Task<IEnumerable<QuestionGetManyResponse>> GetQuestionsBySearch(string search)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.QueryAsync<QuestionGetManyResponse>(
                @"exec dbo.Question_GetMany_BySearch @Search = @Search", new { Search = search });
        }

        public async Task<IEnumerable<QuestionGetManyResponse>> GetQuestionsBySearchWithPaging(string search, int pageNumber,
            int pageSize)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var parameters = new {Search = search, PageNumber = pageNumber, PageSize = pageSize};
            return await connection.QueryAsync<QuestionGetManyResponse>(
                @"exec dbo.Question_GetMany_BySearch_WithPaging @Search = @Search, @PageNumber= @PageNumber, @PageSize = @PageSize",
                parameters);
            
        }

        public async Task<IEnumerable<QuestionGetManyResponse>> GetUnansweredQuestions()
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.QueryAsync<QuestionGetManyResponse>(@"dbo.Question_GetUnanswered");
        }

        public async Task<IEnumerable<QuestionGetManyResponse>> GetUnansweredQuestionsAsync()
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.QueryAsync<QuestionGetManyResponse>(@"dbo.Question_GetUnanswered");
        }

        public async Task<QuestionGetSingleResponse> GetQuestion(int questionId)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var results = await connection.QueryMultipleAsync(
                @"exec dbo.Question_GetSingle @QuestionId = @QuestionId;
                    exec dbo.Answer_Get_ByQuestionId @QuestionId = @QuestionId", new { QuestionId = questionId });
            var question = results.Read<QuestionGetSingleResponse>().FirstOrDefault();
            if(question != null)
            {
                question.Answers = results.Read<AnswerGetResponse>().ToList();
            }
            return question;
            /*
                var question = connection.QuerySingleOrDefault<QuestionGetSingleResponse>(
                    @"exec dbo.Question_GetSingle @QuestionId = @QuestionId", new { QuestionId = questionId });
                if (question != null)
                {
                    question.Answers = connection.Query<AnswerGetResponse>(
                        @"exec dbo.Answer_Get_ByQuestionId @QuestionId = @QuestionId", new { QuestionId = questionId });
                }
                return question; 
                */
        }

        public async Task<bool> QuestionExists(int questionId)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.QueryFirstAsync<bool>(
                @"exec dbo.Question_Exists @QuestionId = @QuestionId", new { QuestionId = questionId });
        }

        public async Task<AnswerGetResponse> GetAnswer(int answerId)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.QueryFirstOrDefaultAsync<AnswerGetResponse>(
                @"exec dbo.Answer_Get_ByAnswerId @AnswerId = @AnswerId", new { AnswerId = answerId });
        }

        public async Task<QuestionGetSingleResponse> PostQuestion(QuestionPostFullRequest question)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var questionId = await connection.QueryFirstAsync<int>(
                @"exec dbo.Question_Post @Title = @Title, 
                    @Content = @Content, @UserId = @UserId, @UserName = @UserName, @Created = @Created", question);

            return await GetQuestion(questionId);
        }

        public async Task<QuestionGetSingleResponse> PutQuestion(int questionId, QuestionPutRequest question)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await connection.ExecuteAsync(
                @"EXEC dbo.Question_Put @QuestionId = @QuestionId, @Title = @Title, @Content = @Content",
                new { QuestionId = questionId, question.Title, question.Content }
            );
            return await GetQuestion(questionId);
        }

        public async Task DeleteQuestion(int questionId)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await connection.ExecuteAsync(
                @"EXEC dbo.Question_Delete @QuestionId = @QuestionId",
                new { QuestionId = questionId }
            );
        }

        public async Task<AnswerGetResponse> PostAnswer(AnswerPostFullRequest answer)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.QueryFirstAsync<AnswerGetResponse>(
                @"exec dbo.Answer_Post @QuestionId = @QuestionId, @Content = @Content, 
                    @UserId = @UserId, @UserName = @UserName,
                    @Created = @Created", answer);
        }       
    }
}
