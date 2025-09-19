function assignCategoryColors(sortedCategories) {
    return sortedCategories.reduce((acc, category, index) => {
        const color = getDistinctColor(index);
        acc[category] = color;
        return acc;
    }, {});
}
