window.chartInterop = {
    createLineChart: function (canvasId, initialData) {
        const ctx = document.getElementById(canvasId).getContext('2d');
        return new Chart(ctx, {
            type: 'line',
            data: {
                labels: initialData.labels,
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
                            unit: 'minute', // Vis kun tidsstempler hver minut
                            tooltipFormat: 'HH:mm:ss', // Viser tid i 24-timers format
                            displayFormats: {
                                minute: 'HH:mm' // Formatér x-aksen for hvert minut
                            }
                        }
                    },
                    y: {
                        beginAtZero: true
                    }
                }
            }
        });
    },
    updateLineChart: function (chart, label, cpuData, rxData, txData) {
        // Tilføj de nye datapunkter
        chart.data.labels.push(label);
        chart.data.datasets[0].data.push(cpuData);
        chart.data.datasets[1].data.push(rxData);
        chart.data.datasets[2].data.push(txData);

        // Fjern det ældste punkt, hvis der er over 30 datapunkter
        if (chart.data.labels.length > 30) {
            chart.data.labels.shift();  // Fjern det ældste tidspunkt
            chart.data.datasets.forEach(dataset => dataset.data.shift());  // Fjern det ældste datapunkt
        }

        chart.update();  // Opdater grafen
    }
};
