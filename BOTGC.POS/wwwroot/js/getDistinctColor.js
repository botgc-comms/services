function getDistinctColor(index) {
    const colors = [
        '#FF5733', '#33FF57', '#3357FF', '#F39C12', '#9B59B6',
        '#E74C3C', '#2ECC71', '#3498DB', '#1ABC9C', '#E67E22'
    ];
    return colors[index % colors.length];
}
