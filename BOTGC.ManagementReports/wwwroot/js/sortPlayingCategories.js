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
