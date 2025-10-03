function prepareCategoryGroupBreakdownChart(labels, dataPoints, annotations) {
    const allGroups = [...new Set(dataPoints.flatMap(dp => dp.categoryGroupBreakdown ? Object.keys(dp.categoryGroupBreakdown) : []
    ))];

    const combinedAnnotations = [...(annotations || [])];

    // Sort groups by average ascending
    const categoryAverages = {};
    allGroups.forEach(group => {
        const total = dataPoints.reduce((sum, dp) => sum + (dp.categoryGroupBreakdown?.[group] || 0), 0);
        categoryAverages[group] = total / dataPoints.length;
    });
    allGroups.sort((a, b) => categoryAverages[a] - categoryAverages[b] || a.localeCompare(b));

    // Prepare stacked data with carry-forward for gaps (you have no nulls, so just map)
    const stackedData = {};
    allGroups.forEach(group => {
        stackedData[group] = dataPoints.map(dp => dp.categoryGroupBreakdown?.[group] || 0);
    });

    const colors = assignCategoryColors(allGroups);

    const datasets = allGroups.map(group => ({
        label: group,
        data: stackedData[group],
        backgroundColor: `${colors[group]}55`,
        borderColor: colors[group],
        fill: 'origin',
        pointRadius: 0,
        borderWidth: 1
    }));

    const chart = new Chart(document.getElementById('categoryGroupBreakdownChart'), {
        type: 'line',
        data: {
            labels: labels,
            datasets: datasets
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            aspectRatio: 1.7,
            plugins: {
                legend: { position: 'top' },
                annotation: { annotations: combinedAnnotations }
            },
            scales: {
                x: { title: { display: true, text: "Date" } },
                y: {
                    stacked: true,
                    beginAtZero: true,
                    title: { display: true, text: "Members" }
                }
            }
        }
    });

    __charts.push(chart);
}
