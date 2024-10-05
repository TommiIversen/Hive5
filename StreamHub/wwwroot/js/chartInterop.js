window.chartInterop = {
    createLineChart: function (canvasId, initialData) {
        const ctx = document.getElementById(canvasId).getContext('2d');
        const chart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: initialData.labels, // UNIX timestamps
                datasets: [
                    {
                        label: 'CPU Usage (%)',
                        data: initialData.cpuData,
                        borderColor: 'rgba(75, 192, 192, 1)',
                        fill: false
                    },
                    {
                        label: 'Network RX (%)',
                        data: initialData.rxData,
                        borderColor: 'rgba(153, 102, 255, 1)',
                        fill: false
                    },
                    {
                        label: 'Network TX (%)',
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
                            unit: 'minute',
                            tooltipFormat: 'HH:mm:ss',
                            displayFormats: {
                                minute: 'HH:mm'
                            }
                        },
                        ticks: {
                            source: 'data'
                        }
                    },
                    y: {
                        beginAtZero: true
                    }
                },
                plugins: {
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                const datasetLabel = context.dataset.label || '';
                                const value = context.raw.toFixed(1); 
                                if (context.datasetIndex === 1 || context.datasetIndex === 2) {
                                    // Hvis det er Network RX eller TX, skal vi tilføje Mbps til tooltip
                                    const metricData = context.chart.data.metricData ? context.chart.data.metricData[context.dataIndex] : null;
                                    if (metricData) {
                                        const rxOrTx = context.datasetIndex === 1 ? metricData.RxMbps : metricData.TxMbps;
                                        return `${datasetLabel}: ${value}% (${rxOrTx.toFixed(2)} Mbps)`;
                                    }
                                }
                                return `${datasetLabel}: ${value}%`;
                            }
                        }
                    }
                }
            }
        });

        // Initialiser metricData til at gemme alle værdier
        chart.data.metricData = initialData.metricData;

        return chart;
    },
    updateLineChart: function (chart, serializedMetricData) {
        const metricData = JSON.parse(serializedMetricData); // Parse serialized data from C#
       console.log('Updating chart with new data:', metricData)

        chart.data.labels.push(metricData.Timestamp);
        chart.data.datasets[0].data.push(metricData.CPUUsage);
        chart.data.datasets[1].data.push(metricData.RxUsagePercent);
        chart.data.datasets[2].data.push(metricData.TxUsagePercent);

        // Gem Mbps data for tooltips
        if (!chart.data.metricData) {
            chart.data.metricData = [];
        }
        chart.data.metricData.push(metricData);

        if (chart.data.labels.length > 30) {
            chart.data.labels.shift();
            chart.data.datasets.forEach(dataset => dataset.data.shift());
            chart.data.metricData.shift();
        }

        chart.update();
    }
};
