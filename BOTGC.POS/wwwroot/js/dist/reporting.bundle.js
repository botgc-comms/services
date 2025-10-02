function prepareCategoryGroupBreakdownChart(labels, dataPoints) {
    const allGroups = [...new Set(dataPoints.flatMap(dp => dp.categoryGroupBreakdown ? Object.keys(dp.categoryGroupBreakdown) : []
    ))];

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
                legend: { position: 'top' }
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

function prepareMembershipChart(labels, playingMembers, nonPlayingMembers, targetPlayingMembers, annotations) {

    const targetPoints = targetPlayingMembers
        .map((value, index) => ({ value, index }))
        .filter(p => p.value !== undefined);

    const redDotAnnotations = targetPoints.length >= 2
        ? [
            {
                type: 'point',
                xValue: labels[targetPoints[0].index],
                yValue: targetPoints[0].value,
                backgroundColor: 'red',
                radius: 5,
                borderColor: 'white',
                borderWidth: 1
            },
            {
                type: 'point',
                xValue: labels[targetPoints[targetPoints.length - 1].index],
                yValue: targetPoints[targetPoints.length - 1].value,
                backgroundColor: 'red',
                radius: 5,
                borderColor: 'white',
                borderWidth: 1
            }
        ]
        : [];

    const combinedAnnotations = [...(annotations || []), ...redDotAnnotations];

    var ctx = document.getElementById('membershipChart').getContext('2d');

    const chart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Non-Playing Members',
                    data: nonPlayingMembers,
                    borderColor: 'red',
                    backgroundColor: 'rgba(255, 0, 0, 0.3)',
                    borderWidth: 1,
                    pointRadius: 0,
                    fill: true
                },
                {
                    label: 'Playing Members',
                    data: playingMembers,
                    borderColor: 'blue',
                    backgroundColor: 'rgba(0, 0, 255, 0.3)',
                    borderWidth: 1,
                    pointRadius: 0,
                    fill: true
                },
                {
                    label: 'Target Playing Members (5% Growth)',
                    data: targetPlayingMembers,
                    spanGaps: false,
                    borderColor: 'green',
                    borderDash: [5, 5],
                    borderWidth: 1,
                    pointRadius: 0,
                    fill: false
                }
            ]
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
                    stacked: false,
                    beginAtZero: true,
                    title: { display: true, text: "Members" }
                }
            }
        }
    });

    __charts.push(chart);
}

function prepareAccRevenueChart(dataPoints, annotations, startDate, endDate) {

    const filtered = dataPoints.filter(dp => {
        const d = new Date(dp.date);
        return d >= startDate && d <= endDate;
    });

    const labels = filtered.map(dp => new Date(dp.date).toLocaleDateString("en-GB"));

    let actualSum = 0;
    let targetSum = 0;
    let billSum = 0;
    let recSum = 0;
    const actuals = filtered.map(dp => (actualSum += dp.actualRevenue || 0));
    const targets = filtered.map(dp => (targetSum += dp.targetRevenue || 0));
    const billed = filtered.map(dp => (billSum += (dp.billedRevenue || 0)));
    const received = filtered.map(dp => (recSum += (dp.receivedRevenue || 0)));

    const chart = new Chart(document.getElementById('accRevenueVsBudgetChart'), {
        type: 'line',
        data: {
            labels,
            datasets: [
                {
                    label: "Actual Revenue (�)",
                    data: actuals,
                    borderColor: "blue",
                    backgroundColor: "rgba(0, 0, 255, 0.1)",
                    fill: false,
                    tension: 0.2,
                    pointRadius: 0
                },
                {
                    label: "Target Revenue (�)",
                    data: targets,
                    borderColor: "green",
                    borderDash: [4, 4],
                    backgroundColor: "rgba(0, 128, 0, 0.1)",
                    fill: false,
                    tension: 0.2,
                    pointRadius: 0
                },
                {
                    label: "Billed (�)",
                    data: billed,
                    borderColor: "purple",
                    backgroundColor: "rgba(128,0,128,.1)",
                    tension: .2, pointRadius: 0, fill: false // NEW
                },
                {
                    label: "Received (�)",
                    data: received,
                    borderColor: "orange",
                    backgroundColor: "rgba(255,165,0,.1)",
                    tension: .2, pointRadius: 0, fill: false // NEW
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            aspectRatio: 1.7,
            plugins: {
                legend: { position: 'top' },
                annotation: {
                    annotations: annotations
                }
            },
            scales: {
                x: {
                    title: { display: true, text: "Date" },
                    ticks: {
                        callback: function(value, index, ticks) {
                            return this.getLabelForValue(value);
                        }
                    }
                },
                y: {
                    beginAtZero: true,
                    title: { display: true, text: "� Revenue per Day" }
                }
            }
        }
    });

    __charts.push(chart);
}

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
function sortPlayingCategories(dataPoints) {
    let categoryAverages = {};
    const playingCategories = [...new Set(dataPoints.flatMap(dp => dp.playingCategoryBreakdown ? Object.keys(dp.playingCategoryBreakdown) : []))];

    playingCategories.forEach(category => {
        const total = dataPoints.reduce((sum, dp) => sum + (dp.playingCategoryBreakdown[category] || 0), 0);
        const average = total / dataPoints.length;
        categoryAverages[category] = average;
    });

    return playingCategories.sort((a, b) => categoryAverages[a] - categoryAverages[b] || a.localeCompare(b));
}

function assignCategoryColors(sortedCategories) {
    return sortedCategories.reduce((acc, category, index) => {
        const color = getDistinctColor(index);
        acc[category] = color;
        return acc;
    }, {});
}

function prepareRevenueChart(dataPoints, annotations, fyStart, fyEnd) {

    const filtered = dataPoints.filter(dp => {
        const d = new Date(dp.date);
        return d >= fyStart && d <= fyEnd;
    });

    const labels = filtered.map(dp => new Date(dp.date).toLocaleDateString("en-GB"));
    const actuals = filtered.map(dp => dp.actualRevenue || 0);
    const targets = filtered.map(dp => dp.targetRevenue || 0);
    const billed = filtered.map(dp => dp.billedRevenue || 0);
    const received = filtered.map(dp => dp.receivedRevenue || 0);

    /* ──────── 1️⃣  work out lowest daily value across *all* series ──────── */
    const minDaily = Math.min(
        ...actuals, ...targets, ...billed, ...received
    );

    /* 20 % below the min, but never negative */
    const yMin = Math.max(0, Math.floor(minDaily * 0.8));

    const chart = new Chart(document.getElementById('revenueVsBudgetChart'), {
        type: 'line',
        data: {
            labels,
            datasets: [
                {
                    label: "Actual Revenue (£)",
                    data: actuals,
                    borderColor: "blue",
                    backgroundColor: "rgba(0,0,255,.10)",
                    fill: false,
                    tension: .2, pointRadius: 0
                },
                {
                    label: "Target Revenue (£)",
                    data: targets,
                    borderColor: "green",
                    borderDash: [4, 4],
                    backgroundColor: "rgba(0,128,0,.10)",
                    fill: false,
                    tension: .2, pointRadius: 0
                },
                {
                    label: "Billed (£)",
                    data: billed,
                    borderColor: "purple",
                    backgroundColor: "rgba(128,0,128,.10)",
                    fill: false,
                    tension: .2, pointRadius: 0
                },
                {
                    label: "Received (£)",
                    data: received,
                    borderColor: "orange",
                    backgroundColor: "rgba(255,165,0,.10)",
                    fill: false,
                    tension: .2, pointRadius: 0
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            aspectRatio: 1.7,
            plugins: {
                legend: { position: 'top' },
                annotation: { annotations }
            },
            scales: {
                x: {
                    title: { display: true, text: "Date" },
                    ticks: {
                        callback(value) { return this.getLabelForValue(value); }
                    }
                },
                y: {
                    title: { display: true, text: "£ Revenue per Day" },
                    beginAtZero: false,        // ⬅︎ let min/max take control
                    min: yMin                  // ⬅︎ start 20 % below series-min
                }
            }
        }
    });

    __charts.push(chart);
}
function prepareStackedData(dataPoints, sortedCategories) {
    let stackedData = {};
    sortedCategories.forEach(category => stackedData[category] = []);

    dataPoints.forEach((dp, index) => {
        sortedCategories.forEach(category => {
            let value = dp.playingCategoryBreakdown[category] || null;
            if (value === null && index > 0) {
                value = stackedData[category][index - 1]; // Carry forward last value
            }
            stackedData[category].push(value);
        });
    });

    return stackedData;
}

function getDistinctColor(index) {
    const colors = [
        '#FF5733', '#33FF57', '#3357FF', '#F39C12', '#9B59B6',
        '#E74C3C', '#2ECC71', '#3498DB', '#1ABC9C', '#E67E22'
    ];
    return colors[index % colors.length];
}
