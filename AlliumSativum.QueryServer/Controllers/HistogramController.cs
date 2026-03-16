using System.Text;
using AlliumSativum.Compiler;
using AlliumSativum.QueryServer.Utils;
using AlliumSativum.Shared.Costs;
using AlliumSativum.Shared.Database;
using AlliumSativum.Shared.Database.Entities;
using AlliumSativum.Shared.Models.ExecutionPlan.PlanOperators;
using AlliumSativum.Shared.Utils;
using Microsoft.AspNetCore.Mvc;
using ScottPlot;
using ScottPlot.Statistics;

namespace AlliumSativum.QueryServer.Controllers;

[Controller]
[Route("[controller]")]
public class HistogramController : Controller
{
    private readonly QueryCompiler _compiler;
    private readonly ICostModel _costModel;
    private readonly DataUtils _dataUtils;

    public HistogramController(
        QueryCompiler compiler,
        ICostModel costModel,
        DataUtils dataUtils)
    {
        _compiler = compiler;
        _costModel = costModel;
        _dataUtils = dataUtils;
    }

    [HttpPost("reconstructed")]
    public async Task<IResult> GetReconstructedHistogram([FromBody] CompileInput query)
    {
        var plt = new Plot();

        var plan = await _compiler.CompileAsync(query.Query);
        if (plan.RootOperator is not ProjectPlanOperator pop)
            return Results.Content("<html><body><p>Only simple select queries are supported</p></body></html>",
                "text/html");
        if (pop.Attributes.Count != 1)
            return Results.Content("<html><body><p>You need to project to one operator here</p></body></html>",
                "text/html");

        var parsed = await _dataUtils.LoadDataAsync(plan);
        var map = parsed
            .GroupBy(x => x)
            .OrderBy(x => x.Key)
            .ToDictionary(g => g.Key, g => g.Count());
        var barWidth = 0.4;
        var offset = barWidth / 2;

        var originalPlotPositions = map.Keys.Select(k => k - offset).ToArray();
        var originalPlotHeights = map.Values.Select(x => (double)x).ToArray();
        var originalPlot = plt.Add.Bars(originalPlotPositions, originalPlotHeights);
        originalPlot.LegendText = "Original";

        foreach (var bar in originalPlot.Bars)
        {
            bar.Size = barWidth;
            bar.LineStyle = LineStyle.None;
            bar.LineWidth = 0;
        }

        var distributionData = plan.RootOperator.DistributionData.Single().Value;
        var reconstructed = _costModel.ReconstructDistribution(distributionData);

        var reconstructedPlotPositions = reconstructed.Keys.Select(k => k + offset).ToArray();
        var reconstructedPlotHeights = reconstructed.Values.ToArray();
        var reconstructedPlot = plt.Add.Bars(reconstructedPlotPositions, reconstructedPlotHeights);
        reconstructedPlot.LegendText = "Reconstructed";

        foreach (var bar in reconstructedPlot.Bars)
        {
            bar.Size = barWidth;
            bar.LineStyle = LineStyle.None;
            bar.LineWidth = 0;
        }

        plt.Axes.Margins(bottom: 0);
        plt.Title("Original vs Reconstructed Distribution");
        var legend = plt.ShowLegend();
        legend.Alignment = Alignment.UpperLeft;
        legend.FontName = "Arial";
        legend.FontSize = 14;

        var svg = plt.GetSvgXml(1200, 800);
        var stringBuilder = new StringBuilder();
        stringBuilder.Append("<html><body>")
            .Append(svg)
            .Append("</body></html>");

        return Results.Content(stringBuilder.ToString(), "text/html");
    }

    [HttpPost]
    public async Task<IResult> GetHistogram([FromBody] List<CompileInput> queries)
    {
        List<Color> colors =
        [
            Color.FromHex("#6CD4FF"),
            Color.FromHex("#FE938C")
        ];
        var plt = new Plot();

        List<AttributeEntity> attributes = [];
        List<Dictionary<double, int>> maps = [];
        double min = 0, max = 0;

        var index = 0;
        foreach (var query in queries)
        {
            var plan = await _compiler.CompileAsync(query.Query);
            if (plan.RootOperator is not ProjectPlanOperator pop)
                return Results.Content("<html><body><p>Only simple select queries are supported</p></body></html>",
                    "text/html");
            if (pop.Attributes.Count != 1)
                return Results.Content("<html><body><p>You need to project to one operator here</p></body></html>",
                    "text/html");

            var parsed = await _dataUtils.LoadDataAsync(plan);

            var (attribute, _) =
                DistributionUtils.CalculateDistribution(parsed.Select(x => (double?)x).ToList(), new AttributeEntity());
            attributes.Add(attribute);

            var map = parsed
                .GroupBy(x => x)
                .OrderBy(x => x.Key)
                .ToDictionary(g => g.Key, g => g.Count());
            min = map.Keys.Min() < min ? map.Keys.Min() : min;
            max = map.Keys.Max() > max ? map.Keys.Max() : max;
            maps.Add(map);

            var hist = Histogram.WithBinCount(map.Count, parsed);
            var histPlot = plt.Add.Histogram(hist, colors[index]);
            histPlot.BarWidthFraction = 0.8;

            index++;
        }

        plt.Axes.Margins(bottom: 0);
        plt.Axes.Bottom.Min = min;
        plt.Axes.Bottom.Max = min;

        var svg = plt.GetSvgXml(600, 400);
        var stringBuilder = new StringBuilder();
        stringBuilder.Append("<html><body>")
            .Append(svg)
            .Append("<table><tr><th>Key</th>");

        for (var i = 0; i < maps.Count; i++) stringBuilder.Append("<th>Query " + (i + 1) + "</th>");
        stringBuilder.Append("</tr>");

        for (var i = min; i < max; i++)
        {
            stringBuilder.Append($"<tr><td>{i}</td> ");
            foreach (var map in maps)
            {
                var entry = map.Where(kv => kv.Key >= i).OrderBy(kv => kv.Key).FirstOrDefault();
                stringBuilder.Append($"<td>{entry.Value}</td>");
            }

            stringBuilder.Append("</tr>");
        }

        stringBuilder
            .Append("</body></html>");

        return Results.Content(stringBuilder.ToString(), "text/html");
    }
}