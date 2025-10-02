function prepareCategoryBreakdownChart(labels, sortedCategories, stackedData, categoryColors) {
    const categoryDatasets = sortedCategories.map(category => ({
        label: category,
        data: stackedData[category],
        borderColor: categoryColors[category],
        backgroundColor: `${categoryColors[category]}55`, 
        borderWidth: 2,
        pointRadius: 0,
        fill: '-1'
    }));

    new Chart(document.getElementById('categoryBreakdownChart'), {
        type: 'line',
        data: {
            labels: labels,
            datasets: categoryDatasets
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            aspectRatio: 1.7,
            plugins: {
                legend: { position: 'top' },
                datalabels: {
                    align: "center",
                    anchor: "center",
                    color: "white",
                    font: {
                        weight: "bold",
                        size: 12
                    },
                    formatter: function (value, context) {
                        const dataset = context.dataset.data;
                        const index = context.dataIndex;
                        const midpoint = Math.floor(dataset.length / 2);
                        return index === midpoint ? context.dataset.label : "";
                    }
                }
            },
            scales: {
                x: { title: { display: true, text: "Date" } },
                y: {
                    stacked: true,
                    beginAtZero: true,
                    title: { display: true, text: "Members" }
                }
            }
        },
        plugins: [ChartDataLabels]
    });
}