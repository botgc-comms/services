function prepareJoinerLeaverBarChart(labels, dataPoints) {
    const categoryGroups = [...new Set(dataPoints.flatMap(dp => Object.keys(dp.dailyJoinersByCategoryGroup || {}).concat(
        Object.keys(dp.dailyLeaversByCategoryGroup || {}))
    ))];

    const categoryColors = {};
    categoryGroups.forEach((group, i) => {
        categoryColors[group] = getDistinctColor(i); // shared colour for both joiners & leavers
    });

    const joinerDatasets = categoryGroups.map(group => ({
        label: `Joiners - ${group}`,
        data: dataPoints.map(dp => (dp.dailyJoinersByCategoryGroup?.[group] || 0)),
        backgroundColor: `${categoryColors[group]}FF`,
        stack: 'joiners'
    }));

    const leaverDatasets = categoryGroups.map(group => ({
        label: `Leavers - ${group}`,
        data: dataPoints.map(dp => -(dp.dailyLeaversByCategoryGroup?.[group] || 0)),
        backgroundColor: `${categoryColors[group]}FF`,
        stack: 'leavers'
    }));

    const ctx = document.getElementById('dailyJoinLeaveChart');

    const chart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels,
            datasets: [...joinerDatasets, ...leaverDatasets]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            aspectRatio: 1.7,
            plugins: {
                legend: { position: 'top' },
                tooltip: {
                    callbacks: {
                        label: context => {
                            const value = Math.abs(context.raw);
                            const type = context.raw >= 0 ? 'Joiners' : 'Leavers';
                            return `${type} - ${context.dataset.label.split(' - ')[1]}: ${value}`;
                        }
                    }
                }
            },
            scales: {
                x: { stacked: true, title: { display: true, text: "Date" } },
                y: {
                    stacked: true,
                    beginAtZero: true,
                    title: { display: true, text: "Joiners (+) / Leavers (−)" }
                }
            }
        }
    });

    __charts.push(chart);
}
