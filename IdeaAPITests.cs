using IdeaAPITesting.Models;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;

namespace IdeaAPITesting
{
    public class IdeaAPITests
    {

        private RestClient client;
        private const string BASEURL = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:84";
        private const string EMAIL = "katia.al@test.com";
        private const string PASSWORD = "123456";

        private static string lastIdeaId;

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken = GetJwtToken(EMAIL, PASSWORD);

            var options = new RestClientOptions(BASEURL)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };
            client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            RestClient authClient = new RestClient(BASEURL);
            var request = new RestRequest("/api/User/Authentication");
            request.AddJsonBody(new { email, password });
            var response = authClient.Execute(request, Method.Post);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Accesstoken is null or empty");
                }
                return token;
            }

            else
            {
                throw new InvalidOperationException($"Unexpected responce type {response.StatusCode} with data {response.Content}");
            }
        }

        [Test, Order(1)]
        public void CreateNewIdea_WithCorrectData_ShouldSucceed()
        {
            var requestData = new IdeaDTO()
            {
                Title = "New Title",
                Description = "Description"
            };
            var request = new RestRequest("/api/Idea/Create");
            request.AddJsonBody(requestData);

            var response = client.Post(request);
            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(responseData.Msg, "Successfully created!");
        }

        [Test, Order(2)]
        public void GetAllIdeas_SchpuldReturnNotEmptyArray()
        {
            var request = new RestRequest("/api/Idea/All");

            var response = client.Get(request);
            var responseDataArray = JsonSerializer.Deserialize<ApiResponseDTO[]>(response.Content);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.Greater(responseDataArray.Length, 0);

            lastIdeaId = responseDataArray[responseDataArray.Length - 1].IdeaId;
        }

        [Test, Order(3)]
        public void EditNewIdea_WithCorrectData_ShouldSucceed()
        {
            var requestData = new IdeaDTO()
            {
                Title = "Edited Title",
                Description = "Edited Description"
            };
            var request = new RestRequest("/api/Idea/Edit");
            request.AddQueryParameter("ideaId", lastIdeaId);
            request.AddJsonBody(requestData);

            var response = client.Execute(request, Method.Put);
            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(responseData.Msg, "Edited successfully");
        }
        [Test, Order(4)]
        public void DeleteIdea_WithCorrectData_ShouldSucceed()
        {
           
            var request = new RestRequest("/api/Idea/Delete");
            request.AddQueryParameter("ideaId", lastIdeaId);

            var response = client.Execute(request, Method.Delete);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.That(response.Content, Does.Contain("The idea is deleted!"));
        }
        [Test, Order(5)]
        public void CreateNewIdea_WithWrongData_ShouldFail()
        {
            var requestData = new IdeaDTO()
            {
                Title = "New Title",
            };
            var request = new RestRequest("/api/Idea/Create");
            request.AddJsonBody(requestData);

            var response = client.Execute(request, Method.Post);
            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void EditNewIdea_WithWrongID_ShouldFail()
        {
            var requestData = new IdeaDTO()
            {
                Title = "Edited Title",
                Description = "Edited Description"
            };
            var request = new RestRequest("/api/Idea/Edit");
            request.AddQueryParameter("ideaId", "112255");
            request.AddJsonBody(requestData);

            var response = client.Execute(request, Method.Put);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.That(response.Content, Does.Contain("There is no such idea!"));
        }

        [Test, Order(7)]
        public void DeleteIdea_WithWrongId_ShouldFail()
        {

            var request = new RestRequest("/api/Idea/Delete");
            request.AddQueryParameter("ideaId", "11445");

            var response = client.Execute(request, Method.Delete);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.That(response.Content, Does.Contain("There is no such idea!"));
        }
    }
}