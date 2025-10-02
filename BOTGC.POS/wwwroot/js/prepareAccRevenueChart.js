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
                    label: "Actual Revenue (£)",
                    data: actuals,
                    borderColor: "blue",
                    backgroundColor: "rgba(0, 0, 255, 0.1)",
                    fill: false,
                    tension: 0.2,
                    pointRadius: 0
                },
                {
                    label: "Target Revenue (£)",
                    data: targets,
                    borderColor: "green",
                    borderDash: [4, 4],
                    backgroundColor: "rgba(0, 128, 0, 0.1)",
                    fill: false,
                    tension: 0.2,
                    pointRadius: 0
                },
                {
                    label: "Billed (£)",
                    data: billed,
                    borderColor: "purple",
                    backgroundColor: "rgba(128,0,128,.1)",
                    tension: .2, pointRadius: 0, fill: false // NEW
                },
                {
                    label: "Received (£)",
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
                    title: { display: true, text: "£ Revenue per Day" }
                }
            }
        }
    });

    __charts.push(chart);
}
