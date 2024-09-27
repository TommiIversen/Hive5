window.chartInterop = {
    createLineChart: function (canvasId, initialData) {
        const ctx = document.getElementById(canvasId).getContext('2d');
        return new Chart(ctx, {
            type: 'line',
            data: {
                labels: initialData.labels, // This should now be UNIX timestamps
                datasets: [
                    {
                        label: 'CPU Usage (%)',
                        data: initialData.cpuData,
                        borderColor: 'rgba(75, 192, 192, 1)',
                        fill: false
                    },
                    {
                        label: 'Network RX (MB)',
                        data: initialData.rxData,
                        borderColor: 'rgba(153, 102, 255, 1)',
                        fill: false
                    },
                    {
                        label: 'Network TX (MB)',
                        data: initialData.txData,
                        borderColor: 'rgba(255, 159, 64, 1)',
                        fill: false
                    }
                ]
            },
            options: {
                scales: {
                    x: {
                        type: 'time',
                        time: {
                            unit: 'minute', // Viser tid per minut
                            tooltipFormat: 'HH:mm:ss',
                            displayFormats: {
                                minute: 'HH:mm' // Vis kun time og minut på x-aksen
                            }
                        },
                        ticks: {
                            source: 'data' // Brug dataens tidspunkter til ticks
                        }
                    },
                    y: {
                        beginAtZero: true
                    }
                }
            }
        });
    },
    updateLineChart: function (chart, timestamp, cpuData, rxData, txData) {
        chart.data.labels.push(timestamp); // UNIX timestamp
        chart.data.datasets[0].data.push(cpuData);
        chart.data.datasets[1].data.push(rxData);
        chart.data.datasets[2].data.push(txData);

        if (chart.data.labels.length > 30) {
            chart.data.labels.shift();
            chart.data.datasets.forEach(dataset => dataset.data.shift());
        }

        chart.update();
    }
};
