using Lineage.Runner;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = FindContentRoot()
});
builder.Services.AddSingleton<LineageRunManager>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

var api = app.MapGroup("/api");

api.MapGet("/scenarios", (LineageRunManager manager) => Results.Ok(manager.ListScenarios()));

api.MapPost("/scenarios/user", (ScenarioSaveRequest request, LineageRunManager manager) =>
{
    try
    {
        return Results.Ok(manager.SaveUserScenario(request));
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

api.MapDelete("/scenarios", (string path, LineageRunManager manager) =>
{
    try
    {
        return Results.Ok(manager.DeleteUserScenario(path));
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

api.MapGet("/scenario-editor", (string path, LineageRunManager manager) =>
{
    try
    {
        return Results.Ok(manager.GetScenarioEditor(path));
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

api.MapGet("/runs", (LineageRunManager manager) => Results.Ok(manager.ListRuns()));

api.MapPost("/runs/export", (RunExportRequest request, LineageRunManager manager) =>
    Results.Text(manager.ExportRunsMarkdown(request.Ids), "text/markdown"));

api.MapGet("/runs/{id}", (string id, LineageRunManager manager) =>
{
    var run = manager.GetRun(id);
    return run is null ? Results.NotFound() : Results.Ok(run);
});

api.MapGet("/runs/{id}/details", (string id, int? lines, LineageRunManager manager) =>
{
    var details = manager.GetRunDetails(id, lines ?? 80);
    return details is null ? Results.NotFound() : Results.Ok(details);
});

api.MapGet("/runs/{id}/clone-settings", (string id, LineageRunManager manager) =>
{
    try
    {
        var settings = manager.GetRunCloneSettings(id);
        return settings is null ? Results.NotFound() : Results.Ok(settings);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

api.MapPost("/runs", async (RunCreateRequest request, LineageRunManager manager) =>
{
    try
    {
        var run = await manager.StartRunAsync(request);
        return Results.Created($"/api/runs/{run.Id}", run);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

api.MapPost("/runs/{id}/rerun", async (string id, LineageRunManager manager) =>
{
    try
    {
        var result = await manager.RerunRunAsync(id);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

api.MapPost("/runs/{id}/stop", (string id, LineageRunManager manager) =>
    manager.SendControl(id, "stop") ? Results.Accepted() : Results.NotFound());

api.MapPost("/runs/{id}/checkpoint", (string id, LineageRunManager manager) =>
    manager.SendControl(id, "checkpoint") ? Results.Accepted() : Results.NotFound());

api.MapPost("/runs/{id}/checkpoint-stop", (string id, LineageRunManager manager) =>
    manager.SendControl(id, "checkpoint-and-stop") ? Results.Accepted() : Results.NotFound());

api.MapPatch("/runs/{id}", (string id, RunRenameRequest request, LineageRunManager manager) =>
{
    try
    {
        var run = manager.RenameRun(id, request.Name);
        return run is null ? Results.NotFound() : Results.Ok(run);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

api.MapPost("/runs/bulk-delete", (RunBulkDeleteRequest request, LineageRunManager manager) =>
    Results.Ok(manager.DeleteRuns(request.Ids, deleteArtifacts: true)));

api.MapDelete("/runs/{id}", (string id, LineageRunManager manager) =>
    manager.DeleteRun(id, deleteArtifacts: true) ? Results.NoContent() : Results.Conflict());

api.MapGet("/runs/{id}/report", (string id, LineageRunManager manager) =>
{
    var reportPath = manager.GetReportPath(id);
    return reportPath is null
        ? Results.NotFound()
        : Results.File(reportPath, "text/html");
});

app.MapFallbackToFile("index.html");
app.Run();

static string FindContentRoot()
{
    foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
    {
        var directory = new DirectoryInfo(start);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Lineage.Runner.csproj"))
                && Directory.Exists(Path.Combine(directory.FullName, "wwwroot")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }
    }

    return Directory.GetCurrentDirectory();
}
