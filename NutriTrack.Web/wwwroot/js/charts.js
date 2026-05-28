window.NutriCharts = {
    _charts: {},

    renderDonut(canvasId, labels, values, colors) {
        this._destroy(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;
        this._charts[canvasId] = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels,
                datasets: [{
                    data: values,
                    backgroundColor: colors,
                    borderWidth: 2,
                    borderColor: '#fff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { position: 'bottom', labels: { padding: 16, font: { size: 12 } } }
                },
                cutout: '65%'
            }
        });
    },

    _destroy(canvasId) {
        if (this._charts[canvasId]) {
            this._charts[canvasId].destroy();
            delete this._charts[canvasId];
        }
    }
};
