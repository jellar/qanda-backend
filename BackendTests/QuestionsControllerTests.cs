using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using QandA.Controllers;
using QandA.Data;
using QandA.Data.Models;
using Xunit;

namespace BackendTests
{
    public class QuestionsControllerTests
    {
        [Fact]
        public async void GetQuestions_WhenNoParameters_ReturnsAllQuestions()
        {
            var mockQuestions = new List<QuestionGetManyResponse>();
            for (int i = 1; i <= 10; i++)
            {    
                mockQuestions.Add(new QuestionGetManyResponse()
                {
                    QuestionId = 1,
                    Title = $"Test Title {i}",
                    Content = $"Test Content {i}",
                    UserName = "User1",
                    Answers = new List<AnswerGetResponse>()
                });
            }
            
            var mockDataRepository = new Mock<IDataRepository>();
            mockDataRepository.Setup(repo => repo.GetQuestions())
                .Returns(() => Task.FromResult(mockQuestions.AsEnumerable()));
            
            var mockConfigurationRoot = new Mock<IConfigurationRoot>();
            mockConfigurationRoot.Setup(config => config[It.IsAny<string>()]).Returns("some setting");

            var questionsController = new QuestionsController(mockDataRepository.Object, null, null, null,
                mockConfigurationRoot.Object);

            var result = await questionsController.GetQuestions(null);
            Assert.Equal(10, result.Count());
            mockDataRepository.Verify(mock=> mock.GetQuestions(), Times.Once);
        }

        [Fact]
        public async void GetQuestion_WhenHaveSearchParameters_ReturnCorrectQuestions()
        {
            var mockQuestions = new List<QuestionGetManyResponse>();
            mockQuestions.Add(new QuestionGetManyResponse()
            {
                QuestionId = 1,
                Title = "Test Title",
                Content = "Test Content",
                UserName = "User1"
            });
            
            var mockDataRepository = new Mock<IDataRepository>();
            mockDataRepository.Setup(repo => repo.GetQuestionsBySearchWithPaging("Test", 1, 20))
                .Returns(() => Task.FromResult(mockQuestions.AsEnumerable()));
            
            var mockConfigurationRoot = new Mock<IConfigurationRoot>();
            mockConfigurationRoot.Setup(config => config[It.IsAny<string>()])
                .Returns("some testing");

            var questionsController = new QuestionsController(mockDataRepository.Object, null, null, null,
                mockConfigurationRoot.Object);


            var result = await questionsController.GetQuestions("Test", false, 1, 20);
            Assert.Single(result);
            mockDataRepository.Verify(mock => mock.GetQuestionsBySearchWithPaging("Test", 1, 20), Times.Once);
        }

        [Fact]
        public async void GetQuestion_WhenQuestionNotFound_Returns404()
        {
            var mockDataRepository = new Mock<IDataRepository>();
            mockDataRepository.Setup(repo => repo.GetQuestion(1))
                .Returns(() => Task.FromResult(default(QuestionGetSingleResponse)));
            
            var mockQuestionCache = new Mock<IQuestionCache>();
            mockQuestionCache.Setup(cache => cache.Get(1))
                .Returns(() => null);
            
            var mockConfigurationRoot = new Mock<IConfigurationRoot>();
            mockConfigurationRoot.Setup(config => config[It.IsAny<string>()])
                .Returns("some setting");

            var questionsController = new QuestionsController(mockDataRepository.Object, null, mockQuestionCache.Object, null,
                mockConfigurationRoot.Object);

            var result = await questionsController.GetQuestion(1);
            var actionResult = Assert.IsType<ActionResult<QuestionGetSingleResponse>>(result);

            Assert.IsType<NotFoundResult>(actionResult.Result);
        }

        [Fact]
        public async void GetQuestion_WhenQuestionFound_ReturnsQuestion()
        {
            var mockQuestion = new QuestionGetSingleResponse
            {
                QuestionId = 1,
                Title = "Test Title",
                Content = "Test Content",
                UserName = "User1"
            };
            
            var mockDataRepository = new Mock<IDataRepository>();
            mockDataRepository.Setup(repo => repo.GetQuestion(1))
                .Returns(() => Task.FromResult(default(QuestionGetSingleResponse)));
            
            var mockQuestionCache = new Mock<IQuestionCache>();
            mockQuestionCache.Setup(cache => cache.Get(1))
                .Returns(() => mockQuestion);
            
            var mockConfigurationRoot = new Mock<IConfigurationRoot>();
            mockConfigurationRoot.Setup(config => config[It.IsAny<string>()])
                .Returns("some setting");
            
            var questionsController = new QuestionsController(mockDataRepository.Object, null, 
                mockQuestionCache.Object, null, mockConfigurationRoot.Object);

            var result = await questionsController.GetQuestion(1);

            var actionResult = Assert.IsType<ActionResult<QuestionGetSingleResponse>>(result);
            var questionResult = Assert.IsType<QuestionGetSingleResponse>(actionResult.Value);
            Assert.Equal(1, questionResult.QuestionId);
        }
    }
}