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
