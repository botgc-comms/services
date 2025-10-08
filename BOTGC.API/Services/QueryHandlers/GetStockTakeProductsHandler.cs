using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetStockTakeProductsHandler(IOptions<AppSettings> settings,
                                             IMediator mediator,
                                             ILogger<GetStockTakeProductsHandler> logger,
                                             IDataProvider dataProvider) : QueryHandlerBase<GetStockTakeProductsQuery, List<DivisionStockTakeSuggestionDto>>
    {
        private const string __CACHE_KEY = "Membership_Subscriptions_{fromDate}_{toDate}";

        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        private readonly ILogger<GetStockTakeProductsHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));

        // Common English stopwords to ignore in clustering
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "the", "and", "of", "a", "an", "in", "on", "for", "to", "with", "by", "at", "from", "is", "are", "as", "it", "its"
        };

        public async override Task<List<DivisionStockTakeSuggestionDto>> Handle(GetStockTakeProductsQuery request, CancellationToken cancellationToken)
        {
            // 1. Get all stock items and their stock take history
            var stockTakes = await _mediator.Send(new GetStockTakesQuery(), cancellationToken);
            var minDays = request.MinDaysSinceLastStockTake ?? 30;

            // 2. Group by division
            var byDivision = stockTakes.GroupBy(x => x.Division);

            var result = new List<DivisionStockTakeSuggestionDto>();

            foreach (var divisionGroup in byDivision)
            {
                if (!divisionGroup.Any(p => (p.DaysSinceLastStockTake ?? int.MaxValue) >= minDays))
                    continue;

                // 3. Advanced name clustering (location grouping)
                var clusters = ClusterByName(divisionGroup.ToList());

                // 4. Select products for stock take, considering complexity and starvation avoidance
                int complexityBudget = request.ComplexityBudgetPerDivision ?? 10;
                var selectedProducts = new List<StockTakeSummaryDto>();
                var alreadySelected = new HashSet<int>();

                foreach (var cluster in clusters.OrderByDescending(c => c.Products.Count))
                {
                    // Sort by longest since last stock take
                    var candidates = cluster.Products
                        .OrderByDescending(p => p.DaysSinceLastStockTake ?? int.MaxValue)
                        .ThenBy(p => p.Name)
                        .ToList();

                    foreach (var product in candidates)
                    {
                        if (alreadySelected.Contains(product.StockItemId))
                            continue;

                        int complexity = 1; // Default if not found
                        if (!string.IsNullOrWhiteSpace(product.Division) && _settings.StockTake.StockTakeComplexity.TryGetValue(product.Division.ToLower(), out var divComplexity))
                        {
                            complexity = divComplexity;
                        }

                        selectedProducts.Add(product);
                        alreadySelected.Add(product.StockItemId);
                        complexityBudget -= complexity;

                        // Avoid starvation: if product has never been selected, always include at least one per run
                        if (product.DaysSinceLastStockTake == null && complexityBudget > 0)
                        {
                            selectedProducts.Add(product);
                            alreadySelected.Add(product.StockItemId);
                            complexityBudget -= complexity;
                        }

                        if (complexityBudget <= 0)
                            break;
                    }

                    if (complexityBudget <= 0)
                        break;
                }

                // 5. Fallback: ensure products never stock-taked are included
                var neverStockTaked = divisionGroup
                    .Where(p => p.DaysSinceLastStockTake == null && !alreadySelected.Contains(p.StockItemId))
                    .OrderBy(p => p.Name)
                    .Take(2);

                selectedProducts.AddRange(neverStockTaked);

                result.Add(new DivisionStockTakeSuggestionDto
                {
                    Division = divisionGroup.Key,
                    Products = selectedProducts.DistinctBy(p => p.StockItemId).ToList()
                });
            }

            return result;
        }

        // --- Advanced Name Clustering ---
        private List<ProductCluster> ClusterByName(List<StockTakeSummaryDto> products)
        {
            // Tokenize names, ignore stopwords, build word sets
            var productWordSets = products.Select(p => new
            {
                Product = p,
                Words = TokenizeName(p.Name)
            }).ToList();

            // Cluster by Jaccard similarity
            var clusters = new List<ProductCluster>();
            var unclustered = new HashSet<StockTakeSummaryDto>(products);

            while (unclustered.Any())
            {
                var seed = unclustered.First();
                var seedWords = TokenizeName(seed.Name);

                var clusterProducts = productWordSets
                    .Where(x => unclustered.Contains(x.Product) && JaccardSimilarity(seedWords, x.Words) > 0.5)
                    .Select(x => x.Product)
                    .ToList();

                if (!clusterProducts.Any())
                    clusterProducts.Add(seed);

                clusters.Add(new ProductCluster
                {
                    Words = seedWords,
                    Products = clusterProducts
                });

                foreach (var p in clusterProducts)
                    unclustered.Remove(p);
            }

            return clusters;
        }

        private HashSet<string> TokenizeName(string name)
        {
            var words = Regex.Split(name ?? "", @"\W+")
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Select(w => w.ToLowerInvariant())
                .Where(w => !StopWords.Contains(w))
                .ToHashSet();
            return words;
        }

        private double JaccardSimilarity(HashSet<string> setA, HashSet<string> setB)
        {
            if (setA.Count == 0 || setB.Count == 0) return 0;
            var intersection = setA.Intersect(setB).Count();
            var union = setA.Union(setB).Count();
            return union == 0 ? 0 : (double)intersection / union;
        }

        private class ProductCluster
        {
            public HashSet<string> Words { get; set; } = new();
            public List<StockTakeSummaryDto> Products { get; set; } = new();
        }
    }

}
