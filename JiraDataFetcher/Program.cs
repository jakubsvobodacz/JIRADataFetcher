using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

//Establish auth data & hostname

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();

string username = config["Jira:Username"];
string jiraUrl = config["Jira:JiraUrl"];
string apiToken = config["Jira:ApiToken"];
string jqlEpics = "project = \"TD\" AND type = Epic AND status = Done AND resolved >= startOfYear()";
string jqlSprint= "project = \"TD\" AND sprint in openSprints()&fields=*all, cf[11512]";
//string searchUrl = $"{jiraUrl}{jqlEpics}";
var configFileExists = File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
Console.WriteLine($"Config file exists: {configFileExists}");

//Dialogue start
Console.WriteLine("Fetching data from Jira...");

using (var client = new HttpClient())
{
    //Authentication
    var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{apiToken}"));
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
    
    //Fetch sprint data
    var response = await client.GetAsync($"{jiraUrl}{jqlSprint}");
    
    if (response.IsSuccessStatusCode)
    {
        var content = await response.Content.ReadAsStringAsync();
        var sprintIssues = JObject.Parse(content);
        
        //add data to json 
        string directoryPath = "DataFetch";
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        string filePath = Path.Combine(directoryPath, $"sprint.json");
        await File.WriteAllTextAsync(filePath, sprintIssues.ToString());
        Console.WriteLine($"Sprint data saved to {filePath}");
        
        //filter out only useful values
        JArray filteredSprintIssues = new JArray();
        foreach (var issue in sprintIssues["issues"])
        {
            JObject filteredIssue = new JObject();
            filteredIssue["epic"] = issue["fields"]["parent"] != null ? (string)issue["fields"]["parent"]["key"] : "no epic"; // Epic might be null
            filteredIssue["storyPoints"] = issue["fields"]["customfield_11512"]?.Type == JTokenType.Null ? 0 : issue["fields"]["customfield_11512"];
            filteredIssue["key"] = (string)issue["key"];
            filteredIssue["issueTypeName"] = (string)issue["fields"]["issuetype"]["name"];
            filteredIssue["summary"] = (string)issue["fields"]["summary"];
            filteredIssue["updated"] = (DateTime)issue["fields"]["updated"];
            filteredIssue["created"] = (DateTime)issue["fields"]["created"];
            filteredIssue["dueDate"] = (DateTime?)issue["fields"]["duedate"]; // Due date might be null

            var testValue = issue["fields"]["customfield_11512"].Any();
            
            filteredSprintIssues.Add(filteredIssue);
        }

        //var test = filteredSprintIssues.FirstOrDefault(issue => issue["epic"].ToString() != "no epic");
        
        // Calculate total numbers and write them into the console
        int totalIssues = filteredSprintIssues.Count();
        Console.WriteLine($"Total number of issues: {totalIssues}");

        int bugs = filteredSprintIssues.Where(issue => issue["issueTypeName"].ToString() == "Bug").Count();
        Console.WriteLine($"Total number of bugs: {bugs}");
        
        int maintenance = filteredSprintIssues.Where(issue => issue["issueTypeName"].ToString() == "Scheduled Maintenance").Count();
        var maintenanceStoryPointsTotal = filteredSprintIssues
            .Where(issue => issue["issueTypeName"].ToString() == "Scheduled Maintenance")
            .Sum(issue => issue["storyPoints"] != null ? Convert.ToDouble(issue["storyPoints"]) : 0);
        Console.WriteLine($"Total number of maintenance tasks: {maintenance} and their story point total: {maintenanceStoryPointsTotal}"); 
        
        // Calculate number of issues associated with an epic 
        var issuesWithEpic = filteredSprintIssues.Where(issue => issue["epic"].ToString() != "no epic").Count();
        var issuesWithEpicStoryPointsTotal = filteredSprintIssues
            .Where(issue => issue["epic"].ToString() != "no epic")
            .Sum(issue => issue["storyPoints"] != null ? Convert.ToDouble(issue["storyPoints"]) : 0);
        Console.WriteLine($"Number of issues associated with an epic: {issuesWithEpic} and their story point total: {issuesWithEpicStoryPointsTotal}");
        
        // Calculate number of issues associated with an epic
        var issuesWithoutEpic = filteredSprintIssues.Where(issue => issue["issueTypeName"].ToString() != "Scheduled Maintenance" && issue["issueTypeName"].ToString() != "Bug" && issue["epic"].ToString() == "no epic").Count();
        var issuesWithoutEpicStoryPointsTotal = filteredSprintIssues
            .Where(issue => issue["issueTypeName"].ToString() != "Scheduled Maintenance" && issue["issueTypeName"].ToString() != "Bug" && issue["epic"].ToString() == "no epic")
            .Sum(issue => issue["storyPoints"] != null ? Convert.ToDouble(issue["storyPoints"]) : 0);
        Console.WriteLine($"Number of issues without an associated epic: {issuesWithoutEpic} and their story point total: {issuesWithoutEpicStoryPointsTotal}");
        
        //int othersWithoutEpic = totalIssues - bugs - maintenance - issuesWithEpic;
        

        // Calculate the epic contribution ratio
        double epicContribution = ((double)issuesWithEpic / totalIssues)*100;
        Console.WriteLine($"Epic Contribution: {epicContribution}");
        
        // Calculate the Maintenance ratio
        double maintenanceRatio = ((double)maintenance / totalIssues)*100;
        Console.WriteLine($"Maintenance Contribution: {maintenanceRatio}");
        
        // Calculate the Bug ratio
        double bugRatio = ((double)bugs / totalIssues)*100;
        Console.WriteLine($"Bug Contribution: {bugRatio}");
        
        // Calculate the Others ratio
        double othersRatio = ((double)issuesWithoutEpic / totalIssues)*100;
        Console.WriteLine($"Other Issues Contribution: {othersRatio}");

        JObject sprintFacts = new JObject()
        {
            ["Total Number of Issues"] = totalIssues,
            ["Number of issues associated with an epic"] = issuesWithEpic,
            ["Number of issues without an associated epic"] = issuesWithoutEpic,
            ["epicRatio"] = Math.Round(epicContribution, 1),
            ["epicRatioStoryPoints"] = Math.Round(issuesWithEpicStoryPointsTotal, 1),
            ["maintenanceRatio"] = Math.Round(maintenanceRatio, 1),
            ["maintenanceRatioStoryPoints"] = Math.Round(maintenanceStoryPointsTotal, 1),
            ["bugRatio"] = Math.Round(bugRatio, 1),
            ["otherIssuesRatio"] = Math.Round(othersRatio, 1),
            ["otherIssuesRatioStoryPoints"] = Math.Round(issuesWithEpicStoryPointsTotal, 1)
        };
        
        string filePathSprintFacts = Path.Combine(directoryPath, $"facts_sprint.json");
        await File.WriteAllTextAsync(filePathSprintFacts, sprintFacts.ToString());
        Console.WriteLine($"Sprint Facts data saved to {filePathSprintFacts}");
        

    }
    else
    {
        Console.WriteLine($"Failed to fetch sprint data. Status code: {response.StatusCode}");
    }
    
    //Fetch epic data
    var responseEpic = await client.GetAsync($"{jiraUrl}{jqlEpics}");

    if (response.IsSuccessStatusCode)
    {
        var content = await responseEpic.Content.ReadAsStringAsync();
        var epicsList = JObject.Parse(content);
        
        // Extract desired properties from each issue and create a new JSON object
        JArray epics = (JArray)epicsList["issues"];
        JArray filteredEpics = new JArray();

        foreach (var epic in epics)
        {
            JObject filteredEpic = new JObject();
            filteredEpic["summary"] = (string)epic["fields"]["summary"];
            filteredEpic["updated"] = (DateTime)epic["fields"]["updated"];
            filteredEpic["created"] = (DateTime)epic["fields"]["created"];
            filteredEpic["dueDate"] = (DateTime?)epic["fields"]["duedate"]; // Due date might be null

            filteredEpics.Add(filteredEpic);
        }
        
        //add data to json 
        string directoryPath = "DataFetch/EpicData";
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        string filePath = Path.Combine(directoryPath, $"epics.json");
        await File.WriteAllTextAsync(filePath, filteredEpics.ToString());
        Console.WriteLine($"Filtered epics data saved to {filePath}");
        
    }
    else
    {
        Console.WriteLine($"Failed to fetch epic data. Status code: {responseEpic.StatusCode}");
    }
        
}

