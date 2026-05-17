// Analytics Charts using Chart.js
// Provides dynamic, interactive visualizations for FiberHelp analytics

// Agent-specific charts (ticket-focused only)
window.renderAgentCharts = function(ticketTrendsData, priorityDistributionData) {
    const commonOptions = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: {
                display: true,
                position: 'top',
                labels: {
                    padding: 15,
                    color: '#FFFFFF', // Bright white for legibility
                    font: {
                        size: 12,
                        weight: '600'
                    }
                }
            },
            tooltip: {
                backgroundColor: 'rgba(15, 23, 42, 0.9)',
                padding: 12,
                cornerRadius: 8,
                titleColor: '#FFFFFF',
                bodyColor: '#FFFFFF',
                titleFont: {
                    size: 14,
                    weight: '700'
                },
                bodyFont: {
                    size: 13
                }
            }
        },
        scales: {
            y: {
                beginAtZero: true,
                grace: '5%', // Add some space at the top
                ticks: {
                    color: '#FFFFFF',
                    font: { weight: '500' }
                },
                grid: {
                    color: 'rgba(255, 255, 255, 0.1)'
                }
            },
            x: {
                ticks: {
                    color: '#FFFFFF',
                    font: { weight: '500' }
                },
                grid: {
                    display: false
                }
            }
        }
    };

    // Agent Ticket Trends Chart
    const ticketCtx = document.getElementById('agentTicketTrendsChart');
    if (ticketCtx) {
        const existingChart = Chart.getChart(ticketCtx);
        if (existingChart) existingChart.destroy();
        
        new Chart(ticketCtx, {
            type: 'line',
            data: ticketTrendsData,
            options: {
                ...commonOptions,
                elements: {
                    line: {
                        tension: 0.4
                    },
                    point: {
                        radius: 3,
                        hoverRadius: 5
                    }
                }
            }
        });
    }

    // Agent Priority Distribution Chart
    const priorityCtx = document.getElementById('agentPriorityChart');
    if (priorityCtx) {
        const existingChart = Chart.getChart(priorityCtx);
        if (existingChart) existingChart.destroy();
        
        new Chart(priorityCtx, {
            type: 'doughnut',
            data: priorityDistributionData,
            options: {
                ...commonOptions,
                cutout: '65%',
                plugins: {
                    ...commonOptions.plugins,
                    legend: {
                        display: true,
                        position: 'right',
                        labels: {
                            padding: 10,
                            color: '#FFFFFF',
                            font: {
                                size: 11,
                                weight: '600'
                            },
                            generateLabels: function(chart) {
                                const data = chart.data;
                                return data.labels.map((label, i) => {
                                    const value = data.datasets[0].data[i];
                                    const total = data.datasets[0].data.reduce((a, b) => a + b, 0);
                                    const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : 0;
                                    return {
                                        text: `${label}: ${value} (${percentage}%)`,
                                        fillStyle: data.datasets[0].backgroundColor[i],
                                        hidden: false,
                                        index: i,
                                        fontColor: '#FFFFFF'
                                    };
                                });
                            }
                        }
                    }
                }
            }
        });
    }

    console.log('Agent charts rendered successfully');
};

window.renderAnalyticsCharts = function(
    ticketTrendsData,
    revenueExpensesData,
    clientGrowthData,
    priorityDistributionData,
    statusComparisonData,
    expenseCategoriesData,
    billingStatusData
) {
    // Common chart options
    const commonOptions = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: {
                display: true,
                position: 'top',
                labels: {
                    padding: 15,
                    color: '#FFFFFF',
                    font: {
                        size: 12,
                        weight: '600'
                    }
                }
            },
            tooltip: {
                backgroundColor: 'rgba(15, 23, 42, 0.9)',
                padding: 12,
                cornerRadius: 8,
                titleColor: '#FFFFFF',
                bodyColor: '#FFFFFF',
                titleFont: {
                    size: 14,
                    weight: '700'
                },
                bodyFont: {
                    size: 13
                }
            }
        },
        scales: {
            y: {
                beginAtZero: true,
                grace: '5%',
                ticks: {
                    color: '#FFFFFF',
                    font: { weight: '500' }
                },
                grid: {
                    color: 'rgba(255, 255, 255, 0.1)'
                }
            },
            x: {
                ticks: {
                    color: '#FFFFFF',
                    font: { weight: '500' }
                },
                grid: {
                    display: false
                }
            }
        }
    };

    // 1. Ticket Trends Chart (Line/Area for mini charts)
    const ticketCtx = document.getElementById('ticketTrendsChart') || document.getElementById('miniTicketTrendsChart');
    if (ticketCtx) {
        const existingChart = Chart.getChart(ticketCtx);
        if (existingChart) existingChart.destroy();
        
        new Chart(ticketCtx, {
            type: 'line',
            data: ticketTrendsData,
            options: {
                ...commonOptions,
                elements: {
                    line: {
                        tension: 0.4
                    },
                    point: {
                        radius: 3,
                        hoverRadius: 5
                    }
                }
            }
        });
    }

    // 2. Revenue vs Expenses Chart (Bar)
    const revenueCtx = document.getElementById('revenueExpensesChart') || document.getElementById('miniRevenueExpensesChart');
    if (revenueCtx) {
        const existingChart = Chart.getChart(revenueCtx);
        if (existingChart) existingChart.destroy();
        
        new Chart(revenueCtx, {
            type: 'bar',
            data: revenueExpensesData,
            options: {
                ...commonOptions,
                scales: {
                    ...commonOptions.scales,
                    y: {
                        ...commonOptions.scales.y,
                        ticks: {
                            ...commonOptions.scales.y.ticks,
                            callback: function(value) {
                                return '$' + value.toLocaleString();
                            }
                        }
                    }
                }
            }
        });
    }

    // 3. Client Growth Chart (Area)
    const clientCtx = document.getElementById('clientGrowthChart') || document.getElementById('miniClientGrowthChart');
    if (clientCtx) {
        const existingChart = Chart.getChart(clientCtx);
        if (existingChart) existingChart.destroy();
        
        new Chart(clientCtx, {
            type: 'line',
            data: clientGrowthData,
            options: {
                ...commonOptions,
                elements: {
                    line: {
                        tension: 0.4
                    },
                    point: {
                        radius: 4,
                        hoverRadius: 6
                    }
                }
            }
        });
    }

    // 4. Priority Distribution Chart (Doughnut)
    const priorityCtx = document.getElementById('priorityDistributionChart') || document.getElementById('miniPriorityChart');
    if (priorityCtx) {
        const existingChart = Chart.getChart(priorityCtx);
        if (existingChart) existingChart.destroy();
        
        new Chart(priorityCtx, {
            type: 'doughnut',
            data: priorityDistributionData,
            options: {
                ...commonOptions,
                cutout: '65%',
                plugins: {
                    ...commonOptions.plugins,
                    legend: {
                        display: true,
                        position: 'right',
                        labels: {
                            padding: 10,
                            color: '#FFFFFF',
                            font: {
                                size: 11,
                                weight: '600'
                            },
                            generateLabels: function(chart) {
                                const data = chart.data;
                                return data.labels.map((label, i) => {
                                    const value = data.datasets[0].data[i];
                                    const total = data.datasets[0].data.reduce((a, b) => a + b, 0);
                                    const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : 0;
                                    return {
                                        text: `${label}: ${value} (${percentage}%)`,
                                        fillStyle: data.datasets[0].backgroundColor[i],
                                        hidden: false,
                                        index: i,
                                        fontColor: '#FFFFFF'
                                    };
                                });
                            }
                        }
                    }
                }
            }
        });
    }

    // 5-7: Only render if not null (for full analytics page)
    if (statusComparisonData) {
        const statusCtx = document.getElementById('statusComparisonChart');
        if (statusCtx) {
            const existingChart = Chart.getChart(statusCtx);
            if (existingChart) existingChart.destroy();
            
            new Chart(statusCtx, {
                type: 'bar',
                data: statusComparisonData,
                options: commonOptions
            });
        }
    }

    if (expenseCategoriesData) {
        const expenseCtx = document.getElementById('expenseCategoriesChart');
        if (expenseCtx) {
            const existingChart = Chart.getChart(expenseCtx);
            if (existingChart) existingChart.destroy();
            
            new Chart(expenseCtx, {
                type: 'pie',
                data: expenseCategoriesData,
                options: {
                    ...commonOptions,
                    plugins: {
                        ...commonOptions.plugins,
                        legend: {
                            display: true,
                            position: 'right',
                            labels: {
                                padding: 12,
                                color: '#FFFFFF',
                                font: {
                                    size: 12,
                                    weight: '600'
                                },
                                generateLabels: function(chart) {
                                    const data = chart.data;
                                    return data.labels.map((label, i) => {
                                        const value = data.datasets[0].data[i];
                                        return {
                                            text: `${label}: $${value.toLocaleString()}`,
                                            fillStyle: data.datasets[0].backgroundColor[i],
                                            hidden: false,
                                            index: i,
                                            fontColor: '#FFFFFF'
                                        };
                                    });
                                }
                            }
                        },
                        tooltip: {
                            ...commonOptions.plugins.tooltip,
                            callbacks: {
                                label: function(context) {
                                    const label = context.label || '';
                                    const value = context.parsed || 0;
                                    const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                    const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : 0;
                                    return `${label}: $${value.toLocaleString()} (${percentage}%)`;
                                }
                            }
                        }
                    }
                }
            });
        }
    }

    if (billingStatusData) {
        const billingCtx = document.getElementById('billingStatusChart');
        if (billingCtx) {
            const existingChart = Chart.getChart(billingCtx);
            if (existingChart) existingChart.destroy();
            
            new Chart(billingCtx, {
                type: 'doughnut',
                data: billingStatusData,
                options: {
                    ...commonOptions,
                    cutout: '70%',
                    plugins: {
                        ...commonOptions.plugins,
                        legend: {
                            display: true,
                            position: 'bottom',
                            labels: {
                                padding: 15,
                                color: '#FFFFFF',
                                font: {
                                    size: 12,
                                    weight: '600'
                                },
                                generateLabels: function(chart) {
                                    const data = chart.data;
                                    return data.labels.map((label, i) => {
                                        const value = data.datasets[0].data[i];
                                        const total = data.datasets[0].data.reduce((a, b) => a + b, 0);
                                        const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : 0;
                                        return {
                                            text: `${label}: ${value} (${percentage}%)`,
                                            fillStyle: data.datasets[0].backgroundColor[i],
                                            hidden: false,
                                            index: i,
                                            fontColor: '#FFFFFF'
                                        };
                                    });
                                }
                            }
                        }
                    }
                }
            });
        }
    }

    console.log('Analytics charts rendered successfully');
};

// Cleanup function
window.destroyAnalyticsCharts = function() {
    const chartIds = [
        'ticketTrendsChart',
        'revenueExpensesChart',
        'clientGrowthChart',
        'priorityDistributionChart',
        'statusComparisonChart',
        'expenseCategoriesChart',
        'billingStatusChart',
        'miniTicketTrendsChart',
        'miniRevenueExpensesChart',
        'miniClientGrowthChart',
        'miniPriorityChart'
    ];
    
    chartIds.forEach(chartId => {
        const canvas = document.getElementById(chartId);
        if (canvas) {
            const chart = Chart.getChart(canvas);
            if (chart) {
                chart.destroy();
            }
        }
    });
};
