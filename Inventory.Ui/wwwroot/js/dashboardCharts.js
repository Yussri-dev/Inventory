// wwwroot/js/dashboardCharts.js
// Requires Chart.js loaded via CDN in index.html:
// <script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js"></script>

window.dashboardCharts = (() => {
    const charts = {};

    function destroy(id) {
        if (id && charts[id]) {
            charts[id].destroy();
            delete charts[id];
        } else {
            // Destroy all
            Object.keys(charts).forEach(k => {
                charts[k].destroy();
                delete charts[k];
            });
        }
    }

    function renderRevenue(canvasId, labels, values) {
        destroy(canvasId);

        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        charts[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: {
                labels,
                datasets: [{
                    label: 'Revenue (€)',
                    data: values,
                    backgroundColor: 'rgba(79, 70, 229, 0.6)',
                    borderColor: 'rgba(99, 102, 241, 1)',
                    borderWidth: 1,
                    borderRadius: 6,
                    borderSkipped: false,
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: '#1f2937',
                        borderColor: 'rgba(255,255,255,0.1)',
                        borderWidth: 1,
                        titleColor: '#e5e7eb',
                        bodyColor: '#9ca3af',
                        callbacks: {
                            label: ctx => ` €${ctx.parsed.y.toFixed(2)}`
                        }
                    }
                },
                scales: {
                    x: {
                        grid: { color: 'rgba(255,255,255,0.05)' },
                        ticks: { color: '#9ca3af', font: { size: 11 } }
                    },
                    y: {
                        grid: { color: 'rgba(255,255,255,0.05)' },
                        ticks: {
                            color: '#9ca3af',
                            font: { size: 11 },
                            callback: v => `€${v}`
                        },
                        beginAtZero: true
                    }
                }
            }
        });
    }

    function renderPayments(canvasId, labels, values) {
        destroy(canvasId);

        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        charts[canvasId] = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels,
                datasets: [{
                    data: values,
                    backgroundColor: [
                        'rgba(79, 70, 229, 0.8)',   // Cash — indigo
                        'rgba(34, 197, 94, 0.8)',    // Card — green
                        'rgba(245, 158, 11, 0.8)',   // Credit — amber
                    ],
                    borderColor: [
                        'rgba(99, 102, 241, 1)',
                        'rgba(52, 211, 153, 1)',
                        'rgba(252, 211, 77, 1)',
                    ],
                    borderWidth: 2,
                    hoverOffset: 8
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '68%',
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            color: '#9ca3af',
                            font: { size: 12 },
                            padding: 16,
                            usePointStyle: true,
                            pointStyleWidth: 10
                        }
                    },
                    tooltip: {
                        backgroundColor: '#1f2937',
                        borderColor: 'rgba(255,255,255,0.1)',
                        borderWidth: 1,
                        titleColor: '#e5e7eb',
                        bodyColor: '#9ca3af',
                        callbacks: {
                            label: ctx => ` €${ctx.parsed.toFixed(2)}`
                        }
                    }
                }
            }
        });
    }

    return { renderRevenue, renderPayments, destroy };
})();