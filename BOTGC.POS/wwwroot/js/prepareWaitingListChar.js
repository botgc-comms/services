function prepareWaitingListChart(labels, dataPoints, startDate, endDate) {

    const filtered = dataPoints.filter(dp => {
        const d = new Date(dp.date);
        return d >= startDate && d <= endDate;
    });

    const filteredLabels = filtered.map(dp =>
        new Date(dp.date).toLocaleDateString("en-GB", { day: "2-digit", month: "2-digit", year: "numeric" })
    );

    const allWaitingCategories = [
        ...new Set(filtered.flatMap(dp =>
            dp.waitingListCategoryBreakdown ? Object.keys(dp.waitingListCategoryBreakdown) : []
        ))
    ];

    const stackedData = {};
    allWaitingCategories.forEach(category => {
        stackedData[category] = filtered.map(dp =>
            dp.waitingListCategoryBreakdown?.[category] || 0
        );
    });

    const colors = assignCategoryColors(allWaitingCategories);

    const datasets = allWaitingCategories.map(category => ({
        label: category,
        data: stackedData[category],
        backgroundColor: colors[category]
    }));

    const chart = new Chart(document.getElementById('waitingListChart'), {
        type: 'bar',
        data: {
            labels: filteredLabels,
            datasets: datasets
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            aspectRatio: 1.7,
            plugins: {
                legend: { position: 'top' }
            },
            scales: {
                x: {
                    stacked: true,
                    title: { display: true, text: "Date" }
                },
                y: {
                    stacked: true,
                    beginAtZero: true,
                    title: { display: true, text: "Members on Waiting List" }
                }
            }
        }
    });

    __charts.push(chart);
}