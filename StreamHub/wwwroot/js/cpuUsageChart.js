window.barChartInterop = {
    createBarChart: function (canvasId, coreNames, initialData) {
        const ctx = document.getElementById(canvasId).getContext('2d');
        const chart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: coreNames,
                datasets: [{
                    label: 'CPU Usage (%)',
                    data: initialData,
                    backgroundColor: 'rgba(54, 162, 235, 0.2)',
                    borderColor: 'rgba(54, 162, 235, 1)',
                    borderWidth: 1
                }]
            },
            options: {
                indexAxis: 'y',
                scales: {
                    x: {
                        beginAtZero: true,
                        max: 100 // Assuming CPU usage max is 100%
                    }
                },
                responsive: true,
                maintainAspectRatio: true
            }
        });
        return chart;
    },

    updateBarChart: function (chart, newData) {
        chart.data.datasets[0].data = newData;
        chart.update();
    }
};
