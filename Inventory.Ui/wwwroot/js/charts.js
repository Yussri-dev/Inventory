// wwwroot/js/charts.js
const chartInstances = {};

function destroyChart(id) {
    if (chartInstances[id]) {
        chartInstances[id].destroy();
        delete chartInstances[id];
    }
}

window.renderDonutChart = (id, labels, values) => {
    destroyChart(id);
    const ctx = document.getElementById(id);
    if (!ctx) return;

    chartInstances[id] = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels,
            datasets: [{
                data: values,
                backgroundColor: ['#4f46e5', '#f59e0b', '#ef4444'],
                borderColor: '#0d1117',
                borderWidth: 3,
                hoverOffset: 6
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            cutout: '68%',
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        color: '#94a3b8',
                        padding: 16,
                        font: { size: 12 }
                    }
                },
                tooltip: {
                    callbacks: {
                        label: ctx => ` €${ctx.parsed.toFixed(2)}`
                    }
                }
            }
        }
    });
};

window.renderBarChart = (id, labels, values) => {
    destroyChart(id);
    const ctx = document.getElementById(id);
    if (!ctx) return;

    const colors = ['#4f46e5', '#f59e0b', values[2] >= 0 ? '#22c55e' : '#ef4444'];

    chartInstances[id] = new Chart(ctx, {
        type: 'bar',
        data: {
            labels,
            datasets: [{
                label: 'Amount (€)',
                data: values,
                backgroundColor: colors,
                borderRadius: 8,
                borderSkipped: false
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: ctx => ` €${ctx.parsed.y.toFixed(2)}`
                    }
                }
            },
            scales: {
                x: {
                    grid: { color: 'rgba(255,255,255,.05)' },
                    ticks: { color: '#94a3b8' }
                },
                y: {
                    grid: { color: 'rgba(255,255,255,.05)' },
                    ticks: {
                        color: '#94a3b8',
                        callback: v => `€${v}`
                    }
                }
            }
        }
    });
};