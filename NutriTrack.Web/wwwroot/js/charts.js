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

    renderLine(canvasId, labels, values, label) {
        this._destroy(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;
        this._charts[canvasId] = new Chart(ctx, {
            type: 'line',
            data: {
                labels,
                datasets: [{
                    label,
                    data: values,
                    borderColor: '#667eea',
                    backgroundColor: 'rgba(102, 126, 234, 0.15)',
                    borderWidth: 2,
                    pointRadius: 3,
                    tension: 0.25,
                    fill: true
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    y: { beginAtZero: false, ticks: { font: { size: 11 } } },
                    x: { ticks: { font: { size: 11 }, maxRotation: 0, autoSkip: true } }
                }
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
