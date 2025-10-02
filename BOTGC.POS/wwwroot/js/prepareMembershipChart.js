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
