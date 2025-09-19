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