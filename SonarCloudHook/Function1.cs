using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using Octokit;
using Octokit.Internal;
using SonarCloudHook.Generated.SonarCloud.SearchIssue;

namespace SonarCloudHook
{
    public static class GitHubHook
    {

        private static HttpClient client;
        private static GitHubClient gitHubClient;
        static GitHubHook()
        {
            string PAT = Environment.GetEnvironmentVariable("SonarPAT");
            client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(
    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(
                    System.Text.ASCIIEncoding.ASCII.GetBytes(
                        string.Format("{0}:{1}", "", PAT))));
            gitHubClient = new GitHubClient(new ProductHeaderValue("CIHookFunctino"));
            var tokenAuth = new Credentials(Environment.GetEnvironmentVariable("GitHubPAT"));
            gitHubClient.Credentials = tokenAuth; 
        }

        [FunctionName("CIHook")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            string pullRequestId = req.Query["pullRequestId"];
            string projectKey = req.Query["projectKey"];
            string commitId = req.Query["commitId"];
            log.LogInformation($"PullRequestId: {pullRequestId} ProjectKey: {projectKey}");

            SearchIssue issues = null;
             using (HttpResponseMessage response = await client.GetAsync(
                    $"https://sonarcloud.io/api/issues/search?pullRequest={pullRequestId}&projects=TsuyoshiUshio_VulnerableApp"))
                {
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseBody);
                    issues = JsonConvert.DeserializeObject<SearchIssue>(responseBody);
                }

             foreach(var issue in issues.issues)
            {

                var comment = new PullRequestReviewCommentCreate(issue.message, commitId, issue.component, issue.line);
                await gitHubClient.PullRequest.ReviewComment.Create("TsuyoshiUshio", "VulnerableApp", int.Parse(pullRequestId), comment);
            }

            return (ActionResult)new OkObjectResult($"Done");
        }


    }
}
